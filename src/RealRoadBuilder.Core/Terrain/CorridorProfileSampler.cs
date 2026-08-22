using System;
using System.Collections.Generic;
using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Terrain;

public static class CorridorProfileSampler
{
    public static IReadOnlyList<CorridorProfileSample> Sample(
        CorridorRoute route,
        ITerrainSampler terrainSampler,
        double sampleIntervalMeters = 20.0)
    {
        if (route == null)
        {
            throw new ArgumentNullException(nameof(route));
        }

        if (terrainSampler == null)
        {
            throw new ArgumentNullException(nameof(terrainSampler));
        }

        if (sampleIntervalMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleIntervalMeters));
        }

        List<CorridorProfileSample> samples = new();
        double stationMeters = 0.0;
        double? previousElevationMeters = null;
        double? previousStationMeters = null;

        CorridorWaypoint firstWaypoint = route.Waypoints[0];
        double firstElevationMeters = terrainSampler.GetElevationMeters(firstWaypoint.Position);
        samples.Add(new CorridorProfileSample(
            0.0,
            firstWaypoint.Position,
            firstElevationMeters,
            0.0));
        previousElevationMeters = firstElevationMeters;
        previousStationMeters = 0.0;

        for (int segmentIndex = 1; segmentIndex < route.Waypoints.Count; segmentIndex++)
        {
            CorridorWaypoint previousWaypoint = route.Waypoints[segmentIndex - 1];
            CorridorWaypoint currentWaypoint = route.Waypoints[segmentIndex];
            PlanarVector segmentVector = currentWaypoint.Position - previousWaypoint.Position;
            double segmentLengthMeters = segmentVector.Length;

            if (segmentLengthMeters <= 1e-9)
            {
                continue;
            }

            int interiorSampleCount = (int)Math.Floor(segmentLengthMeters / sampleIntervalMeters);
            for (int sampleIndex = 1; sampleIndex <= interiorSampleCount; sampleIndex++)
            {
                double distanceAlongSegmentMeters = sampleIndex * sampleIntervalMeters;
                if (distanceAlongSegmentMeters >= segmentLengthMeters - 1e-9)
                {
                    break;
                }

                double interpolation = distanceAlongSegmentMeters / segmentLengthMeters;
                PlanarPoint samplePosition =
                    previousWaypoint.Position + (segmentVector * interpolation);
                double sampleStationMeters = stationMeters + distanceAlongSegmentMeters;
                double sampleElevationMeters = terrainSampler.GetElevationMeters(samplePosition);
                double gradePercent = CalculateGradePercent(
                    previousStationMeters!.Value,
                    previousElevationMeters!.Value,
                    sampleStationMeters,
                    sampleElevationMeters);

                samples.Add(new CorridorProfileSample(
                    sampleStationMeters,
                    samplePosition,
                    sampleElevationMeters,
                    gradePercent));

                previousStationMeters = sampleStationMeters;
                previousElevationMeters = sampleElevationMeters;
            }

            stationMeters += segmentLengthMeters;
            double endpointElevationMeters = terrainSampler.GetElevationMeters(currentWaypoint.Position);
            double endpointGradePercent = CalculateGradePercent(
                previousStationMeters!.Value,
                previousElevationMeters!.Value,
                stationMeters,
                endpointElevationMeters);

            samples.Add(new CorridorProfileSample(
                stationMeters,
                currentWaypoint.Position,
                endpointElevationMeters,
                endpointGradePercent));

            previousStationMeters = stationMeters;
            previousElevationMeters = endpointElevationMeters;
        }

        return samples.AsReadOnly();
    }

    private static double CalculateGradePercent(
        double startStationMeters,
        double startElevationMeters,
        double endStationMeters,
        double endElevationMeters)
    {
        double horizontalDistanceMeters = endStationMeters - startStationMeters;
        if (horizontalDistanceMeters <= 1e-9)
        {
            return 0.0;
        }

        return ((endElevationMeters - startElevationMeters) / horizontalDistanceMeters) * 100.0;
    }
}
