using RealRoadBuilder.Core.Alignment;
using RealRoadBuilder.Core.Design;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Standards;
using RealRoadBuilder.Core.Validation;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class HorizontalAlignmentValidatorTests
{
    [Fact]
    public void Validate_RadiusBetweenNormalAndExceptionalLimits_RequiresExplicitExceptionalMode()
    {
        RoadDesignRule rule = JapanRoadStructureOrdinance.GetExpresswayRule(100);
        HorizontalAlignment alignment = FilletCurveBuilder.Build(
            new PlanarPoint(0.0, 0.0),
            new PlanarPoint(1000.0, 0.0),
            new PlanarPoint(1000.0, 1000.0),
            radiusMeters: 400.0);

        AlignmentValidationResult normalResult = HorizontalAlignmentValidator.Validate(
            alignment,
            rule,
            allowExceptionalCurveRadius: false);
        AlignmentValidationResult exceptionalResult = HorizontalAlignmentValidator.Validate(
            alignment,
            rule,
            allowExceptionalCurveRadius: true);

        Assert.False(normalResult.IsValid);
        Assert.Single(normalResult.Errors);
        Assert.True(exceptionalResult.IsValid);
        Assert.Empty(exceptionalResult.Errors);
        Assert.NotEmpty(exceptionalResult.Warnings);
    }
}
