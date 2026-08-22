using System;
using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Evaluation;

public sealed class ConstructionSegment
{
    public ConstructionSegment(
        double startStationMeters,
        double endStationMeters,
        PlanarPoint startPosition,
        PlanarPoint endPosition,
        ConstructionSectionType sectionType,
        double averageVerticalOffsetMeters,
        double cutVolumeCubicMeters,
        double fillVolumeCubicMeters,
        double relativeCost,
        bool forcedByRequirement)
    {
        if (endStationMeters <= startStationMeters)
        {
            throw new ArgumentOutOfRangeException(nameof(endStationMeters));
        }

        if (cutVolumeCubicMeters < 0.0 || fillVolumeCubicMeters < 0.0 || relativeCost < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(relativeCost),
                "Volumes and relative cost may not be negative.");
        }

        StartStationMeters = startStationMeters;
        EndStationMeters = endStationMeters;
        StartPosition = startPosition;
        EndPosition = endPosition;
        SectionType = sectionType;
        AverageVerticalOffsetMeters = averageVerticalOffsetMeters;
        CutVolumeCubicMeters = cutVolumeCubicMeters;
        FillVolumeCubicMeters = fillVolumeCubicMeters;
        RelativeCost = relativeCost;
        ForcedByRequirement = forcedByRequirement;
    }

    public double StartStationMeters { get; }

    public double EndStationMeters { get; }

    public double LengthMeters => EndStationMeters - StartStationMeters;

    public PlanarPoint StartPosition { get; }

    public PlanarPoint EndPosition { get; }

    public ConstructionSectionType SectionType { get; }

    /// <summary>
    /// Positive is above terrain, negative is below terrain.
    /// </summary>
    public double AverageVerticalOffsetMeters { get; }

    public double CutVolumeCubicMeters { get; }

    public double FillVolumeCubicMeters { get; }

    /// <summary>
    /// Dimensionless comparison score, not a currency estimate.
    /// </summary>
    public double RelativeCost { get; }

    public bool ForcedByRequirement { get; }
}
