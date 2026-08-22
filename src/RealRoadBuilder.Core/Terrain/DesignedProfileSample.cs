namespace RealRoadBuilder.Core.Terrain;

public readonly struct DesignedProfileSample
{
    public DesignedProfileSample(
        double stationMeters,
        double terrainElevationMeters,
        double designElevationMeters,
        double gradeFromPreviousPercent)
    {
        StationMeters = stationMeters;
        TerrainElevationMeters = terrainElevationMeters;
        DesignElevationMeters = designElevationMeters;
        GradeFromPreviousPercent = gradeFromPreviousPercent;
    }

    public double StationMeters { get; }

    public double TerrainElevationMeters { get; }

    public double DesignElevationMeters { get; }

    public double GradeFromPreviousPercent { get; }

    public double CutFillMeters => DesignElevationMeters - TerrainElevationMeters;
}
