using System;

namespace RealRoadBuilder.Core.Design;

public sealed class RoadDesignRule
{
    public RoadDesignRule(
        string standardId,
        string roadClass,
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
        if (string.IsNullOrWhiteSpace(standardId))
        {
            throw new ArgumentException("A standard identifier is required.", nameof(standardId));
        }

        if (string.IsNullOrWhiteSpace(roadClass))
        {
            throw new ArgumentException("A road class is required.", nameof(roadClass));
        }

        if (designSpeedKph <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(designSpeedKph));
        }

        if (minimumCurveRadiusMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumCurveRadiusMeters));
        }

        if (exceptionalMinimumCurveRadiusMeters <= 0.0 ||
            exceptionalMinimumCurveRadiusMeters > minimumCurveRadiusMeters)
        {
            throw new ArgumentOutOfRangeException(nameof(exceptionalMinimumCurveRadiusMeters));
        }

        if (minimumTransitionLengthMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumTransitionLengthMeters));
        }

        if (maximumGradePercent <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumGradePercent));
        }

        if (exceptionalMaximumGradePercent < maximumGradePercent)
        {
            throw new ArgumentOutOfRangeException(nameof(exceptionalMaximumGradePercent));
        }

        if (minimumCrestVerticalCurveRadiusMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumCrestVerticalCurveRadiusMeters));
        }

        if (minimumSagVerticalCurveRadiusMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumSagVerticalCurveRadiusMeters));
        }

        if (minimumVerticalCurveLengthMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumVerticalCurveLengthMeters));
        }

        StandardId = standardId;
        RoadClass = roadClass;
        DesignSpeedKph = designSpeedKph;
        MinimumCurveRadiusMeters = minimumCurveRadiusMeters;
        ExceptionalMinimumCurveRadiusMeters = exceptionalMinimumCurveRadiusMeters;
        MinimumTransitionLengthMeters = minimumTransitionLengthMeters;
        MaximumGradePercent = maximumGradePercent;
        ExceptionalMaximumGradePercent = exceptionalMaximumGradePercent;
        MinimumCrestVerticalCurveRadiusMeters = minimumCrestVerticalCurveRadiusMeters;
        MinimumSagVerticalCurveRadiusMeters = minimumSagVerticalCurveRadiusMeters;
        MinimumVerticalCurveLengthMeters = minimumVerticalCurveLengthMeters;
    }

    public string StandardId { get; }

    public string RoadClass { get; }

    public int DesignSpeedKph { get; }

    public double MinimumCurveRadiusMeters { get; }

    public double ExceptionalMinimumCurveRadiusMeters { get; }

    public double MinimumTransitionLengthMeters { get; }

    public double MaximumGradePercent { get; }

    public double ExceptionalMaximumGradePercent { get; }

    public double MinimumCrestVerticalCurveRadiusMeters { get; }

    public double MinimumSagVerticalCurveRadiusMeters { get; }

    public double MinimumVerticalCurveLengthMeters { get; }
}
