using System;
using System.Collections.Generic;

namespace RealRoadBuilder.Core.Vertical;

/// <summary>
/// Builds a symmetric tangent-parabola-tangent vertical alignment around a
/// profile point of vertical intersection (PVI).
/// </summary>
public static class VerticalCurveBuilder
{
    private const double GeometryToleranceMeters = 1e-6;

    public static VerticalAlignment BuildSymmetric(
        ProfilePoint start,
        ProfilePoint intersection,
        ProfilePoint end,
        double curveLengthMeters)
    {
        if (curveLengthMeters <= 0.0 ||
            double.IsNaN(curveLengthMeters) ||
            double.IsInfinity(curveLengthMeters))
        {
            throw new ArgumentOutOfRangeException(nameof(curveLengthMeters));
        }

        if (intersection.StationMeters <= start.StationMeters ||
            end.StationMeters <= intersection.StationMeters)
        {
            throw new ArgumentException(
                "Profile points must have strictly increasing stations: start < intersection < end.");
        }

        double halfCurveLengthMeters = curveLengthMeters / 2.0;
        double curveStartStationMeters =
            intersection.StationMeters - halfCurveLengthMeters;
        double curveEndStationMeters =
            intersection.StationMeters + halfCurveLengthMeters;

        if (curveStartStationMeters < start.StationMeters - GeometryToleranceMeters ||
            curveEndStationMeters > end.StationMeters + GeometryToleranceMeters)
        {
            throw new ArgumentException(
                "The requested vertical curve does not fit between the supplied profile points.");
        }

        double incomingGradeDecimal =
            (intersection.ElevationMeters - start.ElevationMeters) /
            (intersection.StationMeters - start.StationMeters);
        double outgoingGradeDecimal =
            (end.ElevationMeters - intersection.ElevationMeters) /
            (end.StationMeters - intersection.StationMeters);

        if (Math.Abs(outgoingGradeDecimal - incomingGradeDecimal) <= 1e-12)
        {
            throw new ArgumentException(
                "The incoming and outgoing grades are equal; no vertical curve is required.");
        }

        ProfilePoint curveStart = new(
            curveStartStationMeters,
            intersection.ElevationMeters -
                (incomingGradeDecimal * halfCurveLengthMeters));

        ParabolicVerticalCurveElement curve = new(
            start: curveStart,
            lengthMeters: curveLengthMeters,
            startGradePercent: incomingGradeDecimal * 100.0,
            endGradePercent: outgoingGradeDecimal * 100.0);

        List<VerticalAlignmentElement> elements = new();

        if (curveStart.StationMeters - start.StationMeters > GeometryToleranceMeters)
        {
            elements.Add(new ConstantGradeElement(start, curveStart));
        }

        elements.Add(curve);

        if (end.StationMeters - curve.EndStationMeters > GeometryToleranceMeters)
        {
            elements.Add(new ConstantGradeElement(curve.End, end));
        }

        return new VerticalAlignment(elements);
    }
}
