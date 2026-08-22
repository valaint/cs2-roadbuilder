using System;
using System.Collections.Generic;
using RealRoadBuilder.Core.Terrain;
using RealRoadBuilder.Core.Vertical;

namespace RealRoadBuilder.Core.Fitting;

public static class VerticalProfileControlPointReducer
{
    public static IReadOnlyList<ProfilePoint> Reduce(
        CorridorVerticalProfile profile,
        double targetLengthMeters,
        double elevationToleranceMeters)
    {
        if (profile == null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        if (double.IsNaN(targetLengthMeters) ||
            double.IsInfinity(targetLengthMeters) ||
            targetLengthMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetLengthMeters));
        }

        if (double.IsNaN(elevationToleranceMeters) ||
            double.IsInfinity(elevationToleranceMeters) ||
            elevationToleranceMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(elevationToleranceMeters));
        }

        IReadOnlyList<DesignedProfileSample> samples = profile.Samples;
        double sourceStartStationMeters = samples[0].StationMeters;
        double sourceEndStationMeters = samples[samples.Count - 1].StationMeters;
        double sourceLengthMeters = sourceEndStationMeters - sourceStartStationMeters;

        if (sourceLengthMeters <= 1e-9)
        {
            throw new ArgumentException("The sampled vertical profile has no station length.", nameof(profile));
        }

        double stationScale = targetLengthMeters / sourceLengthMeters;
        List<ProfilePoint> mappedPoints = new(samples.Count);

        foreach (DesignedProfileSample sample in samples)
        {
            double mappedStationMeters =
                (sample.StationMeters - sourceStartStationMeters) * stationScale;
            mappedPoints.Add(new ProfilePoint(
                mappedStationMeters,
                sample.DesignElevationMeters));
        }

        if (mappedPoints.Count == 2)
        {
            return mappedPoints.AsReadOnly();
        }

        bool[] keep = new bool[mappedPoints.Count];
        keep[0] = true;
        keep[mappedPoints.Count - 1] = true;

        Stack<(int StartIndex, int EndIndex)> pendingSegments = new();
        pendingSegments.Push((0, mappedPoints.Count - 1));

        while (pendingSegments.Count > 0)
        {
            (int startIndex, int endIndex) = pendingSegments.Pop();
            if (endIndex <= startIndex + 1)
            {
                continue;
            }

            ProfilePoint start = mappedPoints[startIndex];
            ProfilePoint end = mappedPoints[endIndex];
            double stationSpanMeters = end.StationMeters - start.StationMeters;
            double maximumElevationErrorMeters = -1.0;
            int splitIndex = -1;

            for (int index = startIndex + 1; index < endIndex; index++)
            {
                ProfilePoint point = mappedPoints[index];
                double fraction = stationSpanMeters <= 1e-12
                    ? 0.0
                    : (point.StationMeters - start.StationMeters) / stationSpanMeters;
                double interpolatedElevationMeters =
                    start.ElevationMeters +
                    ((end.ElevationMeters - start.ElevationMeters) * fraction);
                double errorMeters = Math.Abs(
                    point.ElevationMeters - interpolatedElevationMeters);

                if (errorMeters > maximumElevationErrorMeters)
                {
                    maximumElevationErrorMeters = errorMeters;
                    splitIndex = index;
                }
            }

            if (splitIndex < 0 || maximumElevationErrorMeters <= elevationToleranceMeters)
            {
                continue;
            }

            keep[splitIndex] = true;
            pendingSegments.Push((startIndex, splitIndex));
            pendingSegments.Push((splitIndex, endIndex));
        }

        List<ProfilePoint> reduced = new();
        for (int index = 0; index < mappedPoints.Count; index++)
        {
            if (keep[index])
            {
                reduced.Add(mappedPoints[index]);
            }
        }

        return reduced.AsReadOnly();
    }
}
