using RealRoadBuilder.Core.Design;
using RealRoadBuilder.Core.Standards;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class JapanRoadStructureOrdinanceTests
{
    [Fact]
    public void GetExpresswayRule_For100Kph_ReturnsPublishedMlitValues()
    {
        RoadDesignRule rule = JapanRoadStructureOrdinance.GetExpresswayRule(100);

        Assert.Equal("JP-MLIT-RSO", rule.StandardId);
        Assert.Equal(100, rule.DesignSpeedKph);
        Assert.Equal(460.0, rule.MinimumCurveRadiusMeters);
        Assert.Equal(380.0, rule.ExceptionalMinimumCurveRadiusMeters);
        Assert.Equal(85.0, rule.MinimumTransitionLengthMeters);
        Assert.Equal(3.0, rule.MaximumGradePercent);
        Assert.Equal(6.0, rule.ExceptionalMaximumGradePercent);
    }

    [Theory]
    [InlineData(60, 150.0, 120.0, 50.0, 5.0, 8.0)]
    [InlineData(80, 280.0, 230.0, 70.0, 4.0, 7.0)]
    [InlineData(100, 460.0, 380.0, 85.0, 3.0, 6.0)]
    [InlineData(120, 710.0, 570.0, 100.0, 2.0, 5.0)]
    public void ExpresswayRules_MatchInitialMlitCatalog(
        int designSpeedKph,
        double minimumRadiusMeters,
        double exceptionalRadiusMeters,
        double transitionLengthMeters,
        double maximumGradePercent,
        double exceptionalMaximumGradePercent)
    {
        RoadDesignRule rule = JapanRoadStructureOrdinance.GetExpresswayRule(designSpeedKph);

        Assert.Equal(minimumRadiusMeters, rule.MinimumCurveRadiusMeters);
        Assert.Equal(exceptionalRadiusMeters, rule.ExceptionalMinimumCurveRadiusMeters);
        Assert.Equal(transitionLengthMeters, rule.MinimumTransitionLengthMeters);
        Assert.Equal(maximumGradePercent, rule.MaximumGradePercent);
        Assert.Equal(exceptionalMaximumGradePercent, rule.ExceptionalMaximumGradePercent);
    }
}
