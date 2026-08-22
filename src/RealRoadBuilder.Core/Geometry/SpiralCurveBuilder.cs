using System;
using System.Collections.Generic;
using RealRoadBuilder.Core.Alignment;

namespace RealRoadBuilder.Core.Geometry;

/// <summary>
/// Builds a symmetric tangent-spiral-circular-spiral-tangent horizontal curve.
/// </summary>
public static class SpiralCurveBuilder
{
    private const double GeometryTolerance = 1e-6;
    private const double EndPointToleranceMeters = 1e-3;

    public static HorizontalAlignment Build(
        PlanarPoint start,
        PlanarPoint intersection,
        PlanarPoint end,
        double radiusMeters,
        double transitionLengthMeters)
    {
        if (radiusMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusMeters));
        }

        if (transitionLengthMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(transitionLengthMeters));
        }

        PlanarVector incomingVector = intersection - start;
        PlanarVector outgoingVector = end - intersection;
        double incomingLengthMeters = incomingVector.Length;
        double outgoingLengthMeters = outgoingVector.Length;

        if (incomingLengthMeters <= GeometryTolerance || outgoingLengthMeters <= GeometryTolerance)
        {
            throw new ArgumentException("Start, intersection, and end points must define two non-zero tangents.");
        }

        PlanarVector incomingDirection = incomingVector.Normalized();
        PlanarVector outgoingDirection = outgoingVector.Normalized();
        double cross = PlanarVector.Cross(incomingDirection, outgoingDirection);

        if (Math.Abs(cross) <= GeometryTolerance)
        {
            throw new ArgumentException("The tangents must form a non-zero horizontal deflection angle.");
        }

        double turnSign = cross > 0.0 ? 1.0 : -1.0;
        double deflectionAngleRadians = PlanarVector.AngleBetween(
            incomingDirection,
            outgoingDirection);

        if (deflectionAngleRadians >= Math.PI - GeometryTolerance)
        {
            throw new ArgumentException("U-turn geometry is not supported by the spiral curve builder.");
        }

        double spiralAngleRadians = transitionLengthMeters / (2.0 * radiusMeters);
        double circularSweepMagnitudeRadians =
            deflectionAngleRadians - (2.0 * spiralAngleRadians);

        if (circularSweepMagnitudeRadians <= GeometryTolerance)
        {
            throw new ArgumentException(
                "The transition spirals consume the entire deflection. Increase curve radius, reduce transition length, or increase the deflection angle.");
        }

        TransitionSpiralElement localSpiral = new(
            start: new PlanarPoint(0.0, 0.0),
            startHeadingRadians: 0.0,
            lengthMeters: transitionLengthMeters,
            startCurvaturePerMeter: 0.0,
            endCurvaturePerMeter: turnSign / radiusMeters);

        double localSpiralX = localSpiral.End.X;
        double localSpiralY = Math.Abs(localSpiral.End.Y);
        double spiralShiftMeters = localSpiralY -
            (radiusMeters * (1.0 - Math.Cos(spiralAngleRadians)));
        double tangentCorrectionMeters = localSpiralX -
            (radiusMeters * Math.Sin(spiralAngleRadians));
        double tangentLengthMeters =
            ((radiusMeters + spiralShiftMeters) * Math.Tan(deflectionAngleRadians / 2.0)) +
            tangentCorrectionMeters;

        if (tangentLengthMeters >= incomingLengthMeters - GeometryTolerance ||
            tangentLengthMeters >= outgoingLengthMeters - GeometryTolerance)
        {
            throw new ArgumentException(
                "The supplied tangent segments are too short to fit the requested radius and transition length.");
        }

        PlanarPoint transitionStart =
            intersection - (incomingDirection * tangentLengthMeters);
        PlanarPoint expectedTransitionEnd =
            intersection + (outgoingDirection * tangentLengthMeters);
        double incomingHeadingRadians = Math.Atan2(incomingDirection.Y, incomingDirection.X);

        TransitionSpiralElement entrySpiral = new(
            start: transitionStart,
            startHeadingRadians: incomingHeadingRadians,
            lengthMeters: transitionLengthMeters,
            startCurvaturePerMeter: 0.0,
            endCurvaturePerMeter: turnSign / radiusMeters);

        PlanarVector entryEndDirection = DirectionFromHeading(entrySpiral.EndHeadingRadians);
        PlanarPoint center = entrySpiral.End +
            (entryEndDirection.LeftNormal() * (turnSign * radiusMeters));

        double signedCircularSweepRadians =
            turnSign * circularSweepMagnitudeRadians;
        PlanarVector startRadiusVector = entrySpiral.End - center;
        PlanarVector endRadiusVector = Rotate(startRadiusVector, signedCircularSweepRadians);
        PlanarPoint circularArcEnd = center + endRadiusVector;

        CircularArcElement circularArc = new(
            start: entrySpiral.End,
            end: circularArcEnd,
            center: center,
            radiusMeters: radiusMeters,
            sweepAngleRadians: signedCircularSweepRadians);

        double exitStartHeadingRadians =
            entrySpiral.EndHeadingRadians + signedCircularSweepRadians;

        TransitionSpiralElement exitSpiral = new(
            start: circularArc.End,
            startHeadingRadians: exitStartHeadingRadians,
            lengthMeters: transitionLengthMeters,
            startCurvaturePerMeter: turnSign / radiusMeters,
            endCurvaturePerMeter: 0.0);

        double endPointErrorMeters = exitSpiral.End.DistanceTo(expectedTransitionEnd);
        if (endPointErrorMeters > EndPointToleranceMeters)
        {
            throw new InvalidOperationException(
                $"Spiral curve geometry failed to close by {endPointErrorMeters:0.######} m.");
        }

        List<HorizontalAlignmentElement> elements = new()
        {
            new TangentElement(start, transitionStart),
            entrySpiral,
            circularArc,
            exitSpiral,
            new TangentElement(exitSpiral.End, end),
        };

        return new HorizontalAlignment(elements);
    }

    private static PlanarVector DirectionFromHeading(double headingRadians)
    {
        return new PlanarVector(Math.Cos(headingRadians), Math.Sin(headingRadians));
    }

    private static PlanarVector Rotate(PlanarVector vector, double angleRadians)
    {
        double cosine = Math.Cos(angleRadians);
        double sine = Math.Sin(angleRadians);

        return new PlanarVector(
            (vector.X * cosine) - (vector.Y * sine),
            (vector.X * sine) + (vector.Y * cosine));
    }
}
