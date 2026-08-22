using RealRoadBuilder.Core.Evaluation;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Planning;
using RealRoadBuilder.Core.Standards;
using RealRoadBuilder.Core.Terrain;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class HighwayPlanningEngineTests
{
    [Fact]
    public void Plan_FlatTerrain_ProducesOneSuccessfulAtGradeAttempt()
    {
        HighwayPlanningOptions options = new()
        {
            MaximumIterations = 3,
            StopWhenNoMajorStructures = true,
            PreliminaryProfileSampleIntervalMeters = 20.0,
            VerticalOptimizationIterations = 50,
        };
        options.Construction.SampleIntervalMeters = 10.0;

        HighwayPlanningEngine engine = new();
        HighwayPlanningResult result = engine.Plan(
            new PlanarPoint(0.0, 0.0),
            new PlanarPoint(800.0, 0.0),
            new FlatTerrainSampler(),
            JapanRoadStructureOrdinance.GetExpresswayRule(100),
            options);

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Attempts);
        Assert.NotNull(result.BestAttempt);
        Assert.True(result.BestAttempt!.IsSuccessful);
        Assert.NotNull(result.BestAttempt.ConstructionEvaluation);
        Assert.Equal(
            0.0,
            result.BestAttempt.ConstructionEvaluation!.BridgeLengthMeters,
            6);
        Assert.Equal(
            0.0,
            result.BestAttempt.ConstructionEvaluation.TunnelLengthMeters,
            6);
        Assert.Equal(
            ConstructionSectionType.AtGrade,
            result.BestAttempt.ConstructionEvaluation.Segments[0].SectionType);
    }

    private sealed class FlatTerrainSampler : ITerrainSampler
    {
        public double GetElevationMeters(PlanarPoint position) => 0.0;
    }
}
