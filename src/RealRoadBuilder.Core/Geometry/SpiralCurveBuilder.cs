using System.Collections.Generic;
using RealRoadBuilder.Core.Alignment;

namespace RealRoadBuilder.Core.Geometry;

/// <summary>
/// Builds a symmetric tangent-spiral-circular-spiral-tangent horizontal curve.
/// </summary>
public static class SpiralCurveBuilder
{
    public static HorizontalAlignment Build(
        PlanarPoint start,
        PlanarPoint intersection,
        PlanarPoint end,
        double radiusMeters,
        double transitionLengthMeters)
    {
        SpiralCornerGeometry corner = SpiralCornerBuilder.Build(
            start,
            intersection,
            end,
            radiusMeters,
            transitionLengthMeters);

        List<HorizontalAlignmentElement> elements = new()
        {
            new TangentElement(start, corner.TransitionStart),
            corner.EntrySpiral,
            corner.CircularArc,
            corner.ExitSpiral,
            new TangentElement(corner.TransitionEnd, end),
        };

        return new HorizontalAlignment(elements);
    }
}
