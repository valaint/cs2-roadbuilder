using RealRoadBuilder.Core.Design;
using RealRoadBuilder.Core.Standards;
using RealRoadBuilder.Core.Validation;
using RealRoadBuilder.Core.Vertical;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class VerticalAlignmentValidatorTests
{
    [Fact]
    public void Validate_100KphCrestAtMinimumRadius_IsValid()
    {
        RoadDesignRule rule = JapanRoadStructureOrdinance.GetExpresswayRule(100);
        ParabolicVerticalCurveElement curve = new(
            start: new ProfilePoint(0.0, 100.0),
            lengthMeters: 130.0,
            startGradePercent: 1.0,
            endGradePercent: -1.0);

        VerticalAlignment alignment = new(new VerticalAlignmentElement[] { curve });
        AlignmentValidationResult result = VerticalAlignmentValidator.Validate(alignment, rule);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_100KphCrestBelowMinimumRadius_IsRejected()
    {
        RoadDesignRule rule = JapanRoadStructureOrdinance.GetExpresswayRule(100);
        ParabolicVerticalCurveElement curve = new(
            start: new ProfilePoint(0.0, 100.0),
            lengthMeters: 100.0,
            startGradePercent: 1.0,
            endGradePercent: -1.0);

        VerticalAlignment alignment = new(new VerticalAlignmentElement[] { curve });
        AlignmentValidationResult result = VerticalAlignmentValidator.Validate(alignment, rule);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("below the required 6500"));
    }

    [Fact]
    public void Validate_GradeBetweenNormalAndExceptionalLimits_RequiresExplicitExceptionalMode()
    {
        RoadDesignRule rule = JapanRoadStructureOrdinance.GetExpresswayRule(100);
        ConstantGradeElement grade = new(
            start: new ProfilePoint(0.0, 0.0),
            end: new ProfilePoint(100.0, 4.0));

        VerticalAlignment alignment = new(new VerticalAlignmentElement[] { grade });
        AlignmentValidationResult normalResult = VerticalAlignmentValidator.Validate(
            alignment,
            rule,
            allowExceptionalGrade: false);
        AlignmentValidationResult exceptionalResult = VerticalAlignmentValidator.Validate(
            alignment,
            rule,
            allowExceptionalGrade: true);

        Assert.False(normalResult.IsValid);
        Assert.True(exceptionalResult.IsValid);
    }

    [Fact]
    public void Validate_InstantGradeBreak_IsRejected()
    {
        RoadDesignRule rule = JapanRoadStructureOrdinance.GetExpresswayRule(100);
        ConstantGradeElement uphill = new(
            start: new ProfilePoint(0.0, 0.0),
            end: new ProfilePoint(100.0, 2.0));
        ConstantGradeElement downhill = new(
            start: new ProfilePoint(100.0, 2.0),
            end: new ProfilePoint(200.0, 0.0));

        VerticalAlignment alignment = new(
            new VerticalAlignmentElement[] { uphill, downhill });
        AlignmentValidationResult result = VerticalAlignmentValidator.Validate(alignment, rule);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Grade discontinuity"));
    }
}
