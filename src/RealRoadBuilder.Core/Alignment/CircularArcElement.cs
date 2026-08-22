using System;
using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Alignment;

public sealed class CircularArcElement : HorizontalAlignmentElement
{
    public CircularArcElement(
        PlanarPoint start,
        PlanarPoint end,
        PlanarPoint center,
        double radiusMeters,
        double sweepAngleRadians)
        : base(start, end)
    {
        if (radiusMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusMeters));
        }

        if (Math.Abs(sweepAngleRadians) <= 1e-9 || Math.Abs(sweepAngleRadians) >= Math.PI)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sweepAngleRadians),
                "The initial curve builder supports non-zero arcs smaller than 180 degrees.");
        }

        double startRadius = center.DistanceTo(start);
        double endRadius = center.DistanceTo(end);
        const double radiusToleranceMeters = 1e-5;

        if (Math.Abs(startRadius - radiusMeters) > radiusToleranceMeters ||
            Math.Abs(endRadius - radiusMeters) > radiusToleranceMeters)
        {
            throw new ArgumentException("Arc endpoints must lie on the specified circle.");
        }

        Center = center;
        RadiusMeters = radiusMeters;
        SweepAngleRadians = sweepAngleRadians;
        LengthMeters = radiusMeters * Math.Abs(sweepAngleRadians);
    }

    public PlanarPoint Center { get; }

    public double RadiusMeters { get; }

    public double SweepAngleRadians { get; }

    public bool IsLeftTurn => SweepAngleRadians > 0.0;

    public override double LengthMeters { get; }
}
