using System;
using System.Collections.Generic;
using System.Linq;
using RealRoadBuilder.Core.Evaluation;
using RealRoadBuilder.Core.Fitting;
using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Terrain;

public static class RerouteFeedbackBuilder
{
    public static RerouteFeedbackConstraintProvider Build(
        ConstructionEvaluationResult? constructionEvaluation,
        HorizontalAlignmentFitResult? horizontalFit,
        RerouteFeedbackOptions? options = null,
        ICorridorConstraintProvider? baseProvider = null)
    {
        options ??= new RerouteFeedbackOptions();
        List<RerouteFeedbackZone> zones = new();

        if (constructionEvaluation != null)
        {
            AddStructureZones(constructionEvaluation, options, zones);
        }

        if (horizontalFit != null && !horizontalFit.IsSuccessful)
        {
            AddHorizontalFitFailureZones(horizontalFit, options, zones);
        }

        return new RerouteFeedbackConstraintProvider(zones, baseProvider);
    }

    private static void AddStructureZones(
        ConstructionEvaluationResult evaluation,
        RerouteFeedbackOptions options,
        ICollection<RerouteFeedbackZone> zones)
    {
        foreach (ConstructionSegment segment in evaluation.Segments)
        {
            double penalty = segment.SectionType switch
            {
                ConstructionSectionType.Bridge => options.BridgePenalty,
                ConstructionSectionType.Tunnel => options.TunnelPenalty,
                _ => 0.0,
            };

            if (penalty <= 0.0)
            {
                continue;
            }

            double lastAddedStationMeters = double.NegativeInfinity;
            bool addedAny = false;

            foreach (AlignmentTerrainSample sample in evaluation.Samples)
            {
                if (sample.StationMeters < segment.StartStationMeters - 1e-6 ||
                    sample.StationMeters > segment.EndStationMeters + 1e-6)
                {
                    continue;
                }

                if (addedAny &&
                    sample.StationMeters - lastAddedStationMeters < options.MinimumZoneSpacingMeters)
                {
                    continue;
                }

                zones.Add(new RerouteFeedbackZone(
                    sample.Position,
                    options.ZoneRadiusMeters,
                    penalty,
                    blocked: false,
                    reason: $"Avoid costly {segment.SectionType.ToString().ToLowerInvariant()} candidate."));
                lastAddedStationMeters = sample.StationMeters;
                addedAny = true;
            }

            if (!addedAny)
            {
                PlanarPoint midpoint = new(
                    (segment.StartPosition.X + segment.EndPosition.X) * 0.5,
                    (segment.StartPosition.Y + segment.EndPosition.Y) * 0.5);
                zones.Add(new RerouteFeedbackZone(
                    midpoint,
                    options.ZoneRadiusMeters,
                    penalty,
                    blocked: false,
                    reason: $"Avoid costly {segment.SectionType.ToString().ToLowerInvariant()} candidate."));
            }
        }
    }

    private static void AddHorizontalFitFailureZones(
        HorizontalAlignmentFitResult horizontalFit,
        RerouteFeedbackOptions options,
        ICollection<RerouteFeedbackZone> zones)
    {
        List<PlanarPoint> localizedDiagnosticPositions = horizontalFit.Diagnostics
            .Where(diagnostic =>
                diagnostic.Severity == EngineeringDiagnosticSeverity.Error &&
                diagnostic.Position.HasValue)
            .Select(diagnostic => diagnostic.Position!.Value)
            .ToList();

        if (localizedDiagnosticPositions.Count > 0)
        {
            foreach (PlanarPoint position in localizedDiagnosticPositions)
            {
                zones.Add(CreateFitFailureZone(position, options));
            }

            return;
        }

        // Older/fallback diagnostics may not carry locations. In that case the
        // interior reduced PIs are the safest available approximation of where
        // a geometric fit failed or adjacent curve trims collided.
        for (int index = 1; index < horizontalFit.ControlPoints.Count - 1; index++)
        {
            zones.Add(CreateFitFailureZone(horizontalFit.ControlPoints[index], options));
        }
    }

    private static RerouteFeedbackZone CreateFitFailureZone(
        PlanarPoint position,
        RerouteFeedbackOptions options)
    {
        return new RerouteFeedbackZone(
            position,
            options.ZoneRadiusMeters,
            options.FitFailurePenalty,
            blocked: options.BlockHorizontalFitFailures,
            reason: "Discourage a corridor location that could not fit standards-valid horizontal geometry.");
    }
}
