using System;
using System.Collections.Generic;
using RealRoadBuilder.Core.Design;
using RealRoadBuilder.Core.Evaluation;
using RealRoadBuilder.Core.Fitting;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Terrain;

namespace RealRoadBuilder.Core.Planning;

public sealed class HighwayPlanningEngine
{
    private readonly TerrainAwareCorridorRouter _router = new();
    private readonly CorridorVerticalProfileOptimizer _verticalProfileOptimizer = new();

    public HighwayPlanningResult Plan(
        PlanarPoint start,
        PlanarPoint end,
        ITerrainSampler terrainSampler,
        RoadDesignRule designRule,
        HighwayPlanningOptions? options = null,
        ICorridorConstraintProvider? initialConstraintProvider = null,
        IStructureRequirementProvider? structureRequirementProvider = null)
    {
        if (terrainSampler == null)
        {
            throw new ArgumentNullException(nameof(terrainSampler));
        }

        if (designRule == null)
        {
            throw new ArgumentNullException(nameof(designRule));
        }

        options ??= new HighwayPlanningOptions();
        ValidateOptions(options);

        List<HighwayPlanningAttempt> attempts = new();
        ICorridorConstraintProvider? currentConstraintProvider = initialConstraintProvider;

        for (int iteration = 1; iteration <= options.MaximumIterations; iteration++)
        {
            CorridorRoute route;
            try
            {
                route = _router.FindRoute(
                    start,
                    end,
                    terrainSampler,
                    designRule,
                    options.Search,
                    currentConstraintProvider,
                    options.Fit.Vertical.AllowExceptionalGrade);
            }
            catch (InvalidOperationException)
            {
                if (attempts.Count == 0)
                {
                    throw;
                }

                break;
            }

            IReadOnlyList<CorridorProfileSample> terrainProfile = CorridorProfileSampler.Sample(
                route,
                terrainSampler,
                options.PreliminaryProfileSampleIntervalMeters);

            CorridorVerticalProfile preliminaryVerticalProfile;
            try
            {
                preliminaryVerticalProfile = _verticalProfileOptimizer.Optimize(
                    terrainProfile,
                    designRule,
                    options.Fit.Vertical.AllowExceptionalGrade,
                    options.VerticalOptimizationIterations,
                    options.TerrainAttraction);
            }
            catch (InvalidOperationException exception)
            {
                attempts.Add(new HighwayPlanningAttempt(
                    iteration,
                    route,
                    preliminaryVerticalProfile: null,
                    fitResult: null,
                    constructionEvaluation: null,
                    comparisonScore: double.PositiveInfinity,
                    failureReason: exception.Message));

                currentConstraintProvider = CreateRoutePenaltyProvider(
                    route,
                    options.Feedback,
                    currentConstraintProvider,
                    "Preliminary vertical profile could not satisfy the selected engineering envelope.");
                continue;
            }

            EngineeringAlignmentFitResult fitResult = EngineeringAlignmentFitter.Fit(
                route,
                preliminaryVerticalProfile,
                designRule,
                options.Fit);

            if (!fitResult.IsSuccessful)
            {
                attempts.Add(new HighwayPlanningAttempt(
                    iteration,
                    route,
                    preliminaryVerticalProfile,
                    fitResult,
                    constructionEvaluation: null,
                    comparisonScore: double.PositiveInfinity,
                    failureReason: "The corridor could not be converted into a fully valid horizontal and vertical engineering alignment."));

                currentConstraintProvider = fitResult.Horizontal.IsSuccessful
                    ? CreateRoutePenaltyProvider(
                        route,
                        options.Feedback,
                        currentConstraintProvider,
                        "Vertical engineering fitting failed along this corridor.")
                    : RerouteFeedbackBuilder.Build(
                        constructionEvaluation: null,
                        horizontalFit: fitResult.Horizontal,
                        options: options.Feedback,
                        baseProvider: currentConstraintProvider);
                continue;
            }

            ConstructionEvaluationResult constructionEvaluation =
                FinalAlignmentConstructionEvaluator.Evaluate(
                    fitResult,
                    terrainSampler,
                    options.Construction,
                    structureRequirementProvider);

            double comparisonScore =
                constructionEvaluation.TotalRelativeCost +
                (route.TotalCost * options.RouteSearchCostWeight);

            attempts.Add(new HighwayPlanningAttempt(
                iteration,
                route,
                preliminaryVerticalProfile,
                fitResult,
                constructionEvaluation,
                comparisonScore));

            bool hasMajorStructures =
                constructionEvaluation.BridgeLengthMeters > 1e-6 ||
                constructionEvaluation.TunnelLengthMeters > 1e-6;

            if (options.StopWhenNoMajorStructures && !hasMajorStructures)
            {
                break;
            }

            currentConstraintProvider = RerouteFeedbackBuilder.Build(
                constructionEvaluation: constructionEvaluation,
                horizontalFit: fitResult.Horizontal,
                options: options.Feedback,
                baseProvider: currentConstraintProvider);
        }

        if (attempts.Count == 0)
        {
            throw new InvalidOperationException("Highway planning ended without producing a candidate attempt.");
        }

        return new HighwayPlanningResult(attempts);
    }

    private static void ValidateOptions(HighwayPlanningOptions options)
    {
        if (options.Search == null)
        {
            throw new ArgumentException("Search options are required.", nameof(options));
        }

        if (options.Fit == null)
        {
            throw new ArgumentException("Engineering fit options are required.", nameof(options));
        }

        if (options.Construction == null)
        {
            throw new ArgumentException("Construction evaluation options are required.", nameof(options));
        }

        if (options.Feedback == null)
        {
            throw new ArgumentException("Reroute feedback options are required.", nameof(options));
        }
    }

    private static ICorridorConstraintProvider CreateRoutePenaltyProvider(
        CorridorRoute route,
        RerouteFeedbackOptions options,
        ICorridorConstraintProvider? baseProvider,
        string reason)
    {
        List<RerouteFeedbackZone> zones = new();

        if (route.Waypoints.Count > 2)
        {
            for (int index = 1; index < route.Waypoints.Count - 1; index++)
            {
                zones.Add(new RerouteFeedbackZone(
                    route.Waypoints[index].Position,
                    options.ZoneRadiusMeters,
                    options.FitFailurePenalty,
                    blocked: false,
                    reason: reason));
            }
        }
        else
        {
            PlanarPoint midpoint = new(
                (route.Waypoints[0].Position.X + route.Waypoints[1].Position.X) * 0.5,
                (route.Waypoints[0].Position.Y + route.Waypoints[1].Position.Y) * 0.5);
            zones.Add(new RerouteFeedbackZone(
                midpoint,
                options.ZoneRadiusMeters,
                options.FitFailurePenalty,
                blocked: false,
                reason: reason));
        }

        return new RerouteFeedbackConstraintProvider(zones, baseProvider);
    }
}
