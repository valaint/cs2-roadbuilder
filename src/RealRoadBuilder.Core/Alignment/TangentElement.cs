using System;
using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Alignment;

public sealed class TangentElement : HorizontalAlignmentElement
{
    public TangentElement(PlanarPoint start, PlanarPoint end)
        : base(start, end)
    {
        double lengthMeters = start.DistanceTo(end);
        if (lengthMeters <= 1e-9)
        {
            throw new ArgumentException("A tangent element must have non-zero length.");
        }

        LengthMeters = lengthMeters;
        Direction = (end - start).Normalized();
    }

    public override double LengthMeters { get; }

    public PlanarVector Direction { get; }
}
