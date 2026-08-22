using System;
using System.Collections.Generic;
using RealRoadBuilder.Core.Alignment;

namespace RealRoadBuilder.Core.Geometry;

/// <summary>
/// Builds a tangent-arc-tangent horizontal alignment around a point of
/// intersection (PI) using a constant-radius circular curve.
///
/// Transition spirals are intentionally not inserted yet. The design-standard
/// model already carries their required minimum length so a later release can
/// add clothoid/spiral elements without changing the alignment API.
/// </summary>
public static class FilletCurveBuilder
{
    private const double Epsilon = 1e-9;

    public static HorizontalAlignment Build(
        PlanarPoint start,
        PlanarPoint pointOfIntersection,
        PlanarPoint end,
        double radiusMeters)
    {
        if (radiusMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusMeters));
        }

        PlanarVector incomingVector = pointOfIntersection - start;
        PlanarVector outgoingVector = end - pointOfIntersection;
        double incomingLength = incomingVector.Length;
        double outgoingLength = outgoingVector.Length;

        if (incomingLength <= Epsilon || outgoingLength <= Epsilon)
        {
            throw new ArgumentException("Start, PI and end must be distinct points.");
        }

        PlanarVector incomingDirection = incomingVector.Normalized();
        PlanarVector outgoingDirection = outgoingVector.Normalized();
        double turnAngleRadians = PlanarVector.AngleBetween(incomingDirection, outgoingDirection);
        double turnCross = PlanarVector.Cross(incomingDirection, outgoingDirection);

        if (turnAngleRadians <= Epsilon || Math.Abs(turnCross) <= Epsilon)
        {
            throw new ArgumentException("The three points do not define a usable turn.");
        }

        if (Math.PI - turnAngleRadians <= 1e-6)
        {
            throw new ArgumentException("A 180-degree reversal cannot be represented by one fillet curve.");
        }

        double tangentDistanceMeters = radiusMeters * Math.Tan(turnAngleRadians / 2.0);
        if (tangentDistanceMeters >= incomingLength || tangentDistanceMeters >= outgoingLength)
        {
            double maximumRadiusMeters = CalculateMaximumFittingRadius(
                start,
                pointOfIntersection,
                end);

            throw new ArgumentOutOfRangeException(
                nameof(radiusMeters),
                radiusMeters,
                $"The requested radius does not fit between the supplied points. Maximum radius is less than {maximumRadiusMeters:0.###} m.");
        }

        PlanarPoint tangentStart =
            pointOfIntersection - (incomingDirection * tangentDistanceMeters);
        PlanarPoint tangentEnd =
            pointOfIntersection + (outgoingDirection * tangentDistanceMeters);

        bool isLeftTurn = turnCross > 0.0;
        PlanarVector incomingNormal = isLeftTurn
            ? incomingDirection.LeftNormal()
            : incomingDirection.RightNormal();
        PlanarVector outgoingNormal = isLeftTurn
            ? outgoingDirection.LeftNormal()
            : outgoingDirection.RightNormal();

        PlanarPoint centerFromIncoming = tangentStart + (incomingNormal * radiusMeters);
        PlanarPoint centerFromOutgoing = tangentEnd + (outgoingNormal * radiusMeters);
        PlanarPoint center = new PlanarPoint(
            (centerFromIncoming.X + centerFromOutgoing.X) / 2.0,
            (centerFromIncoming.Y + centerFromOutgoing.Y) / 2.0);

        PlanarVector startRadiusVector = tangentStart - center;
        PlanarVector endRadiusVector = tangentEnd - center;
        double sweepAngleRadians = Math.Atan2(
            PlanarVector.Cross(startRadiusVector, endRadiusVector),
            PlanarVector.Dot(startRadiusVector, endRadiusVector));

        List<HorizontalAlignmentElement> elements = new();

        if (start.DistanceTo(tangentStart) > Epsilon)
        {
            elements.Add(new TangentElement(start, tangentStart));
        }

        elements.Add(new CircularArcElement(
            tangentStart,
            tangentEnd,
            center,
            radiusMeters,
            sweepAngleRadians));

        if (tangentEnd.DistanceTo(end) > Epsilon)
        {
            elements.Add(new TangentElement(tangentEnd, end));
        }

        return new HorizontalAlignment(elements);
    }

    public static double CalculateMaximumFittingRadius(
        PlanarPoint start,
        PlanarPoint pointOfIntersection,
        PlanarPoint end)
    {
        PlanarVector incomingVector = pointOfIntersection - start;
        PlanarVector outgoingVector = end - pointOfIntersection;

        if (incomingVector.Length <= Epsilon || outgoingVector.Length <= Epsilon)
        {
            return 0.0;
        }

        PlanarVector incomingDirection = incomingVector.Normalized();
        PlanarVector outgoingDirection = outgoingVector.Normalized();
        double turnAngleRadians = PlanarVector.AngleBetween(incomingDirection, outgoingDirection);

        if (turnAngleRadians <= Epsilon || Math.PI - turnAngleRadians <= 1e-6)
        {
            return 0.0;
        }

        double tangentFactor = Math.Tan(turnAngleRadians / 2.0);
        if (tangentFactor <= Epsilon)
        {
            return 0.0;
        }

        double limitingTangentLength = Math.Min(incomingVector.Length, outgoingVector.Length);
        return limitingTangentLength / tangentFactor;
    }
}
