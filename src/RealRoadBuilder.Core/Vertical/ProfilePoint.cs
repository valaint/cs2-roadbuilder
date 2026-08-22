using System;

namespace RealRoadBuilder.Core.Vertical;

public readonly struct ProfilePoint : IEquatable<ProfilePoint>
{
    public ProfilePoint(double stationMeters, double elevationMeters)
    {
        if (double.IsNaN(stationMeters) || double.IsInfinity(stationMeters))
        {
            throw new ArgumentOutOfRangeException(nameof(stationMeters));
        }

        if (double.IsNaN(elevationMeters) || double.IsInfinity(elevationMeters))
        {
            throw new ArgumentOutOfRangeException(nameof(elevationMeters));
        }

        StationMeters = stationMeters;
        ElevationMeters = elevationMeters;
    }

    public double StationMeters { get; }

    public double ElevationMeters { get; }

    public bool Equals(ProfilePoint other)
    {
        return StationMeters.Equals(other.StationMeters) &&
            ElevationMeters.Equals(other.ElevationMeters);
    }

    public override bool Equals(object? obj)
    {
        return obj is ProfilePoint other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (StationMeters.GetHashCode() * 397) ^ ElevationMeters.GetHashCode();
        }
    }

    public override string ToString()
    {
        return $"Station {StationMeters:0.###} m, Elevation {ElevationMeters:0.###} m";
    }
}
