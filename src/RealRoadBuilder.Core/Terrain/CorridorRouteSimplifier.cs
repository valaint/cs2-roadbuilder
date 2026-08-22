using System;
using System.Collections.Generic;
using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Terrain;

public static class CorridorRouteSimplifier
{
    private const double DirectionTolerance = 1e-9;
    private const double ElevationToleranceMeters = 0.05;

    public static IReadOnlyList<CorridorWaypoint> RemoveCollinearWaypoints(
        IReadOnlyList<CorridorWaypoint> waypoints)
    {
        if (waypoints == null)
        {
            throw new ArgumentNullException(nameof(waypoints));
        }

        if (waypoints.Count <= 2)
        {
            return waypoints;
        }

        List<CorridorWaypoint> simplified = new() { waypoints[0] };

        for (int index = 1; index < waypoints.Count - 1; index++)
        {
            CorridorWaypoint previous = simplified[simplified.Count - 1];
            CorridorWaypoint current = waypoints[index];
            CorridorWaypoint next = waypoints[index + 1];

            if (CanRemove(previous, current, next))
            {
                continue;
            }

            simplified.Add(current);
        }

        simplified.Add(waypoints[waypoints.Count - 1]);
        return simplified.AsReadOnly();
    }

    private static bool CanRemove(
        CorridorWaypoint previous,
        CorridorWaypoint current,
        CorridorWaypoint next)
    {
        PlanarVector firstVector = current.Position - previous.Position;
        PlanarVector secondVector = next.Position - current.Position;

        if (firstVector.Length <= DirectionTolerance || secondVector.Length <= DirectionTolerance)
        {
            return false;
        }

        double cross = Math.Abs(PlanarVector.Cross(firstVector, secondVector));
        double scale = firstVector.Length * secondVector.Length;
        if (cross > DirectionTolerance * scale)
        {
            return false;
        }

        if (PlanarVector.Dot(firstVector, secondVector) <= 0.0)
        {
            return false;
        }

        double fullDistance = previous.Position.DistanceTo(next.Position);
        if (fullDistance <= DirectionTolerance)
        {
            return false;
        }

        double currentDistance = previous.Position.DistanceTo(current.Position);
        double interpolation = currentDistance / fullDistance;
        double expectedElevation =
            previous.ElevationMeters +
            ((next.ElevationMeters - previous.ElevationMeters) * interpolation);

        return Math.Abs(current.ElevationMeters - expectedElevation) <= ElevationToleranceMeters;
    }
}
