using System;
using Colossal.Mathematics;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;
using RealRoadBuilder.Core.Evaluation;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Planning;
using RealRoadBuilder.Core.Standards;
using RealRoadBuilder.Core.Terrain;
using RealRoadBuilder.Mod.GameInterop;
using RealRoadBuilder.Mod.Settings;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace RealRoadBuilder.Mod.Tools;

/// <summary>
/// Read-only highway planning tool. The tool raycasts terrain, accepts start/end
/// points, runs the game-independent planning engine, and renders the resulting
/// fitted alignment as an overlay. It never creates or modifies network entities.
/// </summary>
public sealed partial class RealRoadPreviewToolSystem : ObjectToolBaseSystem
{
    private const float EndpointRadiusMeters = 8f;
    private const float OverlayLiftMeters = 0.35f;

    private TerrainSystem? _terrainSystem;
    private TerrainHeightData _terrainHeightData;
    private OverlayRenderSystem.Buffer _overlayBuffer;
    private readonly HighwayPlanningEngine _planningEngine = new();

    private float3? _startPoint;
    private float3? _endPoint;
    private float3 _cursorPoint;
    private bool _hasCursor;
    private HighwayPlanningResult? _planningResult;

    public override string toolID => "RealRoadBuilder.Preview";

    public string StatusText { get; private set; } =
        "Activate the preview tool, then click a highway start point.";

    public override PrefabBase GetPrefab() => null!;

