using RealRoadBuilder.Core.Alignment;
using RealRoadBuilder.Core.Evaluation;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Terrain;
using RealRoadBuilder.Core.Vertical;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class FinalAlignmentTerrainSamplerTests
{
    [Fact]
    public void Sample_StraightAlignment_UsesExactPlanAndVerticalStations()
    {
        HorizontalAlignment horizontal = new(new HorizontalAlignmentElement[]
        {
            new TangentElement(new PlanarPoint(0.0, 0.0), new PlanarPoint(100.0, 0.0)),
        });
        VerticalAlignment vertical = new(new VerticalAlignmentElement[]
        {
            new ConstantGradeElement(
                new ProfilePoint(0.0, 10.0),
                new ProfilePoint(100.0, 12.0)),
        });

        var samples = FinalAlignmentTerrainSampler.Sample(
            horizontal,
            vertical,
            new FlatTerrainSampler(5.0),
            intervalMeters: 25.0);

        Assert.Equal(5, samples.Count);
        Assert.Equal(50.0, samples[2].Position.X, 6);
        Assert.Equal(0.0, samples[2].Position.Y, 6);
        Assert.Equal(11.0, samples[2].DesignElevationMeters, 6);
        Assert.Equal(5.0, samples[2].TerrainElevationMeters, 6);
        Assert.Equal(6.0, samples[2].VerticalOffsetMeters, 6);
        Assert.Equal(2.0, samples[2].DesignGradePercent, 6);
        Assert.Equal(100.0, samples[^1].StationMeters, 6);
    }

    private sealed class FlatTerrainSampler : ITerrainSampler
    {
        private readonly double _elevationMeters;

        public FlatTerrainSampler(double elevationMeters)
        {
            _elevationMeters = elevationMeters;
        }

        public double GetElevationMeters(PlanarPoint position) => _elevationMeters;
    }
}
