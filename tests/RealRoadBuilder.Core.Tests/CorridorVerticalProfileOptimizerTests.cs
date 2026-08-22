using System;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Standards;
using RealRoadBuilder.Core.Terrain;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class CorridorVerticalProfileOptimizerTests
{
    [Fact]
    public void Optimize_SharpTerrainCrest_SmoothsProfileWithinNormalGradeLimit()
    {
        CorridorProfileSample[] terrainProfile =
        {
            new(0.0, new PlanarPoint(0.0, 0.0), 0.0, 0.0),
            new(100.0, new PlanarPoint(100.0, 0.0), 20.0, 20.0),
            new(200.0, new PlanarPoint(200.0, 0.0), 0.0, -20.0),
        };

        CorridorVerticalProfileOptimizer optimizer = new();
        CorridorVerticalProfile profile = optimizer.Optimize(
            terrainProfile,
            JapanRoadStructureOrdinance.GetExpresswayRule(100),
            iterations: 400,
            terrainAttraction: 0.2);

        Assert.Equal(0.0, profile.Samples[0].DesignElevationMeters, 6);
        Assert.Equal(0.0, profile.Samples[2].DesignElevationMeters, 6);
        Assert.True(profile.Samples[1].DesignElevationMeters < 1.0);
        Assert.True(profile.MaximumAbsoluteGradePercent <= 3.0 + 1e-5);
        Assert.True(profile.MaximumCutMeters > 19.0);
    }

    [Fact]
    public void Optimize_EndpointAverageGradeAboveLimit_ThrowsBeforeIteration()
    {
        CorridorProfileSample[] terrainProfile =
        {
            new(0.0, new PlanarPoint(0.0, 0.0), 0.0, 0.0),
            new(100.0, new PlanarPoint(100.0, 0.0), 4.0, 4.0),
        };

        CorridorVerticalProfileOptimizer optimizer = new();

        Assert.Throws<InvalidOperationException>(() =>
            optimizer.Optimize(
                terrainProfile,
                JapanRoadStructureOrdinance.GetExpresswayRule(100),
                allowExceptionalGrade: false));

        CorridorVerticalProfile exceptionalProfile = optimizer.Optimize(
            terrainProfile,
            JapanRoadStructureOrdinance.GetExpresswayRule(100),
            allowExceptionalGrade: true);

        Assert.Equal(4.0, exceptionalProfile.MaximumAbsoluteGradePercent, 6);
    }
}
