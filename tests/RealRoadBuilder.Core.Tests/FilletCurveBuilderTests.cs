using System;
using System.Linq;
using RealRoadBuilder.Core.Alignment;
using RealRoadBuilder.Core.Geometry;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class FilletCurveBuilderTests
{
    [Fact]
    public void Build_NinetyDegreeLeftTurn_CreatesExpectedTangentArcTangentGeometry()
    {
        PlanarPoint start = new(0.0, 0.0);
        PlanarPoint pointOfIntersection = new(1000.0, 0.0);
        PlanarPoint end = new(1000.0, 1000.0);
        const double radiusMeters = 460.0;

        HorizontalAlignment alignment = FilletCurveBuilder.Build(
            start,
            pointOfIntersection,
            end,
            radiusMeters);

        Assert.Equal(3, alignment.Elements.Count);

        TangentElement firstTangent = Assert.IsType<TangentElement>(alignment.Elements[0]);
        CircularArcElement curve = Assert.IsType<CircularArcElement>(alignment.Elements[1]);
        TangentElement secondTangent = Assert.IsType<TangentElement>(alignment.Elements[2]);

        Assert.Equal(540.0, firstTangent.End.X, 6);
        Assert.Equal(0.0, firstTangent.End.Y, 6);
        Assert.Equal(1000.0, curve.End.X, 6);
        Assert.Equal(460.0, curve.End.Y, 6);
        Assert.Equal(540.0, curve.Center.X, 6);
        Assert.Equal(460.0, curve.Center.Y, 6);
        Assert.Equal(radiusMeters, curve.RadiusMeters, 6);
        Assert.True(curve.IsLeftTurn);
        Assert.Equal(Math.PI / 2.0, curve.SweepAngleRadians, 6);
        Assert.Equal(540.0, secondTangent.LengthMeters, 6);

        double expectedLengthMeters = 540.0 + (radiusMeters * Math.PI / 2.0) + 540.0;
        Assert.Equal(expectedLengthMeters, alignment.TotalLengthMeters, 6);
        Assert.Single(alignment.Curves);
    }

    [Fact]
    public void CalculateMaximumFittingRadius_NinetyDegreeTurn_EqualsLimitingTangentLength()
    {
        PlanarPoint start = new(0.0, 0.0);
        PlanarPoint pointOfIntersection = new(1000.0, 0.0);
        PlanarPoint end = new(1000.0, 750.0);

        double maximumRadiusMeters = FilletCurveBuilder.CalculateMaximumFittingRadius(
            start,
            pointOfIntersection,
            end);

        Assert.Equal(750.0, maximumRadiusMeters, 6);
    }
}
