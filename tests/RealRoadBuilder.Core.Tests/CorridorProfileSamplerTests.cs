using System;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Terrain;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class CorridorProfileSamplerTests
{
    [Fact]
    public void Sample_ResamplesRouteAtRequestedIntervalAndKeepsExactEndpoint()
    {
        CorridorRoute route = new(
            new[]
            {
                new CorridorWaypoint(new PlanarPoint(0.0, 0.0), 0.0),
                new CorridorWaypoint(new PlanarPoint(100.0, 0.0), 2.0),
            },
            totalCost: 100.0,
            expandedStates: 1);

        ITerrainSampler terrain = new DelegateTerrainSampler(point => point.X * 0.02);
        var samples = CorridorProfileSampler.Sample(
            route,
            terrain,
            sampleIntervalMeters: 25.0);

        Assert.Equal(5, samples.Count);
        Assert.Equal(0.0, samples[0].StationMeters, 6);
        Assert.Equal(100.0, samples[4].StationMeters, 6);
        Assert.Equal(new PlanarPoint(100.0, 0.0), samples[4].Position);
        Assert.Equal(2.0, samples[4].TerrainElevationMeters, 6);
        Assert.Equal(2.0, samples[4].GradeFromPreviousPercent, 6);
    }

    [Fact]
    public void Sample_UsesCumulativeStationAcrossBentCorridor()
    {
        CorridorRoute route = new(
            new[]
            {
                new CorridorWaypoint(new PlanarPoint(0.0, 0.0), 0.0),
                new CorridorWaypoint(new PlanarPoint(60.0, 0.0), 0.0),
                new CorridorWaypoint(new PlanarPoint(60.0, 80.0), 0.0),
            },
            totalCost: 140.0,
            expandedStates: 1);

        var samples = CorridorProfileSampler.Sample(
            route,
            new DelegateTerrainSampler(_ => 0.0),
            sampleIntervalMeters: 30.0);

        CorridorProfileSample lastSample = samples[samples.Count - 1];
        Assert.Equal(140.0, lastSample.StationMeters, 6);
        Assert.Equal(new PlanarPoint(60.0, 80.0), lastSample.Position);
    }

    private sealed class DelegateTerrainSampler : ITerrainSampler
    {
        private readonly Func<PlanarPoint, double> _sampler;

        public DelegateTerrainSampler(Func<PlanarPoint, double> sampler)
        {
            _sampler = sampler;
        }

        public double GetElevationMeters(PlanarPoint point)
        {
            return _sampler(point);
        }
    }
}
