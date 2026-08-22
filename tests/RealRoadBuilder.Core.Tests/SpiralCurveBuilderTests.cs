using RealRoadBuilder.Core.Alignment;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Standards;
using RealRoadBuilder.Core.Validation;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class SpiralCurveBuilderTests
{
    [Fact]
    public void Build_100KphNinetyDegreeCurve_ProducesStandardCompliantHorizontalAlignment()
    {
        HorizontalAlignment alignment = SpiralCurveBuilder.Build(
            start: new PlanarPoint(-1000.0, 0.0),
            intersection: new PlanarPoint(0.0, 0.0),
            end: new PlanarPoint(0.0, 1000.0),
            radiusMeters: 460.0,
            transitionLengthMeters: 85.0);

        Assert.Equal(5, alignment.Elements.Count);
        Assert.IsType<TangentElement>(alignment.Elements[0]);
        Assert.IsType<TransitionSpiralElement>(alignment.Elements[1]);
        Assert.IsType<CircularArcElement>(alignment.Elements[2]);
        Assert.IsType<TransitionSpiralElement>(alignment.Elements[3]);
        Assert.IsType<TangentElement>(alignment.Elements[4]);
        Assert.InRange(alignment.End.DistanceTo(new PlanarPoint(0.0, 1000.0)), 0.0, 1e-6);

        AlignmentValidationResult result = HorizontalAlignmentValidator.Validate(
            alignment,
            JapanRoadStructureOrdinance.GetExpresswayRule(100));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Build_RightTurn_ProducesNegativeArcSweep()
    {
        HorizontalAlignment alignment = SpiralCurveBuilder.Build(
            start: new PlanarPoint(-1000.0, 0.0),
            intersection: new PlanarPoint(0.0, 0.0),
            end: new PlanarPoint(0.0, -1000.0),
            radiusMeters: 460.0,
            transitionLengthMeters: 85.0);

        CircularArcElement curve = Assert.IsType<CircularArcElement>(alignment.Elements[2]);
        TransitionSpiralElement entry = Assert.IsType<TransitionSpiralElement>(alignment.Elements[1]);

        Assert.True(curve.SweepAngleRadians < 0.0);
        Assert.True(entry.EndCurvaturePerMeter < 0.0);
    }
}