    public override bool TrySetPrefab(PrefabBase prefab) => false;

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        _terrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();
        _overlayBuffer = World
            .GetOrCreateSystemManaged<OverlayRenderSystem>()
            .GetBuffer(out _);
    }

    public override void InitializeRaycast()
    {
        base.InitializeRaycast();
        m_ToolRaycastSystem.typeMask = TypeMask.Terrain;
        m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround;
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        applyAction.shouldBeEnabled = true;
        cancelAction.shouldBeEnabled = true;
        _hasCursor = false;
        StatusText = _startPoint.HasValue
            ? "Click the highway end point."
            : "Click the highway start point.";
    }

    protected override void OnStopRunning()
    {
        applyAction.shouldBeEnabled = false;
        cancelAction.shouldBeEnabled = false;
        _hasCursor = false;
        base.OnStopRunning();
    }

    public void ToggleTool(bool enable)
    {
        if (enable && m_ToolSystem.activeTool != this)
        {
            m_ToolSystem.selected = Entity.Null;
            m_ToolSystem.activeTool = this;
        }
        else if (!enable && m_ToolSystem.activeTool == this)
        {
            m_ToolSystem.selected = Entity.Null;
            m_ToolSystem.activeTool = m_DefaultToolSystem;
        }
    }

    public void ClearPreview()
    {
        _startPoint = null;
        _endPoint = null;
        _planningResult = null;
        StatusText = "Preview cleared. Click a highway start point.";
    }

    [Preserve]
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        applyMode = ApplyMode.Clear;

        RefreshTerrainSnapshot(inputDeps);
        UpdateCursor();
        HandleCancel();
        HandleApply();
        DrawPreview();

        return inputDeps;
    }

    private void RefreshTerrainSnapshot(JobHandle readerDependency)
    {
        if (_terrainSystem == null)
        {
            return;
        }

        // TerrainSystem tracks CPU readers so terrain writes are not allowed to
        // race the synchronous preview sampler. This mirrors maintained CS2 tool
        // integrations that sample TerrainHeightData on the CPU.
        _terrainSystem.AddCPUHeightReader(readerDependency);
        _terrainHeightData = _terrainSystem.GetHeightData(false);
    }

    private void UpdateCursor()
    {
        _hasCursor = false;

        if (_terrainSystem == null || !GetRaycastResult(out ControlPoint raycastPoint))
        {
            return;
        }

        float3 point = raycastPoint.m_HitPosition;
        point.y = TerrainUtils.SampleHeight(ref _terrainHeightData, point);
        _cursorPoint = point;
        _hasCursor = true;
    }

    private void HandleCancel()
    {
        if (!cancelAction.WasPressedThisFrame())
        {
            return;
        }

        if (_planningResult != null || _endPoint.HasValue)
        {
            _endPoint = null;
            _planningResult = null;
            StatusText = "Preview removed. Click a new highway end point.";
            return;
        }

        if (_startPoint.HasValue)
        {
            _startPoint = null;
            StatusText = "Start point cleared. Click a highway start point.";
            return;
        }

        ToggleTool(false);
    }

    private void HandleApply()
    {
        if (!_hasCursor || !applyAction.WasPressedThisFrame())
        {
            return;
        }

        if (!_startPoint.HasValue)
        {
            _startPoint = _cursorPoint;
            _endPoint = null;
            _planningResult = null;
            StatusText = "Start selected. Click the highway end point.";
            return;
        }

        if (math.distance(_startPoint.Value.xz, _cursorPoint.xz) < 100f)
        {
            StatusText = "End point must be at least 100 m from the start point.";
            return;
        }

        _endPoint = _cursorPoint;
        GeneratePreview();
    }

    private void GeneratePreview()
    {
        if (_terrainSystem == null || !_startPoint.HasValue || !_endPoint.HasValue)
        {
            return;
        }

        RealRoadBuilderSettings settings = Mod.Settings;
        Cs2TerrainSampler terrainSampler = new(_terrainHeightData);
        HighwayPlanningOptions planningOptions = new()
        {
            Search = new CorridorSearchOptions(
                cellSizeMeters: settings.SearchCellSizeMeters,
                searchMarginMeters: 1200.0,
                maximumExpandedStates: 250000,
                gradePenaltyWeight: 8.0,
                turnPenaltyWeight: 20.0,
                additionalCostWeight: 1.0),
            MaximumIterations = settings.MaximumPlanningIterations,
            StopWhenNoMajorStructures = false,
        };
        planningOptions.Fit.UseExceptionalGeometry(settings.AllowExceptionalGeometry);

        PlanarPoint start = ToPlanarPoint(_startPoint.Value);
        PlanarPoint end = ToPlanarPoint(_endPoint.Value);

        try
        {
            StatusText = "Generating highway preview...";
            _planningResult = _planningEngine.Plan(
                start,
                end,
                terrainSampler,
                JapanRoadStructureOrdinance.GetExpresswayRule(settings.DesignSpeedKph),
                planningOptions);

            HighwayPlanningAttempt? bestAttempt = _planningResult.BestAttempt;
            if (bestAttempt?.ConstructionEvaluation == null)
            {
                string failure = _planningResult.Attempts[_planningResult.Attempts.Count - 1].FailureReason
                    ?? "No standards-valid candidate was produced.";
                StatusText = $"No valid preview: {failure}";
                return;
            }

            ConstructionEvaluationResult evaluation = bestAttempt.ConstructionEvaluation;
            StatusText =
                $"{settings.DesignSpeedKph} km/h | " +
                $"{bestAttempt.Route.TotalLengthMeters:0} m corridor | " +
                $"bridge {evaluation.BridgeLengthMeters:0} m | " +
                $"tunnel {evaluation.TunnelLengthMeters:0} m | " +
                $"relative cost {evaluation.TotalRelativeCost:0.0}";

            Mod.Log.Info($"Generated preview: {StatusText}");
        }
        catch (Exception exception)
        {
            _planningResult = null;
            StatusText = $"Preview generation failed: {exception.Message}";
            Mod.Log.Warn($"Highway preview generation failed: {exception}");
        }
    }

    private void DrawPreview()
    {
        RealRoadBuilderSettings settings = Mod.Settings;
        float lineWidth = settings.PreviewLineWidth;

        if (_startPoint.HasValue)
        {
            float3 start = LiftToTerrain(_startPoint.Value);
            _overlayBuffer.DrawCircle(Color.cyan, start, EndpointRadiusMeters);

            if (_planningResult == null && _hasCursor)
            {
                float3 cursor = LiftToTerrain(_cursorPoint);
                _overlayBuffer.DrawDashedLine(
                    Color.cyan,
                    new Line3.Segment(start, cursor),
                    lineWidth,
                    6f,
                    4f);
                _overlayBuffer.DrawCircle(Color.cyan, cursor, EndpointRadiusMeters);
            }
        }

        HighwayPlanningAttempt? bestAttempt = _planningResult?.BestAttempt;
        ConstructionEvaluationResult? evaluation = bestAttempt?.ConstructionEvaluation;
        if (evaluation == null)
        {
            return;
        }

        foreach (ConstructionSegment segment in evaluation.Segments)
        {
            Color color = GetSectionColor(segment.SectionType);
            float3 start = LiftPlanarPoint(segment.StartPosition);
            float3 end = LiftPlanarPoint(segment.EndPosition);

            _overlayBuffer.DrawLine(
                color,
                color,
                0,
                0,
                new Line3.Segment(start, end),
                lineWidth,
                1f);
        }

        if (_endPoint.HasValue)
        {
            _overlayBuffer.DrawCircle(
                Color.cyan,
                LiftToTerrain(_endPoint.Value),
                EndpointRadiusMeters);
        }
    }

    private float3 LiftPlanarPoint(PlanarPoint point)
    {
        float3 gamePoint = new((float)point.X, 0f, (float)point.Y);
        return LiftToTerrain(gamePoint);
    }

    private float3 LiftToTerrain(float3 point)
    {
        if (_terrainSystem == null)
        {
            return point;
        }

        point.y = TerrainUtils.SampleHeight(ref _terrainHeightData, point) + OverlayLiftMeters;
        return point;
    }

    private static PlanarPoint ToPlanarPoint(float3 point) =>
        new(point.x, point.z);

    private static Color GetSectionColor(ConstructionSectionType sectionType)
    {
        return sectionType switch
        {
            ConstructionSectionType.AtGrade => Color.white,
            ConstructionSectionType.Cut => Color.yellow,
            ConstructionSectionType.Embankment => Color.green,
            ConstructionSectionType.Bridge => Color.magenta,
            ConstructionSectionType.Tunnel => Color.red,
            _ => Color.gray,
        };
    }
}
