using RealRoadBuilder.Core.Vertical;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class ParabolicVerticalCurveElementTests
{
    [Fact]
    public void CrestCurve_ComputesElevationGradeAndMinimumRadius()
    {
        ParabolicVerticalCurveElement curve = new(
            start: new ProfilePoint(0.0, 100.0),
            lengthMeters: 130.0,
            startGradePercent: 1.0,
            endGradePercent: -1.0);

        Assert.Equal(VerticalCurveType.Crest, curve.CurveType);
        Assert.Equal(130.0, curve.EndStationMeters, 10);
        Assert.Equal(100.0, curve.EndElevationMeters, 8);
        Assert.Equal(6500.0, curve.MinimumRadiusMeters, 6);
        Assert.Equal(1.0, curve.GradePercentAt(0.0), 8);
        Assert.Equal(0.0, curve.GradePercentAt(65.0), 8);
        Assert.Equal(-1.0, curve.GradePercentAt(130.0), 8);
        Assert.Equal(100.325, curve.ElevationAt(65.0), 6);
    }

    [Fact]
    public void SagCurve_IsClassifiedFromIncreasingGrade()
    {
        ParabolicVerticalCurveElement curve = new(
            start: new ProfilePoint(0.0, 100.0),
            lengthMeters: 100.0,
            startGradePercent: -1.0,
            endGradePercent: 1.0);

        Assert.Equal(VerticalCurveType.Sag, curve.CurveType);
        Assert.Equal(5000.0, curve.MinimumRadiusMeters, 6);
        Assert.Equal(99.75, curve.ElevationAt(50.0), 6);
    }
}
