using System;
using System.Collections.Generic;
using RealRoadBuilder.Core.Design;

namespace RealRoadBuilder.Core.Standards;

/// <summary>
/// Japanese highway design rules sourced from the MLIT English translation
/// of the Road Structure Ordinance, including horizontal alignment,
/// transition sections, grades, and vertical curves.
///
/// Sources:
/// https://www.mlit.go.jp/road/road_e/r1_standard_2.html
/// https://www.mlit.go.jp/road/road_e/r1_standard_3.html
///
/// This catalog intentionally starts with Type 1 regular motor vehicle roads
/// at the expressway design speeds used by the first generator.
/// </summary>
public static class JapanRoadStructureOrdinance
{
    public const string StandardId = "JP-MLIT-RSO";
    public const string ExpresswayRoadClass = "Type 1 regular motor vehicle road";

    private static readonly IReadOnlyDictionary<int, RoadDesignRule> ExpresswayRules =
        new Dictionary<int, RoadDesignRule>
        {
            [120] = CreateRule(
                designSpeedKph: 120,
                minimumCurveRadiusMeters: 710.0,
                exceptionalMinimumCurveRadiusMeters: 570.0,
                minimumTransitionLengthMeters: 100.0,
                maximumGradePercent: 2.0,
                exceptionalMaximumGradePercent: 5.0,
                minimumCrestVerticalCurveRadiusMeters: 11000.0,
                minimumSagVerticalCurveRadiusMeters: 4000.0,
                minimumVerticalCurveLengthMeters: 100.0),
            [100] = CreateRule(
                designSpeedKph: 100,
                minimumCurveRadiusMeters: 460.0,
                exceptionalMinimumCurveRadiusMeters: 380.0,
                minimumTransitionLengthMeters: 85.0,
                maximumGradePercent: 3.0,
                exceptionalMaximumGradePercent: 6.0,
                minimumCrestVerticalCurveRadiusMeters: 6500.0,
                minimumSagVerticalCurveRadiusMeters: 3000.0,
                minimumVerticalCurveLengthMeters: 85.0),
            [80] = CreateRule(
                designSpeedKph: 80,
                minimumCurveRadiusMeters: 280.0,
                exceptionalMinimumCurveRadiusMeters: 230.0,
                minimumTransitionLengthMeters: 70.0,
                maximumGradePercent: 4.0,
                exceptionalMaximumGradePercent: 7.0,
                minimumCrestVerticalCurveRadiusMeters: 3000.0,
                minimumSagVerticalCurveRadiusMeters: 2000.0,
                minimumVerticalCurveLengthMeters: 70.0),
            [60] = CreateRule(
                designSpeedKph: 60,
                minimumCurveRadiusMeters: 150.0,
                exceptionalMinimumCurveRadiusMeters: 120.0,
                minimumTransitionLengthMeters: 50.0,
                maximumGradePercent: 5.0,
                exceptionalMaximumGradePercent: 8.0,
                minimumCrestVerticalCurveRadiusMeters: 1400.0,
                minimumSagVerticalCurveRadiusMeters: 1000.0,
                minimumVerticalCurveLengthMeters: 50.0),
        };

    public static IReadOnlyCollection<int> SupportedExpresswayDesignSpeedsKph =>
        new[] { 60, 80, 100, 120 };

    public static RoadDesignRule GetExpresswayRule(int designSpeedKph)
    {
        if (!ExpresswayRules.TryGetValue(designSpeedKph, out RoadDesignRule? rule))
        {
            throw new ArgumentOutOfRangeException(
                nameof(designSpeedKph),
                designSpeedKph,
                "Supported Japanese expressway design speeds are 60, 80, 100 and 120 km/h.");
        }

        return rule;
    }

    private static RoadDesignRule CreateRule(
        int designSpeedKph,
        double minimumCurveRadiusMeters,
        double exceptionalMinimumCurveRadiusMeters,
        double minimumTransitionLengthMeters,
        double maximumGradePercent,
        double exceptionalMaximumGradePercent,
        double minimumCrestVerticalCurveRadiusMeters,
        double minimumSagVerticalCurveRadiusMeters,
        double minimumVerticalCurveLengthMeters)
    {
        return new RoadDesignRule(
            standardId: StandardId,
            roadClass: ExpresswayRoadClass,
            designSpeedKph: designSpeedKph,
            minimumCurveRadiusMeters: minimumCurveRadiusMeters,
            exceptionalMinimumCurveRadiusMeters: exceptionalMinimumCurveRadiusMeters,
            minimumTransitionLengthMeters: minimumTransitionLengthMeters,
            maximumGradePercent: maximumGradePercent,
            exceptionalMaximumGradePercent: exceptionalMaximumGradePercent,
            minimumCrestVerticalCurveRadiusMeters: minimumCrestVerticalCurveRadiusMeters,
            minimumSagVerticalCurveRadiusMeters: minimumSagVerticalCurveRadiusMeters,
            minimumVerticalCurveLengthMeters: minimumVerticalCurveLengthMeters);
    }
}
