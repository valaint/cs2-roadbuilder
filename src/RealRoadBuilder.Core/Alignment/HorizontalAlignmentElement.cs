using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Alignment;

public abstract class HorizontalAlignmentElement
{
    protected HorizontalAlignmentElement(PlanarPoint start, PlanarPoint end)
    {
        Start = start;
        End = end;
    }

    public PlanarPoint Start { get; }

    public PlanarPoint End { get; }

    public abstract double LengthMeters { get; }
}
