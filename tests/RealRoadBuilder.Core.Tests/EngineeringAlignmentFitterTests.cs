using RealRoadBuilder.Core.Fitting;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Standards;
using RealRoadBuilder.Core.Terrain;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class EngineeringAlignmentFitterTests
{
    [Fact]
    public void Fit_LongFlatNinetyDegreeCorridor_ReturnsCombinedEngineeringAlignment()
    {
        CorridorRoute route = CreateRoute(
            new PlanarPoint(0.0, 0.0),
            new PlanarPoint(1500.0, 0.0),
            new PlanarPoint(1500.0, 1500.0));
        CorridorVerticalProfile verticalProfile = CreateFlatProfile(
            route.TotalLengthMeters,
            elevationMeters: 10.0);

        EngineeringAlignmentFitOptions options = new();
        options.Horizontal.ControlPointToleranceMeters = 1.0;
        options.Vertical.ElevationToleranceMeters = 0.01;

        EngineeringAlignmentFitResult result = EngineeringAlignmentFitter.Fit(
            route,
            verticalProfile,
            JapanRoadStructureOrdinance.GetExpresswayRule(100),
            options);

        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Horizontal.Alignment);
        Assert.NotNull(result.Vertical);
        Assert.NotNull(result.Vertical!.Alignment);
        Assert.Equal(
            result.Horizontal.Alignment!.TotalLengthMeters,
            result.Vertical.Alignment!.TotalLengthMeters,
            4);
        Assert.DoesNotContain(
            result.Diagnostics,
            diagnostic => diagnostic.Severity == EngineeringDiagnosticSeverity.Error);
    }

    [Fact]
    public void Fit_HorizontalGeometryCannotFit_StopsBeforeVerticalFit()
    {
        CorridorRoute route = CreateRoute(
            new PlanarPoint(0.0, 0.0),
            new PlanarPoint(300.0, 0.0),
            new PlanarPoint(300.0, 300.0));
        CorridorVerticalProfile verticalProfile = CreateFlatProfile(
            route.TotalLengthMeters,
            elevationMeters: 0.0);

        EngineeringAlignmentFitOptions options = new();
        options.Horizontal.ControlPointToleranceMeters = 1.0;

        EngineeringAlignmentFitResult result = EngineeringAlignmentFitter.Fit(
            route,
            verticalProfile,
            JapanRoadStructureOrdinance.GetExpresswayRule(100),
            options);

        Assert.False(result.IsSuccessful);
        Assert.Null(result.Vertical);
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code == "ENGINEERING_FIT_STOPPED_AFTER_HORIZONTAL");
    }

    private static CorridorRoute CreateRoute(params PlanarPoint[] points)
    {
        CorridorWaypoint[] waypoints = new CorridorWaypoint[points.Length];
        for (int index = 0; index < points.Length; index++)
        {
            waypoints[index] = new CorridorWaypoint(points[index], 0.0);
        }

        return new CorridorRoute(
            waypoints,
            totalCost: 0.0,
            expandedStates: 0);
    }

    private static CorridorVerticalProfile CreateFlatProfile(
        double lengthMeters,
        double elevationMeters)
    {
        DesignedProfileSample[] samples =
        {
            new(
                stationMeters: 0.0,
                terrainElevationMeters: elevationMeters,
                designElevationMeters: elevationMeters,
                gradeFromPreviousPercent: 0.0),
            new(
                stationMeters: lengthMeters / 2.0,
                terrainElevationMeters: elevationMeters,
                designElevationMeters: elevationMeters,
                gradeFromPreviousPercent: 0.0),
            new(
                stationMeters: lengthMeters,
                terrainElevationMeters: elevationMeters,
                designElevationMeters: elevationMeters,
                gradeFromPreviousPercent: 0.0),
        };

        return new CorridorVerticalProfile(samples);
    }
}
