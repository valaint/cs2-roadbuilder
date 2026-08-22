using System;
using System.Collections.Generic;
using RealRoadBuilder.Core.Alignment;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Terrain;
using RealRoadBuilder.Core.Vertical;

namespace RealRoadBuilder.Core.Evaluation;

public static class ConstructionEvaluator
{
    public static ConstructionEvaluationResult Evaluate(
        HorizontalAlignment horizontalAlignment,
        VerticalAlignment verticalAlignment,
        ITerrainSampler terrainSampler,
        ConstructionEvaluationOptions? options = null,
        IStructureRequirementProvider? requirementProvider = null)
    {
        options ??= new ConstructionEvaluationOptions();

        IReadOnlyList<AlignmentTerrainSample> samples = FinalAlignmentTerrainSampler.Sample(
            horizontalAlignment,
            verticalAlignment,
            terrainSampler,
            options.SampleIntervalMeters);

        List<IntervalDraft> drafts = BuildIntervalDrafts(
            samples,
            options,
            requirementProvider);

        NormalizeShortStructureRuns(
            drafts,
            ConstructionSectionType.Bridge,
            options.MinimumBridgeRunMeters,
            options);
        NormalizeShortStructureRuns(
            drafts,
            ConstructionSectionType.Tunnel,
            options.MinimumTunnelRunMeters,
            options);

        IReadOnlyList<ConstructionSegment> segments = MergeDrafts(drafts, options);
        return new ConstructionEvaluationResult(samples, segments);
    }

    private static List<IntervalDraft> BuildIntervalDrafts(
        IReadOnlyList<AlignmentTerrainSample> samples,
        ConstructionEvaluationOptions options,
        IStructureRequirementProvider? requirementProvider)
    {
        List<IntervalDraft> drafts = new(samples.Count - 1);

        for (int index = 1; index < samples.Count; index++)
        {
            AlignmentTerrainSample start = samples[index - 1];
            AlignmentTerrainSample end = samples[index];
            double lengthMeters = end.StationMeters - start.StationMeters;
            if (lengthMeters <= 0.0)
            {
                throw new InvalidOperationException("Final alignment terrain samples must have strictly increasing stations.");
            }

            PlanarPoint midpoint = new(
                (start.Position.X + end.Position.X) * 0.5,
                (start.Position.Y + end.Position.Y) * 0.5);

            StructureRequirement requirement = ResolveRequirement(
                start.Position,
                midpoint,
                end.Position,
                requirementProvider);

            double averageOffsetMeters =
                (start.VerticalOffsetMeters + end.VerticalOffsetMeters) * 0.5;
            ConstructionSectionType sectionType = requirement switch
            {
                StructureRequirement.Bridge => ConstructionSectionType.Bridge,
                StructureRequirement.Tunnel => ConstructionSectionType.Tunnel,
                _ => ClassifyByTerrainOffset(averageOffsetMeters, options),
            };

            double startCutAreaSquareMeters = CalculateEarthworkArea(
                start.CutDepthMeters,
                options);
            double endCutAreaSquareMeters = CalculateEarthworkArea(
                end.CutDepthMeters,
                options);
            double startFillAreaSquareMeters = CalculateEarthworkArea(
                start.FillDepthMeters,
                options);
            double endFillAreaSquareMeters = CalculateEarthworkArea(
                end.FillDepthMeters,
                options);

            double potentialCutVolumeCubicMeters =
                ((startCutAreaSquareMeters + endCutAreaSquareMeters) * 0.5) * lengthMeters;
            double potentialFillVolumeCubicMeters =
                ((startFillAreaSquareMeters + endFillAreaSquareMeters) * 0.5) * lengthMeters;

            drafts.Add(new IntervalDraft(
                start,
                end,
                sectionType,
                averageOffsetMeters,
                potentialCutVolumeCubicMeters,
                potentialFillVolumeCubicMeters,
                requirement != StructureRequirement.None));
        }

        return drafts;
    }

    private static StructureRequirement ResolveRequirement(
        PlanarPoint start,
        PlanarPoint midpoint,
        PlanarPoint end,
        IStructureRequirementProvider? provider)
    {
        if (provider == null)
        {
            return StructureRequirement.None;
        }

        StructureRequirement resolved = StructureRequirement.None;
        PlanarPoint[] points = { start, midpoint, end };

        foreach (PlanarPoint point in points)
        {
            StructureRequirement requirement = provider.GetRequirement(point);
            if (requirement == StructureRequirement.None)
            {
                continue;
            }

            if (resolved != StructureRequirement.None && resolved != requirement)
            {
                throw new InvalidOperationException(
                    "A construction interval cannot simultaneously require both bridge and tunnel treatment.");
            }

            resolved = requirement;
        }

        return resolved;
    }

    private static ConstructionSectionType ClassifyByTerrainOffset(
        double verticalOffsetMeters,
        ConstructionEvaluationOptions options)
    {
        if (verticalOffsetMeters >= options.BridgeDepthThresholdMeters)
        {
            return ConstructionSectionType.Bridge;
        }

        if (verticalOffsetMeters > options.AtGradeToleranceMeters)
        {
            return ConstructionSectionType.Embankment;
        }

        if (verticalOffsetMeters <= -options.TunnelDepthThresholdMeters)
        {
            return ConstructionSectionType.Tunnel;
        }

        if (verticalOffsetMeters < -options.AtGradeToleranceMeters)
        {
            return ConstructionSectionType.Cut;
        }

        return ConstructionSectionType.AtGrade;
    }

