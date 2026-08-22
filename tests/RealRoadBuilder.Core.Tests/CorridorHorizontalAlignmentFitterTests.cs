using RealRoadBuilder.Core.Alignment;
using RealRoadBuilder.Core.Fitting;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Standards;
using RealRoadBuilder.Core.Terrain;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class CorridorHorizontalAlignmentFitterTests
{
    [Fact]
    public void Fit_LongNinetyDegreeCorridor_BuildsValidatedSpiralArcSpiralCurve()
    {
        CorridorRoute route = CreateRoute(
            new PlanarPoint(0.0, 0.0),
            new PlanarPoint(1500.0, 0.0),
            new PlanarPoint(1500.0, 1500.0));

        HorizontalAlignmentFitResult result = CorridorHorizontalAlignmentFitter.Fit(
            route,
            JapanRoadStructureOrdinance.GetExpresswayRule(100),
            new HorizontalFitOptions
            {
                ControlPointToleranceMeters = 1.0,
            });

        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Alignment);
        Assert.Single(result.Corners);
        Assert.Equal(460.0, result.Corners[0].RadiusMeters, 6);
        Assert.Equal(85.0, result.Corners[0].TransitionLengthMeters, 6);
        Assert.Equal(5, result.Alignment!.Elements.Count);
        Assert.IsType<TangentElement>(result.Alignment.Elements[0]);
        Assert.IsType<TransitionSpiralElement>(result.Alignment.Elements[1]);
        Assert.IsType<CircularArcElement>(result.Alignment.Elements[2]);
        Assert.IsType<TransitionSpiralElement>(result.Alignment.Elements[3]);
        Assert.IsType<TangentElement>(result.Alignment.Elements[4]);
        Assert.DoesNotContain(
            result.Diagnostics,
            diagnostic => diagnostic.Severity == EngineeringDiagnosticSeverity.Error);
    }

    [Fact]
    public void Fit_ShortCorridor_ReturnsFitFailureInsteadOfForcingInvalidGeometry()
    {
        CorridorRoute route = CreateRoute(
            new PlanarPoint(0.0, 0.0),
            new PlanarPoint(300.0, 0.0),
            new PlanarPoint(300.0, 300.0));

        HorizontalAlignmentFitResult result = CorridorHorizontalAlignmentFitter.Fit(
            route,
            JapanRoadStructureOrdinance.GetExpresswayRule(100),
            new HorizontalFitOptions
            {
                ControlPointToleranceMeters = 1.0,
            });

        Assert.False(result.IsSuccessful);
        Assert.Null(result.Alignment);
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code == "HORIZONTAL_CORNER_FIT_FAILED");
    }

    [Fact]
    public void Fit_TwoCurvesWithInsufficientSharedTangent_ReturnsOverlapDiagnostic()
    {
        CorridorRoute route = CreateRoute(
            new PlanarPoint(0.0, 0.0),
            new PlanarPoint(1500.0, 0.0),
            new PlanarPoint(1500.0, 700.0),
            new PlanarPoint(3000.0, 700.0));

        HorizontalAlignmentFitResult result = CorridorHorizontalAlignmentFitter.Fit(
            route,
            JapanRoadStructureOrdinance.GetExpresswayRule(100),
            new HorizontalFitOptions
            {
                ControlPointToleranceMeters = 1.0,
            });

        Assert.False(result.IsSuccessful);
        Assert.Null(result.Alignment);
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code == "HORIZONTAL_ADJACENT_CURVES_OVERLAP");
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
}
