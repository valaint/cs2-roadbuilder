using System;
using System.Collections.Generic;
using RealRoadBuilder.Core.Alignment;
using RealRoadBuilder.Core.Terrain;
using RealRoadBuilder.Core.Vertical;

namespace RealRoadBuilder.Core.Evaluation;

public static class FinalAlignmentTerrainSampler
{
    private const double AlignmentLengthToleranceMeters = 1e-3;

    public static IReadOnlyList<AlignmentTerrainSample> Sample(
        HorizontalAlignment horizontalAlignment,
        VerticalAlignment verticalAlignment,
        ITerrainSampler terrainSampler,
        double intervalMeters = 10.0)
    {
        if (horizontalAlignment == null)
        {
            throw new ArgumentNullException(nameof(horizontalAlignment));
        }

        if (verticalAlignment == null)
        {
            throw new ArgumentNullException(nameof(verticalAlignment));
        }

        if (terrainSampler == null)
        {
            throw new ArgumentNullException(nameof(terrainSampler));
        }

        if (double.IsNaN(intervalMeters) || double.IsInfinity(intervalMeters) || intervalMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalMeters));
        }

        if (Math.Abs(verticalAlignment.StartStationMeters) > AlignmentLengthToleranceMeters)
        {
            throw new ArgumentException(
                "The final vertical alignment must start at station 0 for terrain evaluation.",
                nameof(verticalAlignment));
        }

        if (Math.Abs(horizontalAlignment.TotalLengthMeters - verticalAlignment.TotalLengthMeters) >
            AlignmentLengthToleranceMeters)
        {
            throw new ArgumentException(
                "Horizontal and vertical alignment lengths must match before terrain evaluation.");
        }

        double totalLengthMeters = horizontalAlignment.TotalLengthMeters;
        List<AlignmentTerrainSample> samples = new();

        for (double stationMeters = 0.0;
             stationMeters < totalLengthMeters;
             stationMeters += intervalMeters)
        {
            samples.Add(CreateSample(
                horizontalAlignment,
                verticalAlignment,
                terrainSampler,
                stationMeters));
        }

        samples.Add(CreateSample(
            horizontalAlignment,
            verticalAlignment,
            terrainSampler,
            totalLengthMeters));

        return samples.AsReadOnly();
    }

    private static AlignmentTerrainSample CreateSample(
        HorizontalAlignment horizontalAlignment,
        VerticalAlignment verticalAlignment,
        ITerrainSampler terrainSampler,
        double stationMeters)
    {
        var position = HorizontalAlignmentStationSampler.PointAt(
            horizontalAlignment,
            stationMeters);
        VerticalAlignmentElement verticalElement = FindVerticalElement(
            verticalAlignment,
            stationMeters);
        double designElevationMeters = verticalElement.ElevationAt(stationMeters);
        double designGradePercent = verticalElement.GradePercentAt(stationMeters);
        double terrainElevationMeters = terrainSampler.GetElevationMeters(position);

        return new AlignmentTerrainSample(
            stationMeters,
            position,
            terrainElevationMeters,
            designElevationMeters,
            designGradePercent);
    }

    private static VerticalAlignmentElement FindVerticalElement(
        VerticalAlignment alignment,
        double stationMeters)
    {
        for (int index = 0; index < alignment.Elements.Count; index++)
        {
            VerticalAlignmentElement element = alignment.Elements[index];
            if (stationMeters <= element.EndStationMeters + AlignmentLengthToleranceMeters)
            {
                double clampedStationMeters = Math.Max(
                    element.StartStationMeters,
                    Math.Min(element.EndStationMeters, stationMeters));

                if (Math.Abs(clampedStationMeters - stationMeters) <= AlignmentLengthToleranceMeters)
                {
                    return element;
                }
            }
        }

        return alignment.Elements[alignment.Elements.Count - 1];
    }
}
