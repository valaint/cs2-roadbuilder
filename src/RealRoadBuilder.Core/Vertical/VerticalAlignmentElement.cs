using System;

namespace RealRoadBuilder.Core.Vertical;

public abstract class VerticalAlignmentElement
{
    protected VerticalAlignmentElement(double startStationMeters, double endStationMeters)
    {
        if (double.IsNaN(startStationMeters) || double.IsInfinity(startStationMeters))
        {
            throw new ArgumentOutOfRangeException(nameof(startStationMeters));
        }

        if (double.IsNaN(endStationMeters) || double.IsInfinity(endStationMeters))
        {
            throw new ArgumentOutOfRangeException(nameof(endStationMeters));
        }

        if (endStationMeters <= startStationMeters)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endStationMeters),
                "The end station must be greater than the start station.");
        }

        StartStationMeters = startStationMeters;
        EndStationMeters = endStationMeters;
    }

    public double StartStationMeters { get; }

    public double EndStationMeters { get; }

    public double LengthMeters => EndStationMeters - StartStationMeters;

    public abstract double StartElevationMeters { get; }

    public abstract double EndElevationMeters { get; }

    public abstract double ElevationAt(double stationMeters);

    public abstract double GradePercentAt(double stationMeters);

    protected double ValidateAndGetLocalStation(double stationMeters)
    {
        if (stationMeters < StartStationMeters || stationMeters > EndStationMeters)
        {
            throw new ArgumentOutOfRangeException(nameof(stationMeters));
        }

        return stationMeters - StartStationMeters;
    }
}
