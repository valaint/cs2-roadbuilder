using System;
using System.Linq;
using RealRoadBuilder.Core.Alignment;
using RealRoadBuilder.Core.Evaluation;
using RealRoadBuilder.Core.Fitting;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Standards;
using RealRoadBuilder.Core.Terrain;
using RealRoadBuilder.Core.Vertical;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class RerouteFeedbackTests
{
    [Fact]
    public void FeedbackZone_HighSoftCost_PushesAStarAwayFromDirectRoute()
    {
        TerrainAwareCorridorRouter router = new();
        CorridorSearchOptions searchOptions = new(
            cellSizeMeters: 40.0,
            searchMarginMeters: 300.0,
            maximumExpandedStates: 50000,
            gradePenaltyWeight: 0.0,
            turnPenaltyWeight: 10.0,
            additionalCostWeight: 1.0);
        RerouteFeedbackConstraintProvider feedback = new(new[]
        {
            new RerouteFeedbackZone(
                new PlanarPoint(200.0, 0.0),
                radiusMeters: 120.0,
                peakAdditionalCost: 1000.0),
        });

        CorridorRoute route = router.FindRoute(
            new PlanarPoint(0.0, 0.0),
            new PlanarPoint(400.0, 0.0),
            new FlatTerrainSampler(),
            JapanRoadStructureOrdinance.GetExpresswayRule(60),
            searchOptions,
            feedback);

        Assert.Contains(route.Waypoints, waypoint => Math.Abs(waypoint.Position.Y) >= 40.0);
    }

    [Fact]
    public void Build_DeepBridgeEvaluation_CreatesBridgePenaltyZones()
    {
        HorizontalAlignment horizontal = new(new HorizontalAlignmentElement[]
        {
            new TangentElement(new PlanarPoint(0.0, 0.0), new PlanarPoint(200.0, 0.0)),
        });
        VerticalAlignment vertical = new(new VerticalAlignmentElement[]
        {
            new ConstantGradeElement(new ProfilePoint(0.0, 0.0), new ProfilePoint(200.0, 0.0)),
        });
        ConstructionEvaluationResult evaluation = ConstructionEvaluator.Evaluate(
            horizontal,
            vertical,
            new ConstantTerrainSampler(-10.0),
            new ConstructionEvaluationOptions
            {
                SampleIntervalMeters = 10.0,
                BridgeDepthThresholdMeters = 8.0,
                MinimumBridgeRunMeters = 20.0,
            });

        RerouteFeedbackConstraintProvider feedback = RerouteFeedbackBuilder.Build(
            constructionEvaluation: evaluation,
            horizontalFit: null,
            options: new RerouteFeedbackOptions
            {
                ZoneRadiusMeters = 80.0,
                MinimumZoneSpacingMeters = 50.0,
                BridgePenalty = 100.0,
            });

        Assert.NotEmpty(feedback.Zones);
        Assert.True(feedback.GetAdditionalCost(new PlanarPoint(100.0, 0.0)) > 0.0);
        Assert.All(feedback.Zones, zone => Assert.False(zone.Blocked));
    }

    [Fact]
    public void Build_FailedHorizontalFit_FallsBackToInteriorControlPoints()
    {
        PlanarPoint[] controlPoints =
        {
            new(0.0, 0.0),
            new(100.0, 0.0),
            new(100.0, 100.0),
        };
        HorizontalAlignmentFitResult failedFit = new(
            alignment: null,
            controlPoints,
            corners: Array.Empty<SpiralCornerGeometry>(),
            diagnostics: new[]
            {
                new EngineeringDiagnostic(
                    "HORIZONTAL_CORNER_FIT_FAILED",
                    EngineeringDiagnosticSeverity.Error,
                    "test failure"),
            });

        RerouteFeedbackConstraintProvider feedback = RerouteFeedbackBuilder.Build(
            constructionEvaluation: null,
            horizontalFit: failedFit,
            options: new RerouteFeedbackOptions
            {
                ZoneRadiusMeters = 75.0,
                FitFailurePenalty = 250.0,
            });

        Assert.Single(feedback.Zones);
        Assert.Equal(controlPoints[1], feedback.Zones.Single().Center);
        Assert.True(feedback.GetAdditionalCost(controlPoints[1]) >= 250.0 - 1e-9);
    }

    private sealed class FlatTerrainSampler : ITerrainSampler
    {
        public double GetElevationMeters(PlanarPoint position) => 0.0;
    }

    private sealed class ConstantTerrainSampler : ITerrainSampler
    {
        private readonly double _elevationMeters;

        public ConstantTerrainSampler(double elevationMeters)
        {
            _elevationMeters = elevationMeters;
        }

        public double GetElevationMeters(PlanarPoint position) => _elevationMeters;
    }
}
