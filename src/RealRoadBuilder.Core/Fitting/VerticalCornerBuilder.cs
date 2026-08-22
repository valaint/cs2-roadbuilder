using System;
using RealRoadBuilder.Core.Design;
using RealRoadBuilder.Core.Vertical;

namespace RealRoadBuilder.Core.Fitting;

public static class VerticalCornerBuilder
{
    private const double GeometryToleranceMeters = 1e-6;

    public static VerticalCornerGeometry Build(
        ProfilePoint previousPoint,
        ProfilePoint intersection,
        ProfilePoint nextPoint,
        RoadDesignRule designRule)
    {
        if (designRule == null)
        {
            throw new ArgumentNullException(nameof(designRule));
        }

        if (intersection.StationMeters <= previousPoint.StationMeters ||
            nextPoint.StationMeters <= intersection.StationMeters)
        {
            throw new ArgumentException(
                "Vertical profile control points must have strictly increasing stations.");
        }

        double incomingGradeDecimal =
            (intersection.ElevationMeters - previousPoint.ElevationMeters) /
            (intersection.StationMeters - previousPoint.StationMeters);
        double outgoingGradeDecimal =
            (nextPoint.ElevationMeters - intersection.ElevationMeters) /
            (nextPoint.StationMeters - intersection.StationMeters);
        double gradeChangeDecimal = outgoingGradeDecimal - incomingGradeDecimal;

        if (Math.Abs(gradeChangeDecimal) <= 1e-12)
        {
            throw new ArgumentException(
                "Incoming and outgoing grades are equal; no vertical curve is required.");
        }

        VerticalCurveType curveType = gradeChangeDecimal < 0.0
            ? VerticalCurveType.Crest
            : VerticalCurveType.Sag;
        double requiredRadiusMeters = curveType == VerticalCurveType.Crest
            ? designRule.MinimumCrestVerticalCurveRadiusMeters
            : designRule.MinimumSagVerticalCurveRadiusMeters;

        double radiusDrivenLengthMeters =
            requiredRadiusMeters * Math.Abs(gradeChangeDecimal) * 1.001;
        double curveLengthMeters = Math.Max(
            designRule.MinimumVerticalCurveLengthMeters,
            radiusDrivenLengthMeters);
        double halfCurveLengthMeters = curveLengthMeters / 2.0;

        double incomingAvailableMeters =
            intersection.StationMeters - previousPoint.StationMeters;
        double outgoingAvailableMeters =
            nextPoint.StationMeters - intersection.StationMeters;

        if (halfCurveLengthMeters > incomingAvailableMeters + GeometryToleranceMeters ||
            halfCurveLengthMeters > outgoingAvailableMeters + GeometryToleranceMeters)
        {
            throw new ArgumentException(
                $"A {curveType.ToString().ToLowerInvariant()} curve meeting the " +
                $"{requiredRadiusMeters:0.###} m radius requirement needs approximately " +
                $"{curveLengthMeters:0.###} m of vertical-curve length, but the adjacent " +
                "PVI spacing is too short.");
        }

        ProfilePoint curveStart = new(
            intersection.StationMeters - halfCurveLengthMeters,
            intersection.ElevationMeters -
                (incomingGradeDecimal * halfCurveLengthMeters));

        ParabolicVerticalCurveElement curve = new(
            start: curveStart,
            lengthMeters: curveLengthMeters,
            startGradePercent: incomingGradeDecimal * 100.0,
            endGradePercent: outgoingGradeDecimal * 100.0);

        double expectedEndElevationMeters =
            intersection.ElevationMeters +
            (outgoingGradeDecimal * halfCurveLengthMeters);

        if (Math.Abs(curve.End.ElevationMeters - expectedEndElevationMeters) > 1e-5)
        {
            throw new InvalidOperationException(
                "Vertical curve geometry did not close onto the outgoing tangent.");
        }

        return new VerticalCornerGeometry(
            previousPoint,
            intersection,
            nextPoint,
            curveStart,
            curve,
            requiredRadiusMeters);
    }
}
