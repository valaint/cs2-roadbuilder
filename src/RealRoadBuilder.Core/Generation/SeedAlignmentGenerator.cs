using System;
using System.Collections.Generic;
using System.Linq;
using RealRoadBuilder.Core.Alignment;
using RealRoadBuilder.Core.Design;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Scoring;
using RealRoadBuilder.Core.Validation;

namespace RealRoadBuilder.Core.Generation;

/// <summary>
/// Produces simple terrain-independent seed alignments between two points.
/// These seeds are intended to become the starting population for the later
/// terrain/collision corridor search; they are not the final highway solver.
/// </summary>
public static class SeedAlignmentGenerator
{
    private const double MinimumEndpointSeparationMeters = 1.0;
    private const double BypassOffsetFraction = 0.20;
    private const double RadiusFitSafetyFactor = 0.95;

    public static IReadOnlyList<AlignmentCandidate> Generate(
        PlanarPoint start,
        PlanarPoint end,
        RoadDesignRule designRule,
        AlignmentGenerationMode generationMode = AlignmentGenerationMode.Balanced,
        bool allowExceptionalCurveRadius = false)
    {
        if (designRule == null)
        {
            throw new ArgumentNullException(nameof(designRule));
        }

        PlanarVector baseline = end - start;
        double endpointSeparationMeters = baseline.Length;
        if (endpointSeparationMeters < MinimumEndpointSeparationMeters)
        {
            throw new ArgumentException(
                $"Start and end must be at least {MinimumEndpointSeparationMeters:0.###} m apart.");
        }

        List<AlignmentCandidate> candidates = new();

        HorizontalAlignment straightAlignment = new(
            new HorizontalAlignmentElement[]
            {
                new TangentElement(start, end),
            });
        candidates.Add(CreateCandidate(
            "Direct",
            straightAlignment,
            designRule,
            generationMode,
            allowExceptionalCurveRadius));

        PlanarVector baselineDirection = baseline.Normalized();
        PlanarVector leftNormal = baselineDirection.LeftNormal();
        PlanarPoint midpoint = start + (baselineDirection * (endpointSeparationMeters / 2.0));
        double bypassOffsetMeters = endpointSeparationMeters * BypassOffsetFraction;

        TryAddBypassCandidate(
            candidates,
            "Left bypass",
            start,
            midpoint + (leftNormal * bypassOffsetMeters),
            end,
            designRule,
            generationMode,
            allowExceptionalCurveRadius);

        TryAddBypassCandidate(
            candidates,
            "Right bypass",
            start,
            midpoint - (leftNormal * bypassOffsetMeters),
            end,
            designRule,
            generationMode,
            allowExceptionalCurveRadius);

        return candidates
            .OrderBy(candidate => candidate.Score.TotalCost)
            .ToList()
            .AsReadOnly();
    }

    private static void TryAddBypassCandidate(
        ICollection<AlignmentCandidate> candidates,
        string name,
        PlanarPoint start,
        PlanarPoint pointOfIntersection,
        PlanarPoint end,
        RoadDesignRule designRule,
        AlignmentGenerationMode generationMode,
        bool allowExceptionalCurveRadius)
    {
        double requiredRadiusMeters = allowExceptionalCurveRadius
            ? designRule.ExceptionalMinimumCurveRadiusMeters
            : designRule.MinimumCurveRadiusMeters;
        double maximumFittingRadiusMeters = FilletCurveBuilder.CalculateMaximumFittingRadius(
            start,
            pointOfIntersection,
            end);
        double safeMaximumRadiusMeters = maximumFittingRadiusMeters * RadiusFitSafetyFactor;

        if (safeMaximumRadiusMeters < requiredRadiusMeters)
        {
            return;
        }

        double radiusMultiplier = generationMode switch
        {
            AlignmentGenerationMode.Shortest => 1.00,
            AlignmentGenerationMode.Balanced => 1.50,
            AlignmentGenerationMode.HighStandard => 2.25,
            _ => throw new ArgumentOutOfRangeException(nameof(generationMode)),
        };

        double preferredRadiusMeters = requiredRadiusMeters * radiusMultiplier;
        double selectedRadiusMeters = Math.Min(preferredRadiusMeters, safeMaximumRadiusMeters);

        HorizontalAlignment alignment = FilletCurveBuilder.Build(
            start,
            pointOfIntersection,
            end,
            selectedRadiusMeters);

        candidates.Add(CreateCandidate(
            name,
            alignment,
            designRule,
            generationMode,
            allowExceptionalCurveRadius));
    }

    private static AlignmentCandidate CreateCandidate(
        string name,
        HorizontalAlignment alignment,
        RoadDesignRule designRule,
        AlignmentGenerationMode generationMode,
        bool allowExceptionalCurveRadius)
    {
        AlignmentValidationResult validation = HorizontalAlignmentValidator.Validate(
            alignment,
            designRule,
            allowExceptionalCurveRadius);
        AlignmentScore score = AlignmentScorer.Score(
            alignment,
            designRule,
            generationMode);

        return new AlignmentCandidate(name, alignment, validation, score);
    }
}
