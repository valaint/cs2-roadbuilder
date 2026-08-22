using RealRoadBuilder.Core.Alignment;

namespace RealRoadBuilder.Core.Geometry;

public sealed class SpiralCornerGeometry
{
    public SpiralCornerGeometry(
        PlanarPoint intersection,
        PlanarPoint transitionStart,
        PlanarPoint transitionEnd,
        TransitionSpiralElement entrySpiral,
        CircularArcElement circularArc,
        TransitionSpiralElement exitSpiral,
        double tangentLengthMeters,
        double radiusMeters,
        double transitionLengthMeters,
        double deflectionAngleRadians)
    {
        Intersection = intersection;
        TransitionStart = transitionStart;
        TransitionEnd = transitionEnd;
        EntrySpiral = entrySpiral;
        CircularArc = circularArc;
        ExitSpiral = exitSpiral;
        TangentLengthMeters = tangentLengthMeters;
        RadiusMeters = radiusMeters;
        TransitionLengthMeters = transitionLengthMeters;
        DeflectionAngleRadians = deflectionAngleRadians;
    }

    public PlanarPoint Intersection { get; }

    public PlanarPoint TransitionStart { get; }

    public PlanarPoint TransitionEnd { get; }

    public TransitionSpiralElement EntrySpiral { get; }

    public CircularArcElement CircularArc { get; }

    public TransitionSpiralElement ExitSpiral { get; }

    public double TangentLengthMeters { get; }

    public double RadiusMeters { get; }

    public double TransitionLengthMeters { get; }

    public double DeflectionAngleRadians { get; }
}
