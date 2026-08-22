using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Evaluation;

public readonly struct AlignmentTerrainSample
{
    public AlignmentTerrainSample(
        double stationMeters,
        PlanarPoint position,
        double terrainElevationMeters,
        double designElevationMeters,
        double designGradePercent)
    {
        StationMeters = stationMeters;
        Position = position;
        TerrainElevationMeters = terrainElevationMeters;
        DesignElevationMeters = designElevationMeters;
        DesignGradePercent = designGradePercent;
    }

    public double StationMeters { get; }

    public PlanarPoint Position { get; }

    public double TerrainElevationMeters { get; }

    public double DesignElevationMeters { get; }

    public double DesignGradePercent { get; }

    /// <summary>
    /// Positive values mean the designed roadway is above terrain (fill/structure).
    /// Negative values mean the designed roadway is below terrain (cut/tunnel).
    /// </summary>
    public double VerticalOffsetMeters => DesignElevationMeters - TerrainElevationMeters;

    public double FillDepthMeters => VerticalOffsetMeters > 0.0 ? VerticalOffsetMeters : 0.0;

    public double CutDepthMeters => VerticalOffsetMeters < 0.0 ? -VerticalOffsetMeters : 0.0;
}
