using System;
using System.Linq;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Standards;
using RealRoadBuilder.Core.Terrain;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class TerrainAwareCorridorRouterTests
{
    [Fact]
    public void FindRoute_OnFlatTerrain_ReturnsDirectExactEndpointRoute()
    {
        TerrainAwareCorridorRouter router = new();
        CorridorRoute route = router.FindRoute(
            new PlanarPoint(0.0, 0.0),
            new PlanarPoint(400.0, 0.0),
            new DelegateTerrainSampler(_ => 12.0),
            JapanRoadStructureOrdinance.GetExpresswayRule(100),
            new CorridorSearchOptions(
                cellSizeMeters: 40.0,
                searchMarginMeters: 200.0));

        Assert.Equal(new PlanarPoint(0.0, 0.0), route.Waypoints[0].Position);
        Assert.Equal(new PlanarPoint(400.0, 0.0), route.Waypoints[route.Waypoints.Count - 1].Position);
        Assert.Equal(400.0, route.TotalLengthMeters, 6);
        Assert.Equal(0.0, route.MaximumAbsoluteGradePercent, 6);
        Assert.Equal(2, route.Waypoints.Count);
    }

    [Fact]
    public void FindRoute_AroundSteepTerrainBarrier_DetoursInsteadOfViolatingGrade()
    {
        TerrainAwareCorridorRouter router = new();
        DelegateTerrainSampler terrain = new(point =>
        {
            bool insideBarrier =
                point.X >= 160.0 &&
                point.X <= 240.0 &&
                point.Y >= -80.0 &&
                point.Y <= 80.0;
            return insideBarrier ? 40.0 : 0.0;
        });

        CorridorRoute route = router.FindRoute(
            new PlanarPoint(0.0, 0.0),
            new PlanarPoint(400.0, 0.0),
            terrain,
            JapanRoadStructureOrdinance.GetExpresswayRule(100),
            new CorridorSearchOptions(
                cellSizeMeters: 40.0,
                searchMarginMeters: 240.0,
                gradePenaltyWeight: 4.0,
                turnPenaltyWeight: 10.0));

        Assert.True(route.Waypoints.Any(waypoint => Math.Abs(waypoint.Position.Y) > 80.0));
        Assert.True(route.MaximumAbsoluteGradePercent <= 3.0 + 1e-6);
        Assert.True(route.TotalLengthMeters > 400.0);
    }

    [Fact]
    public void FindRoute_BlockedCorridor_UsesConstraintProviderToDetour()
    {
        TerrainAwareCorridorRouter router = new();
        RectangleConstraintProvider constraints = new(
            minimumX: 160.0,
            maximumX: 240.0,
            minimumY: -80.0,
            maximumY: 80.0);

        CorridorRoute route = router.FindRoute(
            new PlanarPoint(0.0, 0.0),
            new PlanarPoint(400.0, 0.0),
            new DelegateTerrainSampler(_ => 0.0),
            JapanRoadStructureOrdinance.GetExpresswayRule(100),
            new CorridorSearchOptions(
                cellSizeMeters: 40.0,
                searchMarginMeters: 240.0),
            constraints);

        Assert.True(route.Waypoints.Any(waypoint => Math.Abs(waypoint.Position.Y) > 80.0));
        Assert.DoesNotContain(
            route.Waypoints,
            waypoint => constraints.IsBlocked(waypoint.Position));
    }

    [Fact]
    public void FindRoute_FourPercentStraightGrade_RequiresExceptionalModeWhenNoDetourExists()
    {
        TerrainAwareCorridorRouter router = new();
        DelegateTerrainSampler terrain = new(point => point.X * 0.04);
        CorridorSearchOptions options = new(
            cellSizeMeters: 40.0,
            searchMarginMeters: 0.0);

        Assert.Throws<InvalidOperationException>(() =>
            router.FindRoute(
                new PlanarPoint(0.0, 0.0),
                new PlanarPoint(200.0, 0.0),
                terrain,
                JapanRoadStructureOrdinance.GetExpresswayRule(100),
                options,
                allowExceptionalGrade: false));

        CorridorRoute exceptionalRoute = router.FindRoute(
            new PlanarPoint(0.0, 0.0),
            new PlanarPoint(200.0, 0.0),
            terrain,
            JapanRoadStructureOrdinance.GetExpresswayRule(100),
            options,
            allowExceptionalGrade: true);

        Assert.Equal(4.0, exceptionalRoute.MaximumAbsoluteGradePercent, 6);
    }

    private sealed class DelegateTerrainSampler : ITerrainSampler
    {
        private readonly Func<PlanarPoint, double> _sampler;

        public DelegateTerrainSampler(Func<PlanarPoint, double> sampler)
        {
            _sampler = sampler;
        }

        public double GetElevationMeters(PlanarPoint point)
        {
            return _sampler(point);
        }
    }

    private sealed class RectangleConstraintProvider : ICorridorConstraintProvider
    {
        private readonly double _minimumX;
        private readonly double _maximumX;
        private readonly double _minimumY;
        private readonly double _maximumY;

        public RectangleConstraintProvider(
            double minimumX,
            double maximumX,
            double minimumY,
            double maximumY)
        {
            _minimumX = minimumX;
            _maximumX = maximumX;
            _minimumY = minimumY;
            _maximumY = maximumY;
        }

        public bool IsBlocked(PlanarPoint point)
        {
            return
                point.X >= _minimumX &&
                point.X <= _maximumX &&
                point.Y >= _minimumY &&
                point.Y <= _maximumY;
        }

        public double GetAdditionalCost(PlanarPoint point)
        {
            return 0.0;
        }
    }
}
