using System;
using System.Collections.Generic;
using System.Linq;

namespace RealRoadBuilder.Core.Evaluation;

public sealed class ConstructionEvaluationResult
{
    public ConstructionEvaluationResult(
        IEnumerable<AlignmentTerrainSample> samples,
        IEnumerable<ConstructionSegment> segments)
    {
        if (samples == null)
        {
            throw new ArgumentNullException(nameof(samples));
        }

        if (segments == null)
        {
            throw new ArgumentNullException(nameof(segments));
        }

        List<AlignmentTerrainSample> materializedSamples = samples.ToList();
        List<ConstructionSegment> materializedSegments = segments.ToList();

        if (materializedSamples.Count < 2)
        {
            throw new ArgumentException("Construction evaluation requires at least two terrain samples.", nameof(samples));
        }

        Samples = materializedSamples.AsReadOnly();
        Segments = materializedSegments.AsReadOnly();
        TotalCutVolumeCubicMeters = materializedSegments.Sum(segment => segment.CutVolumeCubicMeters);
        TotalFillVolumeCubicMeters = materializedSegments.Sum(segment => segment.FillVolumeCubicMeters);
        TotalRelativeCost = materializedSegments.Sum(segment => segment.RelativeCost);
        MaximumCutDepthMeters = materializedSamples.Max(sample => sample.CutDepthMeters);
        MaximumFillDepthMeters = materializedSamples.Max(sample => sample.FillDepthMeters);
        BridgeLengthMeters = LengthFor(ConstructionSectionType.Bridge);
        TunnelLengthMeters = LengthFor(ConstructionSectionType.Tunnel);
        CutLengthMeters = LengthFor(ConstructionSectionType.Cut);
        EmbankmentLengthMeters = LengthFor(ConstructionSectionType.Embankment);
        AtGradeLengthMeters = LengthFor(ConstructionSectionType.AtGrade);
    }

    public IReadOnlyList<AlignmentTerrainSample> Samples { get; }

    public IReadOnlyList<ConstructionSegment> Segments { get; }

    public double TotalCutVolumeCubicMeters { get; }

    public double TotalFillVolumeCubicMeters { get; }

    public double TotalRelativeCost { get; }

    public double MaximumCutDepthMeters { get; }

    public double MaximumFillDepthMeters { get; }

    public double BridgeLengthMeters { get; }

    public double TunnelLengthMeters { get; }

    public double CutLengthMeters { get; }

    public double EmbankmentLengthMeters { get; }

    public double AtGradeLengthMeters { get; }

    public double TotalLengthMeters => Segments.Sum(segment => segment.LengthMeters);

    public double LengthFor(ConstructionSectionType sectionType)
    {
        return Segments
            .Where(segment => segment.SectionType == sectionType)
            .Sum(segment => segment.LengthMeters);
    }
}
