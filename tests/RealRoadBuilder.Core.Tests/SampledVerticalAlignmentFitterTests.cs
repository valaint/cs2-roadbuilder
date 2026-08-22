using RealRoadBuilder.Core.Fitting;
using RealRoadBuilder.Core.Standards;
using RealRoadBuilder.Core.Terrain;
using RealRoadBuilder.Core.Vertical;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class SampledVerticalAlignmentFitterTests
{
    [Fact]
    public void Fit_WideCrestProfile_BuildsStandardsValidParabolicCurve()
    {
        CorridorVerticalProfile profile = CreateProfile(
            (0.0, 0.0),
            (1000.0, 20.0),
            (2000.0, 0.0));

        VerticalAlignmentFitResult result = SampledVerticalAlignmentFitter.Fit(
            profile,
            targetLengthMeters: 2000.0,
            JapanRoadStructureOrdinance.GetExpresswayRule(100),
            new VerticalFitOptions
            {
                ElevationToleranceMeters = 0.01,
            });

        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Alignment);
        Assert.Single(result.Corners);
        Assert.Equal(VerticalCurveType.Crest, result.Corners[0].Curve.CurveType);
        Assert.True(result.Corners[0].Curve.MinimumRadiusMeters >= 6500.0);
        Assert.DoesNotContain(
            result.Diagnostics,
            diagnostic => diagnostic.Severity == EngineeringDiagnosticSeverity.Error);
    }

    [Fact]
    public void Fit_ShortCrestProfile_ReturnsFitFailure()
    {
        CorridorVerticalProfile profile = CreateProfile(
            (0.0, 0.0),
            (100.0, 2.0),
            (200.0, 0.0));

        VerticalAlignmentFitResult result = SampledVerticalAlignmentFitter.Fit(
            profile,
            targetLengthMeters: 200.0,
            JapanRoadStructureOrdinance.GetExpresswayRule(100),
            new VerticalFitOptions
            {
                ElevationToleranceMeters = 0.01,
            });

        Assert.False(result.IsSuccessful);
        Assert.Null(result.Alignment);
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code == "VERTICAL_CURVE_FIT_FAILED");
    }

    [Fact]
    public void Fit_FlatProfile_ReducesToSingleConstantGradeElement()
    {
        CorridorVerticalProfile profile = CreateProfile(
            (0.0, 10.0),
            (500.0, 10.0),
            (1000.0, 10.0));

        VerticalAlignmentFitResult result = SampledVerticalAlignmentFitter.Fit(
            profile,
            targetLengthMeters: 1000.0,
            JapanRoadStructureOrdinance.GetExpresswayRule(100));

        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Alignment);
        Assert.Empty(result.Corners);
        Assert.Single(result.Alignment!.Elements);
        Assert.IsType<ConstantGradeElement>(result.Alignment.Elements[0]);
    }

    private static CorridorVerticalProfile CreateProfile(
        params (double StationMeters, double ElevationMeters)[] points)
    {
        DesignedProfileSample[] samples = new DesignedProfileSample[points.Length];

        for (int index = 0; index < points.Length; index++)
        {
            double gradePercent = 0.0;
            if (index > 0)
            {
                double stationDelta =
                    points[index].StationMeters - points[index - 1].StationMeters;
                double elevationDelta =
                    points[index].ElevationMeters - points[index - 1].ElevationMeters;
                gradePercent = (elevationDelta / stationDelta) * 100.0;
            }

            samples[index] = new DesignedProfileSample(
                points[index].StationMeters,
                terrainElevationMeters: points[index].ElevationMeters,
                designElevationMeters: points[index].ElevationMeters,
                gradeFromPreviousPercent: gradePercent);
        }

        return new CorridorVerticalProfile(samples);
    }
}
