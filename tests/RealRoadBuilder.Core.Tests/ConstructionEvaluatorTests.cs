using System.Linq;
using RealRoadBuilder.Core.Alignment;
using RealRoadBuilder.Core.Evaluation;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Terrain;
using RealRoadBuilder.Core.Vertical;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class ConstructionEvaluatorTests
{
    [Fact]
    public void Evaluate_FlatTerrain_ClassifiesAtGrade()
    {
        var (horizontal, vertical) = CreateFlatAlignment(200.0, 0.0);

        ConstructionEvaluationResult result = ConstructionEvaluator.Evaluate(
            horizontal,
            vertical,
            new FunctionalTerrainSampler(_ => 0.0),
            new ConstructionEvaluationOptions { SampleIntervalMeters = 10.0 });

        Assert.Single(result.Segments);
        Assert.Equal(ConstructionSectionType.AtGrade, result.Segments[0].SectionType);
        Assert.Equal(200.0, result.AtGradeLengthMeters, 6);
        Assert.Equal(0.0, result.TotalCutVolumeCubicMeters, 6);
        Assert.Equal(0.0, result.TotalFillVolumeCubicMeters, 6);
    }

    [Fact]
    public void Evaluate_DeepContinuousValley_ClassifiesBridge()
    {
        var (horizontal, vertical) = CreateFlatAlignment(200.0, 0.0);

        ConstructionEvaluationResult result = ConstructionEvaluator.Evaluate(
            horizontal,
            vertical,
            new FunctionalTerrainSampler(_ => -10.0),
            new ConstructionEvaluationOptions
            {
                SampleIntervalMeters = 10.0,
                BridgeDepthThresholdMeters = 8.0,
                MinimumBridgeRunMeters = 40.0,
            });

        Assert.Single(result.Segments);
        Assert.Equal(ConstructionSectionType.Bridge, result.Segments[0].SectionType);
        Assert.Equal(200.0, result.BridgeLengthMeters, 6);
        Assert.Equal(0.0, result.TotalFillVolumeCubicMeters, 6);
        Assert.Equal(4000.0, result.TotalRelativeCost, 6);
    }

    [Fact]
    public void Evaluate_DeepContinuousHill_ClassifiesTunnel()
    {
        var (horizontal, vertical) = CreateFlatAlignment(200.0, 0.0);

        ConstructionEvaluationResult result = ConstructionEvaluator.Evaluate(
            horizontal,
            vertical,
            new FunctionalTerrainSampler(_ => 15.0),
            new ConstructionEvaluationOptions
            {
                SampleIntervalMeters = 10.0,
                TunnelDepthThresholdMeters = 12.0,
                MinimumTunnelRunMeters = 40.0,
            });

        Assert.Single(result.Segments);
        Assert.Equal(ConstructionSectionType.Tunnel, result.Segments[0].SectionType);
        Assert.Equal(200.0, result.TunnelLengthMeters, 6);
    }

    [Fact]
    public void Evaluate_ShortDeepValley_DowngradesBridgeToEmbankment()
    {
        var (horizontal, vertical) = CreateFlatAlignment(200.0, 0.0);
        FunctionalTerrainSampler terrain = new(position =>
            position.X >= 90.0 && position.X <= 110.0 ? -10.0 : 0.0);

        ConstructionEvaluationResult result = ConstructionEvaluator.Evaluate(
            horizontal,
            vertical,
            terrain,
            new ConstructionEvaluationOptions
            {
                SampleIntervalMeters = 10.0,
                BridgeDepthThresholdMeters = 8.0,
                MinimumBridgeRunMeters = 50.0,
            });

        Assert.Equal(0.0, result.BridgeLengthMeters, 6);
        Assert.True(result.EmbankmentLengthMeters > 0.0);
        Assert.True(result.TotalFillVolumeCubicMeters > 0.0);
    }

    [Fact]
    public void Evaluate_ForcedBridge_PreservesShortStructureRun()
    {
        var (horizontal, vertical) = CreateFlatAlignment(200.0, 0.0);

        ConstructionEvaluationResult result = ConstructionEvaluator.Evaluate(
            horizontal,
            vertical,
            new FunctionalTerrainSampler(_ => 0.0),
            new ConstructionEvaluationOptions
            {
                SampleIntervalMeters = 10.0,
                MinimumBridgeRunMeters = 100.0,
            },
            new WaterCrossingRequirementProvider());

        Assert.True(result.BridgeLengthMeters > 0.0);
        Assert.Contains(
            result.Segments,
            segment => segment.SectionType == ConstructionSectionType.Bridge && segment.ForcedByRequirement);
    }

    [Fact]
    public void Evaluate_Embankment_ComputesPositiveFillVolume()
    {
        var (horizontal, vertical) = CreateFlatAlignment(100.0, 2.0);
        ConstructionEvaluationOptions options = new()
        {
            SampleIntervalMeters = 10.0,
            FormationWidthMeters = 20.0,
            SideSlopeHorizontalToVertical = 1.0,
            BridgeDepthThresholdMeters = 8.0,
        };

        ConstructionEvaluationResult result = ConstructionEvaluator.Evaluate(
            horizontal,
            vertical,
            new FunctionalTerrainSampler(_ => 0.0),
            options);

        // Constant 2 m fill: cross-section area = 20*2 + 1*2^2 = 44 m².
        Assert.Equal(4400.0, result.TotalFillVolumeCubicMeters, 6);
        Assert.Equal(ConstructionSectionType.Embankment, result.Segments.Single().SectionType);
    }

    private static (HorizontalAlignment Horizontal, VerticalAlignment Vertical) CreateFlatAlignment(
        double lengthMeters,
        double elevationMeters)
    {
        HorizontalAlignment horizontal = new(new HorizontalAlignmentElement[]
        {
            new TangentElement(
                new PlanarPoint(0.0, 0.0),
                new PlanarPoint(lengthMeters, 0.0)),
        });
        VerticalAlignment vertical = new(new VerticalAlignmentElement[]
        {
            new ConstantGradeElement(
                new ProfilePoint(0.0, elevationMeters),
                new ProfilePoint(lengthMeters, elevationMeters)),
        });

        return (horizontal, vertical);
    }

    private sealed class FunctionalTerrainSampler : ITerrainSampler
    {
        private readonly System.Func<PlanarPoint, double> _function;

        public FunctionalTerrainSampler(System.Func<PlanarPoint, double> function)
        {
            _function = function;
        }

        public double GetElevationMeters(PlanarPoint position) => _function(position);
    }

    private sealed class WaterCrossingRequirementProvider : IStructureRequirementProvider
    {
        public StructureRequirement GetRequirement(PlanarPoint position)
        {
            return position.X >= 95.0 && position.X <= 105.0
                ? StructureRequirement.Bridge
                : StructureRequirement.None;
        }
    }
}
