using System;
using System.Collections.Generic;
using System.Linq;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Terrain;

namespace RealRoadBuilder.Core.Fitting;

public static class CorridorControlPointReducer
{
    public static IReadOnlyList<PlanarPoint> Reduce(
        CorridorRoute route,
        double toleranceMeters)
    {
        if (route == null)
        {
            throw new ArgumentNullException(nameof(route));
        }

        return Reduce(
            route.Waypoints.Select(waypoint => waypoint.Position).ToList(),
            toleranceMeters);
    }

    public static IReadOnlyList<PlanarPoint> Reduce(
        IReadOnlyList<PlanarPoint> points,
        double toleranceMeters)
    {
        if (points == null)
        {
            throw new ArgumentNullException(nameof(points));
        }

        if (points.Count < 2)
        {
            throw new ArgumentException("At least two points are required.", nameof(points));
        }

        if (double.IsNaN(toleranceMeters) ||
            double.IsInfinity(toleranceMeters) ||
            toleranceMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(toleranceMeters));
        }

        if (points.Count == 2)
        {
            return new[] { points[0], points[1] };
        }

        bool[] keep = new bool[points.Count];
        keep[0] = true;
        keep[points.Count - 1] = true;

        Stack<(int StartIndex, int EndIndex)> pendingSegments = new();
        pendingSegments.Push((0, points.Count - 1));

        while (pendingSegments.Count > 0)
        {
            (int startIndex, int endIndex) = pendingSegments.Pop();
            if (endIndex <= startIndex + 1)
            {
                continue;
            }

            double maximumDistanceMeters = -1.0;
            int splitIndex = -1;

            for (int index = startIndex + 1; index < endIndex; index++)
            {
                double distanceMeters = DistanceToSegment(
                    points[index],
                    points[startIndex],
                    points[endIndex]);

                if (distanceMeters > maximumDistanceMeters)
                {
                    maximumDistanceMeters = distanceMeters;
                    splitIndex = index;
                }
            }

            if (splitIndex < 0 || maximumDistanceMeters <= toleranceMeters)
            {
                continue;
            }

            keep[splitIndex] = true;
            pendingSegments.Push((startIndex, splitIndex));
            pendingSegments.Push((splitIndex, endIndex));
        }

        List<PlanarPoint> reduced = new();
        for (int index = 0; index < points.Count; index++)
        {
            if (keep[index])
            {
                reduced.Add(points[index]);
            }
        }

        return reduced.AsReadOnly();
    }

    private static double DistanceToSegment(
        PlanarPoint point,
        PlanarPoint segmentStart,
        PlanarPoint segmentEnd)
    {
        PlanarVector segment = segmentEnd - segmentStart;
        double lengthSquared = segment.LengthSquared;

        if (lengthSquared <= 1e-12)
        {
            return point.DistanceTo(segmentStart);
        }

        PlanarVector fromStart = point - segmentStart;
        double projection = PlanarVector.Dot(fromStart, segment) / lengthSquared;
        projection = Math.Max(0.0, Math.Min(1.0, projection));

        PlanarPoint closest = segmentStart + (segment * projection);
        return point.DistanceTo(closest);
    }
}
