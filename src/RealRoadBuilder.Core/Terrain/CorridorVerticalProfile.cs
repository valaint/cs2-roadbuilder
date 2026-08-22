using System;
using System.Collections.Generic;
using System.Linq;

namespace RealRoadBuilder.Core.Terrain;

public sealed class CorridorVerticalProfile
{
    public CorridorVerticalProfile(IEnumerable<DesignedProfileSample> samples)
    {
        if (samples == null)
        {
            throw new ArgumentNullException(nameof(samples));
        }

        List<DesignedProfileSample> materializedSamples = samples.ToList();
        if (materializedSamples.Count < 2)
        {
            throw new ArgumentException("A designed profile requires at least two samples.", nameof(samples));
        }

        for (int index = 1; index < materializedSamples.Count; index++)
        {
            if (materializedSamples[index].StationMeters <=
                materializedSamples[index - 1].StationMeters)
            {
                throw new ArgumentException(
                    "Designed profile sample stations must be strictly increasing.",
                    nameof(samples));
            }
        }

        Samples = materializedSamples.AsReadOnly();
        MaximumAbsoluteGradePercent = materializedSamples
            .Skip(1)
            .Max(sample => Math.Abs(sample.GradeFromPreviousPercent));
        MaximumCutMeters = materializedSamples
            .Max(sample => Math.Max(0.0, sample.TerrainElevationMeters - sample.DesignElevationMeters));
        MaximumFillMeters = materializedSamples
            .Max(sample => Math.Max(0.0, sample.DesignElevationMeters - sample.TerrainElevationMeters));
    }

    public IReadOnlyList<DesignedProfileSample> Samples { get; }

    public double MaximumAbsoluteGradePercent { get; }

    public double MaximumCutMeters { get; }

    public double MaximumFillMeters { get; }
}
