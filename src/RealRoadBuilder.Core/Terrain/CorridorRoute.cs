using System;
using System.Collections.Generic;
using System.Linq;

namespace RealRoadBuilder.Core.Terrain;

public sealed class CorridorRoute
{
    public CorridorRoute(
        IEnumerable<CorridorWaypoint> waypoints,
        double totalCost,
        int expandedStates)
    {
        if (waypoints == null)
        {
            throw new ArgumentNullException(nameof(waypoints));
        }

        List<CorridorWaypoint> materializedWaypoints = waypoints.ToList();
        if (materializedWaypoints.Count < 2)
        {
            throw new ArgumentException("A corridor route requires at least two waypoints.", nameof(waypoints));
        }

        if (double.IsNaN(totalCost) || double.IsInfinity(totalCost) || totalCost < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCost));
        }

        if (expandedStates < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expandedStates));
        }

        Waypoints = materializedWaypoints.AsReadOnly();
        TotalCost = totalCost;
        ExpandedStates = expandedStates;

        double totalLengthMeters = 0.0;
        double maximumAbsoluteGradePercent = 0.0;

        for (int index = 1; index < materializedWaypoints.Count; index++)
        {
            CorridorWaypoint previous = materializedWaypoints[index - 1];
            CorridorWaypoint current = materializedWaypoints[index];
            double horizontalDistanceMeters = previous.Position.DistanceTo(current.Position);

            if (horizontalDistanceMeters <= 1e-9)
            {
                continue;
            }

            totalLengthMeters += horizontalDistanceMeters;
            double gradePercent =
                ((current.ElevationMeters - previous.ElevationMeters) / horizontalDistanceMeters) * 100.0;
            maximumAbsoluteGradePercent = Math.Max(maximumAbsoluteGradePercent, Math.Abs(gradePercent));
        }

        TotalLengthMeters = totalLengthMeters;
        MaximumAbsoluteGradePercent = maximumAbsoluteGradePercent;
    }

    public IReadOnlyList<CorridorWaypoint> Waypoints { get; }

    public double TotalCost { get; }

    public int ExpandedStates { get; }

    public double TotalLengthMeters { get; }

    public double MaximumAbsoluteGradePercent { get; }
}
