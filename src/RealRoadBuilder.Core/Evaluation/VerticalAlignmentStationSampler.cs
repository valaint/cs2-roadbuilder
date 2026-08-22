using System;
using RealRoadBuilder.Core.Vertical;

namespace RealRoadBuilder.Core.Evaluation;

public static class VerticalAlignmentStationSampler
{
    private const double StationToleranceMeters = 1e-6;

    public static double ElevationAt(VerticalAlignment alignment, double stationMeters)
    {
        VerticalAlignmentElement element = FindElement(alignment, stationMeters, out double clampedStationMeters);
        return element.ElevationAt(clampedStationMeters);
    }

    public static double GradePercentAt(VerticalAlignment alignment, double stationMeters)
    {
        VerticalAlignmentElement element = FindElement(alignment, stationMeters, out double clampedStationMeters);
        return element.GradePercentAt(clampedStationMeters);
    }

    private static VerticalAlignmentElement FindElement(
        VerticalAlignment alignment,
        double stationMeters,
        out double clampedStationMeters)
    {
        if (alignment == null)
        {
            throw new ArgumentNullException(nameof(alignment));
        }

        if (double.IsNaN(stationMeters) || double.IsInfinity(stationMeters))
        {
            throw new ArgumentOutOfRangeException(nameof(stationMeters));
        }

        if (stationMeters < alignment.StartStationMeters - StationToleranceMeters ||
            stationMeters > alignment.EndStationMeters + StationToleranceMeters)
        {
            throw new ArgumentOutOfRangeException(nameof(stationMeters));
        }

        for (int index = 0; index < alignment.Elements.Count; index++)
        {
            VerticalAlignmentElement element = alignment.Elements[index];
            if (stationMeters <= element.EndStationMeters + StationToleranceMeters &&
                stationMeters >= element.StartStationMeters - StationToleranceMeters)
            {
                clampedStationMeters = Math.Max(
                    element.StartStationMeters,
                    Math.Min(element.EndStationMeters, stationMeters));
                return element;
            }
        }

        VerticalAlignmentElement finalElement = alignment.Elements[alignment.Elements.Count - 1];
        clampedStationMeters = finalElement.EndStationMeters;
        return finalElement;
    }
}