    private static double CalculateEarthworkArea(
        double depthMeters,
        ConstructionEvaluationOptions options)
    {
        if (depthMeters <= 0.0)
        {
            return 0.0;
        }

        // Symmetric trapezoid: formation width * depth plus the two triangular
        // side slopes, which together contribute slopeRatio * depth^2.
        return
            (options.FormationWidthMeters * depthMeters) +
            (options.SideSlopeHorizontalToVertical * depthMeters * depthMeters);
    }

    private static void NormalizeShortStructureRuns(
        IList<IntervalDraft> drafts,
        ConstructionSectionType structureType,
        double minimumRunMeters,
        ConstructionEvaluationOptions options)
    {
        if (minimumRunMeters <= 0.0)
        {
            return;
        }

        int index = 0;
        while (index < drafts.Count)
        {
            if (drafts[index].SectionType != structureType)
            {
                index++;
                continue;
            }

            int runStart = index;
            double runLengthMeters = 0.0;
            bool anyForced = false;

            while (index < drafts.Count && drafts[index].SectionType == structureType)
            {
                runLengthMeters += drafts[index].LengthMeters;
                anyForced |= drafts[index].ForcedByRequirement;
                index++;
            }

            if (anyForced || runLengthMeters + 1e-9 >= minimumRunMeters)
            {
                continue;
            }

            for (int draftIndex = runStart; draftIndex < index; draftIndex++)
            {
                IntervalDraft draft = drafts[draftIndex];
                draft.SectionType = FallbackEarthworkType(
                    draft.AverageVerticalOffsetMeters,
                    options);
            }
        }
    }

    private static ConstructionSectionType FallbackEarthworkType(
        double verticalOffsetMeters,
        ConstructionEvaluationOptions options)
    {
        if (verticalOffsetMeters > options.AtGradeToleranceMeters)
        {
            return ConstructionSectionType.Embankment;
        }

        if (verticalOffsetMeters < -options.AtGradeToleranceMeters)
        {
            return ConstructionSectionType.Cut;
        }

        return ConstructionSectionType.AtGrade;
    }

    private static IReadOnlyList<ConstructionSegment> MergeDrafts(
        IReadOnlyList<IntervalDraft> drafts,
        ConstructionEvaluationOptions options)
    {
        List<ConstructionSegment> segments = new();
        int index = 0;

        while (index < drafts.Count)
        {
            ConstructionSectionType sectionType = drafts[index].SectionType;
            int runStart = index;
            double totalLengthMeters = 0.0;
            double weightedOffset = 0.0;
            double cutVolumeCubicMeters = 0.0;
            double fillVolumeCubicMeters = 0.0;
            double relativeCost = 0.0;
            bool forced = false;

            while (index < drafts.Count && drafts[index].SectionType == sectionType)
            {
                IntervalDraft draft = drafts[index];
                totalLengthMeters += draft.LengthMeters;
                weightedOffset += draft.AverageVerticalOffsetMeters * draft.LengthMeters;
                forced |= draft.ForcedByRequirement;

                if (sectionType == ConstructionSectionType.Bridge)
                {
                    relativeCost += options.BridgeCostPerMeter * draft.LengthMeters;
                }
                else if (sectionType == ConstructionSectionType.Tunnel)
                {
                    relativeCost += options.TunnelCostPerMeter * draft.LengthMeters;
                }
                else
                {
                    cutVolumeCubicMeters += draft.PotentialCutVolumeCubicMeters;
                    fillVolumeCubicMeters += draft.PotentialFillVolumeCubicMeters;
                    relativeCost +=
                        (options.SurfaceCostPerMeter * draft.LengthMeters) +
                        (options.EarthworkCostPerCubicMeter *
                            (draft.PotentialCutVolumeCubicMeters + draft.PotentialFillVolumeCubicMeters));
                }

                index++;
            }

            IntervalDraft first = drafts[runStart];
            IntervalDraft last = drafts[index - 1];
            segments.Add(new ConstructionSegment(
                first.Start.StationMeters,
                last.End.StationMeters,
                first.Start.Position,
                last.End.Position,
                sectionType,
                weightedOffset / totalLengthMeters,
                cutVolumeCubicMeters,
                fillVolumeCubicMeters,
                relativeCost,
                forced));
        }

        return segments.AsReadOnly();
    }

    private sealed class IntervalDraft
    {
        public IntervalDraft(
            AlignmentTerrainSample start,
            AlignmentTerrainSample end,
            ConstructionSectionType sectionType,
            double averageVerticalOffsetMeters,
            double potentialCutVolumeCubicMeters,
            double potentialFillVolumeCubicMeters,
            bool forcedByRequirement)
        {
            Start = start;
            End = end;
            SectionType = sectionType;
            AverageVerticalOffsetMeters = averageVerticalOffsetMeters;
            PotentialCutVolumeCubicMeters = potentialCutVolumeCubicMeters;
            PotentialFillVolumeCubicMeters = potentialFillVolumeCubicMeters;
            ForcedByRequirement = forcedByRequirement;
        }

        public AlignmentTerrainSample Start { get; }

        public AlignmentTerrainSample End { get; }

        public double LengthMeters => End.StationMeters - Start.StationMeters;

        public ConstructionSectionType SectionType { get; set; }

        public double AverageVerticalOffsetMeters { get; }

        public double PotentialCutVolumeCubicMeters { get; }

        public double PotentialFillVolumeCubicMeters { get; }

        public bool ForcedByRequirement { get; }
    }
}
