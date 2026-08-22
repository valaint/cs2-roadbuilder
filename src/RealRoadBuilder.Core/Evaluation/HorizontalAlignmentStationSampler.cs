using System;
using RealRoadBuilder.Core.Alignment;
using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Evaluation;

public static class HorizontalAlignmentStationSampler
{
    private const double StationToleranceMeters = 1e-6;

    public static PlanarPoint PointAt(HorizontalAlignment alignment, double stationMeters)
    {
        if (alignment == null)
        {
            throw new ArgumentNullException(nameof(alignment));
        }

        if (double.IsNaN(stationMeters) || double.IsInfinity(stationMeters))
        {
            throw new ArgumentOutOfRangeException(nameof(stationMeters));
        }

        if (stationMeters < -StationToleranceMeters ||
            stationMeters > alignment.TotalLengthMeters + StationToleranceMeters)
        {
            throw new ArgumentOutOfRangeException(nameof(stationMeters));
        }

        if (stationMeters <= 0.0)
        {
            return alignment.Start;
        }

        if (stationMeters >= alignment.TotalLengthMeters)
        {
            return alignment.End;
        }

        double elementStartStationMeters = 0.0;

        foreach (HorizontalAlignmentElement element in alignment.Elements)
        {
            double elementEndStationMeters = elementStartStationMeters + element.LengthMeters;
            if (stationMeters <= elementEndStationMeters + StationToleranceMeters)
            {
                double localStationMeters = Math.Max(
                    0.0,
                    Math.Min(element.LengthMeters, stationMeters - elementStartStationMeters));

                return PointAt(element, localStationMeters);
            }

            elementStartStationMeters = elementEndStationMeters;
        }

        return alignment.End;
    }

    private static PlanarPoint PointAt(
        HorizontalAlignmentElement element,
        double localStationMeters)
    {
        if (element is TangentElement tangent)
        {
            return tangent.Start + (tangent.Direction * localStationMeters);
        }

        if (element is TransitionSpiralElement spiral)
        {
            return spiral.PointAt(localStationMeters);
        }

        if (element is CircularArcElement arc)
        {
            if (localStationMeters <= 0.0)
            {
                return arc.Start;
            }

            if (localStationMeters >= arc.LengthMeters)
            {
                return arc.End;
            }

            double startAngleRadians = Math.Atan2(
                arc.Start.Y - arc.Center.Y,
                arc.Start.X - arc.Center.X);
            double fraction = localStationMeters / arc.LengthMeters;
            double angleRadians = startAngleRadians + (arc.SweepAngleRadians * fraction);

            return new PlanarPoint(
                arc.Center.X + (arc.RadiusMeters * Math.Cos(angleRadians)),
                arc.Center.Y + (arc.RadiusMeters * Math.Sin(angleRadians)));
        }

        throw new NotSupportedException(
            $"Unsupported horizontal alignment element type: {element.GetType().FullName}.");
    }
}
