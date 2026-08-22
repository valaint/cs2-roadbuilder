using System;
using RealRoadBuilder.Core.Alignment;
using RealRoadBuilder.Core.Evaluation;
using RealRoadBuilder.Core.Geometry;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class HorizontalAlignmentStationSamplerTests
{
    [Fact]
    public void PointAt_SpiralArcSpiralAlignment_MatchesEveryElementBoundary()
    {
        HorizontalAlignment alignment = SpiralCurveBuilder.Build(
            new PlanarPoint(0.0, 0.0),
            new PlanarPoint(1000.0, 0.0),
            new PlanarPoint(1000.0, 1000.0),
            radiusMeters: 460.0,
            transitionLengthMeters: 85.0);

        double stationMeters = 0.0;
        foreach (HorizontalAlignmentElement element in alignment.Elements)
        {
            stationMeters += element.LengthMeters;
            PlanarPoint sampled = HorizontalAlignmentStationSampler.PointAt(
                alignment,
                stationMeters);

            Assert.True(
                sampled.DistanceTo(element.End) < 1e-3,
                $"Station {stationMeters:0.###} missed the element boundary by {sampled.DistanceTo(element.End):0.######} m.");
        }
    }

    [Fact]
    public void PointAt_CircularArcMidpoint_RemainsOnSpecifiedRadius()
    {
        HorizontalAlignment alignment = SpiralCurveBuilder.Build(
            new PlanarPoint(0.0, 0.0),
            new PlanarPoint(1000.0, 0.0),
            new PlanarPoint(1000.0, 1000.0),
            radiusMeters: 460.0,
            transitionLengthMeters: 85.0);

        double stationMeters = 0.0;
        CircularArcElement? arc = null;
        double arcStartStationMeters = 0.0;

        foreach (HorizontalAlignmentElement element in alignment.Elements)
        {
            if (element is CircularArcElement circularArc)
            {
                arc = circularArc;
                arcStartStationMeters = stationMeters;
                break;
            }

            stationMeters += element.LengthMeters;
        }

        Assert.NotNull(arc);
        PlanarPoint midpoint = HorizontalAlignmentStationSampler.PointAt(
            alignment,
            arcStartStationMeters + (arc!.LengthMeters * 0.5));

        Assert.Equal(arc.RadiusMeters, midpoint.DistanceTo(arc.Center), 5);
        Assert.True(midpoint.DistanceTo(arc.Start) > 1.0);
        Assert.True(midpoint.DistanceTo(arc.End) > 1.0);
    }
}
