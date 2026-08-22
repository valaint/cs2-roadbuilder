using RealRoadBuilder.Core.Standards;
using RealRoadBuilder.Core.Validation;
using RealRoadBuilder.Core.Vertical;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class VerticalCurveBuilderTests
{
    [Fact]
    public void BuildSymmetric_100KphCrest_ProducesContinuousValidProfile()
    {
        VerticalAlignment alignment = VerticalCurveBuilder.BuildSymmetric(
            start: new ProfilePoint(0.0, 97.0),
            intersection: new ProfilePoint(300.0, 100.0),
            end: new ProfilePoint(600.0, 97.0),
            curveLengthMeters: 130.0);

        Assert.Equal(3, alignment.Elements.Count);
        Assert.IsType<ConstantGradeElement>(alignment.Elements[0]);
        ParabolicVerticalCurveElement curve =
            Assert.IsType<ParabolicVerticalCurveElement>(alignment.Elements[1]);
        Assert.IsType<ConstantGradeElement>(alignment.Elements[2]);

        Assert.Equal(235.0, curve.StartStationMeters, 8);
        Assert.Equal(365.0, curve.EndStationMeters, 8);
        Assert.Equal(1.0, curve.StartGradePercent, 8);
        Assert.Equal(-1.0, curve.EndGradePercent, 8);
        Assert.Equal(6500.0, curve.MinimumRadiusMeters, 6);

        AlignmentValidationResult result = VerticalAlignmentValidator.Validate(
            alignment,
            JapanRoadStructureOrdinance.GetExpresswayRule(100));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
