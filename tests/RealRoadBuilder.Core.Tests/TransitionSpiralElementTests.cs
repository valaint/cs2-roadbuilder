using System;
using RealRoadBuilder.Core.Alignment;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Standards;
using RealRoadBuilder.Core.Validation;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class TransitionSpiralElementTests
{
    [Fact]
    public void TransitionSpiral_FromTangentTo500MeterRadius_ProducesExpectedGeometry()
    {
        TransitionSpiralElement spiral = new(
            start: new PlanarPoint(0.0, 0.0),
            startHeadingRadians: 0.0,
            lengthMeters: 100.0,
            startCurvaturePerMeter: 0.0,
            endCurvaturePerMeter: 1.0 / 500.0);

        Assert.Equal(100.0, spiral.LengthMeters, 10);
        Assert.Equal(0.0, spiral.StartCurvaturePerMeter, 12);
        Assert.Equal(1.0 / 500.0, spiral.EndCurvaturePerMeter, 12);
        Assert.Equal(500.0, spiral.MinimumRadiusMeters, 8);
        Assert.Equal(0.1, spiral.EndHeadingRadians, 8);
        Assert.InRange(spiral.End.X, 99.89, 99.91);
        Assert.InRange(spiral.End.Y, 3.32, 3.35);
    }

    [Fact]
    public void TransitionSpiral_RightTurn_UsesNegativeCurvature()
    {
        TransitionSpiralElement spiral = new(
            start: new PlanarPoint(0.0, 0.0),
            startHeadingRadians: 0.0,
            lengthMeters: 100.0,
            startCurvaturePerMeter: 0.0,
            endCurvaturePerMeter: -1.0 / 500.0);

        Assert.Equal(-0.1, spiral.EndHeadingRadians, 8);
        Assert.True(spiral.End.Y < 0.0);
    }

    [Fact]
    public void HorizontalValidator_RejectsTooShortTransition()
    {
        TransitionSpiralElement spiral = new(
            start: new PlanarPoint(0.0, 0.0),
            startHeadingRadians: 0.0,
            lengthMeters: 60.0,
            startCurvaturePerMeter: 0.0,
            endCurvaturePerMeter: 1.0 / 500.0);

        HorizontalAlignment alignment = new(new HorizontalAlignmentElement[] { spiral });
        AlignmentValidationResult result = HorizontalAlignmentValidator.Validate(
            alignment,
            JapanRoadStructureOrdinance.GetExpresswayRule(100));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("below the required 85"));
    }
}
