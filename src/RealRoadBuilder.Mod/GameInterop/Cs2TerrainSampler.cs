using Game.Simulation;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Terrain;
using Unity.Mathematics;

namespace RealRoadBuilder.Mod.GameInterop;

/// <summary>
/// Adapts the current Cities: Skylines II terrain-height snapshot to the
/// game-independent RealRoad Builder terrain interface.
/// </summary>
public sealed class Cs2TerrainSampler : ITerrainSampler
{
    private TerrainHeightData _heightData;

    public Cs2TerrainSampler(TerrainSystem terrainSystem)
    {
        _heightData = terrainSystem.GetHeightData();
    }

    public double GetElevationMeters(PlanarPoint position)
    {
        float3 gamePosition = new((float)position.X, 0f, (float)position.Y);
        return TerrainUtils.SampleHeight(ref _heightData, gamePosition);
    }
}
