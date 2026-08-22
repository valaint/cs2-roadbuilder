using System.Linq;
using RealRoadBuilder.Core.Design;
using RealRoadBuilder.Core.Generation;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Standards;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class SeedAlignmentGeneratorTests
{
    [Fact]
    public void Generate_LongCorridor_ProducesDirectAndTwoValidBypassSeeds()
    {
        RoadDesignRule rule = JapanRoadStructureOrdinance.GetExpresswayRule(100);

        var candidates = SeedAlignmentGenerator.Generate(
            new PlanarPoint(0.0, 0.0),
            new PlanarPoint(5000.0, 0.0),
            rule,
            AlignmentGenerationMode.Balanced);

        Assert.Equal(3, candidates.Count);
        Assert.Equal("Direct", candidates[0].Name);
        Assert.All(candidates, candidate => Assert.True(candidate.Validation.IsValid));
        Assert.Contains(candidates, candidate => candidate.Name == "Left bypass");
        Assert.Contains(candidates, candidate => candidate.Name == "Right bypass");
        Assert.Equal(
            candidates.OrderBy(candidate => candidate.Score.TotalCost).Select(candidate => candidate.Name),
            candidates.Select(candidate => candidate.Name));
    }
}
