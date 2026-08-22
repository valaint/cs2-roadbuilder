using System;
using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Terrain;

public sealed class RerouteFeedbackZone
{
    public RerouteFeedbackZone(
        PlanarPoint center,
        double radiusMeters,
        double peakAdditionalCost,
        bool blocked = false,
        string? reason = null)
    {
        if (double.IsNaN(radiusMeters) || double.IsInfinity(radiusMeters) || radiusMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusMeters));
        }

        if (double.IsNaN(peakAdditionalCost) ||
            double.IsInfinity(peakAdditionalCost) ||
            peakAdditionalCost < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(peakAdditionalCost));
        }

        Center = center;
        RadiusMeters = radiusMeters;
        PeakAdditionalCost = peakAdditionalCost;
        Blocked = blocked;
        Reason = reason;
    }

    public PlanarPoint Center { get; }

    public double RadiusMeters { get; }

    public double PeakAdditionalCost { get; }

    public bool Blocked { get; }

    public string? Reason { get; }

    public bool Contains(PlanarPoint point) => Center.DistanceTo(point) < RadiusMeters;

    public double GetAdditionalCost(PlanarPoint point)
    {
        double distanceMeters = Center.DistanceTo(point);
        if (distanceMeters >= RadiusMeters)
        {
            return 0.0;
        }

        double normalizedDistance = distanceMeters / RadiusMeters;
        return PeakAdditionalCost * (1.0 - normalizedDistance);
    }
}
