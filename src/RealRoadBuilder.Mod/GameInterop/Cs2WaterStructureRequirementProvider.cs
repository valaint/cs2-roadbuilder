using System;
using Game.Simulation;
using RealRoadBuilder.Core.Evaluation;
using RealRoadBuilder.Core.Geometry;
using Unity.Jobs;
using Unity.Mathematics;

namespace RealRoadBuilder.Mod.GameInterop;

/// <summary>
/// Read-only water snapshot used during one planning run. Points with real CS2
/// surface-water depth above the configured threshold require a bridge in the
/// construction evaluator. This does not modify the water simulation.
/// </summary>
public sealed class Cs2WaterStructureRequirementProvider : IStructureRequirementProvider
{
    private WaterSurfaceData _waterData;
    private readonly float _minimumBridgeDepthMeters;

    private Cs2WaterStructureRequirementProvider(
        WaterSurfaceData waterData,
        float minimumBridgeDepthMeters)
    {
        _waterData = waterData;
        _minimumBridgeDepthMeters = minimumBridgeDepthMeters;
    }

    public static Cs2WaterStructureRequirementProvider Capture(
        WaterSystem waterSystem,
        float minimumBridgeDepthMeters)
    {
        if (waterSystem == null)
        {
            throw new ArgumentNullException(nameof(waterSystem));
        }

        if (minimumBridgeDepthMeters < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumBridgeDepthMeters));
        }

        WaterSurfaceData waterData = waterSystem.GetSurfaceData(out JobHandle dependencies);
        dependencies.Complete();
        return new Cs2WaterStructureRequirementProvider(
            waterData,
            minimumBridgeDepthMeters);
    }

    public StructureRequirement GetRequirement(PlanarPoint position)
    {
        float3 gamePosition = new((float)position.X, 0f, (float)position.Y);
        float depthMeters = WaterUtils.SampleDepth(ref _waterData, gamePosition);
        return depthMeters >= _minimumBridgeDepthMeters
            ? StructureRequirement.Bridge
            : StructureRequirement.None;
    }
}
