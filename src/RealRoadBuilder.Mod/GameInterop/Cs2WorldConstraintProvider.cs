using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Terrain;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Transform = Game.Objects.Transform;

namespace RealRoadBuilder.Mod.GameInterop;

/// <summary>
/// Immutable, read-only snapshot of live CS2 world features used by the corridor
/// search. Buildings are represented by transformed prefab bounds rather than
/// arbitrary circular approximations. Existing road and surface-rail centerlines
/// remain soft costs so crossings stay possible until interchange/grade-separation
/// rules are implemented explicitly.
/// </summary>
public sealed class Cs2WorldConstraintProvider : ICorridorConstraintProvider
{
    private readonly IReadOnlyList<BuildingFootprint> _buildings;
    private readonly IReadOnlyList<NetworkInfluence> _roads;
    private readonly IReadOnlyList<NetworkInfluence> _rails;
    private readonly bool _buildingsAreHardObstacles;
    private readonly double _buildingSoftCost;

    private Cs2WorldConstraintProvider(
        IReadOnlyList<BuildingFootprint> buildings,
        IReadOnlyList<NetworkInfluence> roads,
        IReadOnlyList<NetworkInfluence> rails,
        bool buildingsAreHardObstacles,
        double buildingSoftCost)
    {
        _buildings = buildings;
        _roads = roads;
        _rails = rails;
        _buildingsAreHardObstacles = buildingsAreHardObstacles;
        _buildingSoftCost = buildingSoftCost;
    }

    public int BuildingCount => _buildings.Count;

    public int RoadEdgeCount => _roads.Count;

    public int RailEdgeCount => _rails.Count;

