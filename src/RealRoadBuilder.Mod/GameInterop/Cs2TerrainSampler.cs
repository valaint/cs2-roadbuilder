using Game.Simulation;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Terrain;
using Unity.Mathematics;

namespace RealRoadBuilder.Mod.GameInterop;

/// <summary>
/// Adapts a Cities: Skylines II CPU terrain-height snapshot to the
/// game-independent RealRoad Builder terrain interface.
///
/// The owning game system is responsible for registering a CPU height reader
/// before creating the snapshot. Keeping that lifecycle outside this adapter
/// avoids hiding Unity job dependencies behind the synchronous core interface.
/// </summary>
public sealed class Cs2TerrainSampler : ITerrainSampler
{
    private TerrainHeightData _heightData;

    public Cs2TerrainSampler(TerrainHeightData heightData)
    {
        _heightData = heightData;
    }

    public double GetElevationMeters(PlanarPoint position)
    {
        float3 gamePosition = new((float)position.X, 0f, (float)position.Y);
        return TerrainUtils.SampleHeight(ref _heightData, gamePosition);
    }
}
