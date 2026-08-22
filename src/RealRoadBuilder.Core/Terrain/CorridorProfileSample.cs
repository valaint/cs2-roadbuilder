using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Terrain;

public readonly struct CorridorProfileSample
{
    public CorridorProfileSample(
        double stationMeters,
        PlanarPoint position,
        double terrainElevationMeters,
        double gradeFromPreviousPercent)
    {
        StationMeters = stationMeters;
        Position = position;
        TerrainElevationMeters = terrainElevationMeters;
        GradeFromPreviousPercent = gradeFromPreviousPercent;
    }

    public double StationMeters { get; }

    public PlanarPoint Position { get; }

    public double TerrainElevationMeters { get; }

    public double GradeFromPreviousPercent { get; }
}
