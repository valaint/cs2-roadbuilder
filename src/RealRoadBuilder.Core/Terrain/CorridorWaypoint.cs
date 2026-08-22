using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Terrain;

public readonly struct CorridorWaypoint
{
    public CorridorWaypoint(PlanarPoint position, double elevationMeters)
    {
        Position = position;
        ElevationMeters = elevationMeters;
    }

    public PlanarPoint Position { get; }

    public double ElevationMeters { get; }
}
