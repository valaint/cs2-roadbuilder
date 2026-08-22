using RealRoadBuilder.Core.Fitting;
using RealRoadBuilder.Core.Geometry;
using Xunit;

namespace RealRoadBuilder.Core.Tests;

public sealed class CorridorControlPointReducerTests
{
    [Fact]
    public void Reduce_SmallDeviationInsideTolerance_RemovesIntermediatePoint()
    {
        PlanarPoint[] points =
        {
            new(0.0, 0.0),
            new(100.0, 5.0),
            new(200.0, 0.0),
        };

        var reduced = CorridorControlPointReducer.Reduce(
            points,
            toleranceMeters: 10.0);

        Assert.Equal(2, reduced.Count);
        Assert.Equal(points[0], reduced[0]);
        Assert.Equal(points[2], reduced[1]);
    }

    [Fact]
    public void Reduce_MeaningfulDeviationOutsideTolerance_PreservesIntermediatePoint()
    {
        PlanarPoint[] points =
        {
            new(0.0, 0.0),
            new(100.0, 5.0),
            new(200.0, 0.0),
        };

        var reduced = CorridorControlPointReducer.Reduce(
            points,
            toleranceMeters: 1.0);

        Assert.Equal(3, reduced.Count);
        Assert.Equal(points[1], reduced[1]);
    }
}
