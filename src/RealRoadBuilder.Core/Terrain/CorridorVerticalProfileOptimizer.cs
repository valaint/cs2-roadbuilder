using System;
using System.Collections.Generic;
using RealRoadBuilder.Core.Design;

namespace RealRoadBuilder.Core.Terrain;

/// <summary>
/// Produces a preliminary smooth design profile from sampled terrain.
/// The optimizer enforces grade limits and a finite-difference approximation
/// of the selected crest/sag radius limits. The result is intended for
/// corridor evaluation and preview; final construction geometry must still be
/// converted into explicit parabolic vertical curves and validated.
/// </summary>
public sealed class CorridorVerticalProfileOptimizer
{
    public CorridorVerticalProfile Optimize(
        IReadOnlyList<CorridorProfileSample> terrainProfile,
        RoadDesignRule designRule,
        bool allowExceptionalGrade = false,
        int iterations = 300,
        double terrainAttraction = 0.18)
    {
        if (terrainProfile == null)
        {
            throw new ArgumentNullException(nameof(terrainProfile));
        }

        if (designRule == null)
        {
            throw new ArgumentNullException(nameof(designRule));
        }

        if (terrainProfile.Count < 2)
        {
            throw new ArgumentException("A terrain profile requires at least two samples.", nameof(terrainProfile));
        }

        if (iterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations));
        }

        if (terrainAttraction < 0.0 || terrainAttraction > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(terrainAttraction));
        }

        ValidateStations(terrainProfile);

        double maximumGradePercent = allowExceptionalGrade
            ? designRule.ExceptionalMaximumGradePercent
            : designRule.MaximumGradePercent;
        double maximumGradeDecimal = maximumGradePercent / 100.0;

        double overallGradeDecimal =
            (terrainProfile[terrainProfile.Count - 1].TerrainElevationMeters -
             terrainProfile[0].TerrainElevationMeters) /
            (terrainProfile[terrainProfile.Count - 1].StationMeters -
             terrainProfile[0].StationMeters);

        if (Math.Abs(overallGradeDecimal) > maximumGradeDecimal + 1e-12)
        {
            throw new InvalidOperationException(
                $"The fixed corridor endpoints require an average grade of {Math.Abs(overallGradeDecimal) * 100.0:0.###}%, above the allowed {maximumGradePercent:0.###}%.");
        }

        double[] terrainElevations = new double[terrainProfile.Count];
        double[] designElevations = new double[terrainProfile.Count];
        double[] workingElevations = new double[terrainProfile.Count];

        for (int index = 0; index < terrainProfile.Count; index++)
        {
            double terrainElevationMeters = terrainProfile[index].TerrainElevationMeters;
            terrainElevations[index] = terrainElevationMeters;
            designElevations[index] = terrainElevationMeters;
            workingElevations[index] = terrainElevationMeters;
        }

        double startElevationMeters = terrainElevations[0];
        double endElevationMeters = terrainElevations[terrainElevations.Length - 1];

        for (int iteration = 0; iteration < iterations; iteration++)
        {
            workingElevations[0] = startElevationMeters;
            workingElevations[workingElevations.Length - 1] = endElevationMeters;

            for (int index = 1; index < terrainProfile.Count - 1; index++)
            {
                double previousStationMeters = terrainProfile[index - 1].StationMeters;
                double currentStationMeters = terrainProfile[index].StationMeters;
                double nextStationMeters = terrainProfile[index + 1].StationMeters;

                double previousWeight = nextStationMeters - currentStationMeters;
                double nextWeight = currentStationMeters - previousStationMeters;
                double totalSpanMeters = nextStationMeters - previousStationMeters;

                double linearNeighborElevation =
                    ((designElevations[index - 1] * previousWeight) +
                     (designElevations[index + 1] * nextWeight)) /
                    totalSpanMeters;

                workingElevations[index] =
                    ((1.0 - terrainAttraction) * linearNeighborElevation) +
                    (terrainAttraction * terrainElevations[index]);
            }

            Array.Copy(workingElevations, designElevations, designElevations.Length);
            designElevations[0] = startElevationMeters;
            designElevations[designElevations.Length - 1] = endElevationMeters;

            ProjectGradeLimitsForward(
                terrainProfile,
                designElevations,
                maximumGradeDecimal);
            designElevations[designElevations.Length - 1] = endElevationMeters;

            ProjectGradeLimitsBackward(
                terrainProfile,
                designElevations,
                maximumGradeDecimal);
            designElevations[0] = startElevationMeters;

            ProjectApproximateCurvatureLimits(
                terrainProfile,
                designElevations,
                designRule.MinimumCrestVerticalCurveRadiusMeters,
                designRule.MinimumSagVerticalCurveRadiusMeters);

            designElevations[0] = startElevationMeters;
            designElevations[designElevations.Length - 1] = endElevationMeters;
        }

        double maximumObservedGradePercent = GetMaximumAbsoluteGradePercent(
            terrainProfile,
            designElevations);

        if (maximumObservedGradePercent > maximumGradePercent + 1e-5)
        {
            throw new InvalidOperationException(
                $"The preliminary profile optimizer did not converge within the {maximumGradePercent:0.###}% grade limit. Observed {maximumObservedGradePercent:0.###}%.");
        }

        ValidateApproximateCurvatureEnvelope(
            terrainProfile,
            designElevations,
            designRule.MinimumCrestVerticalCurveRadiusMeters,
            designRule.MinimumSagVerticalCurveRadiusMeters);

        List<DesignedProfileSample> samples = new(terrainProfile.Count);
        for (int index = 0; index < terrainProfile.Count; index++)
        {
            double gradePercent = 0.0;
            if (index > 0)
            {
                gradePercent = CalculateGradePercent(
                    terrainProfile[index - 1].StationMeters,
                    designElevations[index - 1],
                    terrainProfile[index].StationMeters,
                    designElevations[index]);
            }

            samples.Add(new DesignedProfileSample(
                terrainProfile[index].StationMeters,
                terrainElevations[index],
                designElevations[index],
                gradePercent));
        }

        return new CorridorVerticalProfile(samples);
    }

    private static void ValidateStations(IReadOnlyList<CorridorProfileSample> terrainProfile)
    {
        for (int index = 1; index < terrainProfile.Count; index++)
        {
            if (terrainProfile[index].StationMeters <= terrainProfile[index - 1].StationMeters)
            {
                throw new ArgumentException(
                    "Terrain profile stations must be strictly increasing.",
                    nameof(terrainProfile));
            }
        }
    }

    private static void ProjectGradeLimitsForward(
        IReadOnlyList<CorridorProfileSample> terrainProfile,
        double[] elevations,
        double maximumGradeDecimal)
    {
        for (int index = 1; index < elevations.Length - 1; index++)
        {
            double distanceMeters =
                terrainProfile[index].StationMeters -
                terrainProfile[index - 1].StationMeters;
            double maximumElevationChangeMeters = maximumGradeDecimal * distanceMeters;
            double minimumElevationMeters = elevations[index - 1] - maximumElevationChangeMeters;
            double maximumElevationMeters = elevations[index - 1] + maximumElevationChangeMeters;

            elevations[index] = Math.Max(
                minimumElevationMeters,
                Math.Min(maximumElevationMeters, elevations[index]));
        }
    }

    private static void ProjectGradeLimitsBackward(
        IReadOnlyList<CorridorProfileSample> terrainProfile,
        double[] elevations,
        double maximumGradeDecimal)
    {
        for (int index = elevations.Length - 2; index > 0; index--)
        {
            double distanceMeters =
                terrainProfile[index + 1].StationMeters -
                terrainProfile[index].StationMeters;
            double maximumElevationChangeMeters = maximumGradeDecimal * distanceMeters;
            double minimumElevationMeters = elevations[index + 1] - maximumElevationChangeMeters;
            double maximumElevationMeters = elevations[index + 1] + maximumElevationChangeMeters;

            elevations[index] = Math.Max(
                minimumElevationMeters,
                Math.Min(maximumElevationMeters, elevations[index]));
        }
    }

    private static void ProjectApproximateCurvatureLimits(
        IReadOnlyList<CorridorProfileSample> terrainProfile,
        double[] elevations,
        double minimumCrestRadiusMeters,
        double minimumSagRadiusMeters)
    {
        for (int index = 1; index < elevations.Length - 1; index++)
        {
            double previousDistanceMeters =
                terrainProfile[index].StationMeters -
                terrainProfile[index - 1].StationMeters;
            double nextDistanceMeters =
                terrainProfile[index + 1].StationMeters -
                terrainProfile[index].StationMeters;

            double incomingGradeDecimal =
                (elevations[index] - elevations[index - 1]) /
                previousDistanceMeters;
            double outgoingGradeDecimal =
                (elevations[index + 1] - elevations[index]) /
                nextDistanceMeters;
            double gradeChangeDecimal = outgoingGradeDecimal - incomingGradeDecimal;

            if (Math.Abs(gradeChangeDecimal) <= 1e-12)
            {
                continue;
            }

            double effectiveDistanceMeters =
                (previousDistanceMeters + nextDistanceMeters) * 0.5;
            double requiredRadiusMeters = gradeChangeDecimal < 0.0
                ? minimumCrestRadiusMeters
                : minimumSagRadiusMeters;
            double maximumGradeChangeDecimal =
                effectiveDistanceMeters / requiredRadiusMeters;

            if (Math.Abs(gradeChangeDecimal) <= maximumGradeChangeDecimal)
            {
                continue;
            }

            double desiredGradeChangeDecimal =
                Math.Sign(gradeChangeDecimal) * maximumGradeChangeDecimal;
            double elevationCoefficient =
                (1.0 / previousDistanceMeters) +
                (1.0 / nextDistanceMeters);

            elevations[index] +=
                (gradeChangeDecimal - desiredGradeChangeDecimal) /
                elevationCoefficient;
        }
    }

    private static void ValidateApproximateCurvatureEnvelope(
        IReadOnlyList<CorridorProfileSample> terrainProfile,
        double[] elevations,
        double minimumCrestRadiusMeters,
        double minimumSagRadiusMeters)
    {
        const double tolerance = 1e-5;

        for (int index = 1; index < elevations.Length - 1; index++)
        {
            double previousDistanceMeters =
                terrainProfile[index].StationMeters -
                terrainProfile[index - 1].StationMeters;
            double nextDistanceMeters =
                terrainProfile[index + 1].StationMeters -
                terrainProfile[index].StationMeters;

            double incomingGradeDecimal =
                (elevations[index] - elevations[index - 1]) /
                previousDistanceMeters;
            double outgoingGradeDecimal =
                (elevations[index + 1] - elevations[index]) /
                nextDistanceMeters;
            double gradeChangeDecimal = outgoingGradeDecimal - incomingGradeDecimal;

            if (Math.Abs(gradeChangeDecimal) <= 1e-12)
            {
                continue;
            }

            double effectiveDistanceMeters =
                (previousDistanceMeters + nextDistanceMeters) * 0.5;
            double requiredRadiusMeters = gradeChangeDecimal < 0.0
                ? minimumCrestRadiusMeters
                : minimumSagRadiusMeters;
            double maximumGradeChangeDecimal =
                effectiveDistanceMeters / requiredRadiusMeters;

            if (Math.Abs(gradeChangeDecimal) > maximumGradeChangeDecimal + tolerance)
            {
                throw new InvalidOperationException(
                    "The preliminary profile optimizer did not converge inside the selected approximate crest/sag curvature envelope.");
            }
        }
    }

    private static double GetMaximumAbsoluteGradePercent(
        IReadOnlyList<CorridorProfileSample> terrainProfile,
        double[] elevations)
    {
        double maximumAbsoluteGradePercent = 0.0;

        for (int index = 1; index < elevations.Length; index++)
        {
            double gradePercent = CalculateGradePercent(
                terrainProfile[index - 1].StationMeters,
                elevations[index - 1],
                terrainProfile[index].StationMeters,
                elevations[index]);
            maximumAbsoluteGradePercent = Math.Max(
                maximumAbsoluteGradePercent,
                Math.Abs(gradePercent));
        }

        return maximumAbsoluteGradePercent;
    }

    private static double CalculateGradePercent(
        double startStationMeters,
        double startElevationMeters,
        double endStationMeters,
        double endElevationMeters)
    {
        return 100.0 *
            (endElevationMeters - startElevationMeters) /
            (endStationMeters - startStationMeters);
    }
}