    public static Cs2WorldConstraintProvider Capture(
        EntityManager entityManager,
        double buildingClearanceMeters,
        bool buildingsAreHardObstacles,
        double buildingSoftCost,
        double roadInfluenceMeters,
        double roadCost,
        double railInfluenceMeters,
        double railCost)
    {
        if (buildingClearanceMeters < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingClearanceMeters));
        }

        if (roadInfluenceMeters < 0.0 || railInfluenceMeters < 0.0)
        {
            throw new ArgumentOutOfRangeException("Network influence distances may not be negative.");
        }

        List<BuildingFootprint> buildings = CaptureBuildings(
            entityManager,
            buildingClearanceMeters);
        CaptureNetworks(
            entityManager,
            roadInfluenceMeters,
            roadCost,
            railInfluenceMeters,
            railCost,
            out List<NetworkInfluence> roads,
            out List<NetworkInfluence> rails);

        return new Cs2WorldConstraintProvider(
            buildings.AsReadOnly(),
            roads.AsReadOnly(),
            rails.AsReadOnly(),
            buildingsAreHardObstacles,
            buildingSoftCost);
    }

    public bool IsBlocked(PlanarPoint point)
    {
        if (!_buildingsAreHardObstacles)
        {
            return false;
        }

        for (int index = 0; index < _buildings.Count; index++)
        {
            if (_buildings[index].Contains(point))
            {
                return true;
            }
        }

        return false;
    }

    public double GetAdditionalCost(PlanarPoint point)
    {
        double cost = 0.0;

        if (!_buildingsAreHardObstacles)
        {
            for (int index = 0; index < _buildings.Count; index++)
            {
                if (_buildings[index].Contains(point))
                {
                    cost += _buildingSoftCost;
                }
            }
        }

        cost += SumNetworkCosts(_roads, point);
        cost += SumNetworkCosts(_rails, point);
        return cost;
    }

    private static double SumNetworkCosts(
        IReadOnlyList<NetworkInfluence> influences,
        PlanarPoint point)
    {
        double cost = 0.0;
        float2 position = new((float)point.X, (float)point.Y);

        for (int index = 0; index < influences.Count; index++)
        {
            NetworkInfluence influence = influences[index];
            float distance = MathUtils.Distance(influence.Curve.xz, position, out _);
            if (distance >= influence.RadiusMeters)
            {
                continue;
            }

            double fraction = 1.0 - (distance / influence.RadiusMeters);
            cost += influence.Cost * fraction * fraction;
        }

        return cost;
    }

    private static List<BuildingFootprint> CaptureBuildings(
        EntityManager entityManager,
        double clearanceMeters)
    {
        EntityQuery query = entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<Building>(),
                ComponentType.ReadOnly<Transform>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<Deleted>(),
                ComponentType.ReadOnly<Temp>(),
            },
        });

        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        List<BuildingFootprint> result = new(entities.Length);

        try
        {
            for (int index = 0; index < entities.Length; index++)
            {
                Entity entity = entities[index];
                Transform transform = entityManager.GetComponentData<Transform>(entity);

                if (TryCreatePrefabFootprint(
                        entityManager,
                        entity,
                        transform,
                        clearanceMeters,
                        out BuildingFootprint footprint) ||
                    TryCreateWorldBoundsFootprint(
                        entityManager,
                        entity,
                        clearanceMeters,
                        out footprint))
                {
                    result.Add(footprint);
                }
            }
        }
        finally
        {
            entities.Dispose();
            query.Dispose();
        }

        return result;
    }

    private static bool TryCreatePrefabFootprint(
        EntityManager entityManager,
        Entity buildingEntity,
        Transform transform,
        double clearanceMeters,
        out BuildingFootprint footprint)
    {
        footprint = default;

        if (!entityManager.TryGetComponent<PrefabRef>(buildingEntity, out PrefabRef prefabRef) ||
            prefabRef.m_Prefab == Entity.Null ||
            !entityManager.TryGetComponent<ObjectGeometryData>(prefabRef.m_Prefab, out ObjectGeometryData geometry))
        {
            return false;
        }

        float3 localCenter = (geometry.m_Bounds.min + geometry.m_Bounds.max) * 0.5f;
        float3 size = geometry.m_Bounds.max - geometry.m_Bounds.min;
        if (size.x <= 0.01f || size.z <= 0.01f)
        {
            return false;
        }

        float3 worldCenter = transform.m_Position + math.rotate(transform.m_Rotation, localCenter);
        float3 right3 = math.rotate(transform.m_Rotation, new float3(1f, 0f, 0f));
        float3 forward3 = math.rotate(transform.m_Rotation, new float3(0f, 0f, 1f));
        float2 right = math.normalizesafe(right3.xz, new float2(1f, 0f));
        float2 forward = math.normalizesafe(forward3.xz, new float2(0f, 1f));

        footprint = new BuildingFootprint(
            new PlanarPoint(worldCenter.x, worldCenter.z),
            right,
            forward,
            (size.x * 0.5) + clearanceMeters,
            (size.z * 0.5) + clearanceMeters);
        return true;
    }

    private static bool TryCreateWorldBoundsFootprint(
        EntityManager entityManager,
        Entity buildingEntity,
        double clearanceMeters,
        out BuildingFootprint footprint)
    {
        footprint = default;
        if (!entityManager.TryGetComponent<ObjectGeometryData>(buildingEntity, out ObjectGeometryData geometry))
        {
            return false;
        }

        float3 size = geometry.m_Bounds.max - geometry.m_Bounds.min;
        if (size.x <= 0.01f || size.z <= 0.01f)
        {
            return false;
        }

        float3 center = (geometry.m_Bounds.min + geometry.m_Bounds.max) * 0.5f;
        footprint = new BuildingFootprint(
            new PlanarPoint(center.x, center.z),
            new float2(1f, 0f),
            new float2(0f, 1f),
            (size.x * 0.5) + clearanceMeters,
            (size.z * 0.5) + clearanceMeters);
        return true;
    }

    private static void CaptureNetworks(
        EntityManager entityManager,
        double roadInfluenceMeters,
        double roadCost,
        double railInfluenceMeters,
        double railCost,
        out List<NetworkInfluence> roads,
        out List<NetworkInfluence> rails)
    {
        EntityQuery query = entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<Edge>(),
                ComponentType.ReadOnly<Curve>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<Deleted>(),
                ComponentType.ReadOnly<Temp>(),
            },
        });

        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        roads = new List<NetworkInfluence>();
        rails = new List<NetworkInfluence>();

        try
        {
            for (int index = 0; index < entities.Length; index++)
            {
                Entity entity = entities[index];
                Curve curve = entityManager.GetComponentData<Curve>(entity);

                if (roadInfluenceMeters > 0.0 && entityManager.HasComponent<Road>(entity))
                {
                    roads.Add(new NetworkInfluence(
                        curve.m_Bezier,
                        (float)roadInfluenceMeters,
                        roadCost));
                }

                bool isSurfaceRail =
                    entityManager.HasComponent<TrainTrack>(entity) ||
                    entityManager.HasComponent<TramTrack>(entity);

                // SubwayTrack is intentionally omitted here: a purely planar
                // proximity penalty would incorrectly treat underground tracks
                // as surface conflicts. A future 3D clearance adapter can add it.
                if (railInfluenceMeters > 0.0 && isSurfaceRail)
                {
                    rails.Add(new NetworkInfluence(
                        curve.m_Bezier,
                        (float)railInfluenceMeters,
                        railCost));
                }
            }
        }
        finally
        {
            entities.Dispose();
            query.Dispose();
        }
    }

    private readonly struct BuildingFootprint
    {
        public BuildingFootprint(
            PlanarPoint center,
            float2 right,
            float2 forward,
            double halfWidthMeters,
            double halfDepthMeters)
        {
            Center = center;
            Right = right;
            Forward = forward;
            HalfWidthMeters = halfWidthMeters;
            HalfDepthMeters = halfDepthMeters;
        }

        public PlanarPoint Center { get; }
        public float2 Right { get; }
        public float2 Forward { get; }
        public double HalfWidthMeters { get; }
        public double HalfDepthMeters { get; }

        public bool Contains(PlanarPoint point)
        {
            float2 delta = new(
                (float)(point.X - Center.X),
                (float)(point.Y - Center.Y));
            double localX = math.dot(delta, Right);
            double localZ = math.dot(delta, Forward);

            return
                Math.Abs(localX) <= HalfWidthMeters &&
                Math.Abs(localZ) <= HalfDepthMeters;
        }
    }

    private readonly struct NetworkInfluence
    {
        public NetworkInfluence(Bezier4x3 curve, float radiusMeters, double cost)
        {
            Curve = curve;
            RadiusMeters = radiusMeters;
            Cost = cost;
        }

        public Bezier4x3 Curve { get; }
        public float RadiusMeters { get; }
        public double Cost { get; }
    }
}
