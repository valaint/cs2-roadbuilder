using System;
using System.Collections.Generic;
using System.Linq;
using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Terrain;

public sealed class RerouteFeedbackConstraintProvider : ICorridorConstraintProvider
{
    private readonly ICorridorConstraintProvider? _baseProvider;
    private readonly IReadOnlyList<RerouteFeedbackZone> _zones;

    public RerouteFeedbackConstraintProvider(
        IEnumerable<RerouteFeedbackZone> zones,
        ICorridorConstraintProvider? baseProvider = null)
    {
        if (zones == null)
        {
            throw new ArgumentNullException(nameof(zones));
        }

        _zones = zones.ToList().AsReadOnly();
        _baseProvider = baseProvider;
    }

    public IReadOnlyList<RerouteFeedbackZone> Zones => _zones;

    public bool IsBlocked(PlanarPoint point)
    {
        if (_baseProvider?.IsBlocked(point) == true)
        {
            return true;
        }

        return _zones.Any(zone => zone.Blocked && zone.Contains(point));
    }

    public double GetAdditionalCost(PlanarPoint point)
    {
        double cost = _baseProvider?.GetAdditionalCost(point) ?? 0.0;
        if (cost < 0.0)
        {
            throw new InvalidOperationException("The base corridor constraint provider returned a negative cost.");
        }

        foreach (RerouteFeedbackZone zone in _zones)
        {
            cost += zone.GetAdditionalCost(point);
        }

        return cost;
    }
}
