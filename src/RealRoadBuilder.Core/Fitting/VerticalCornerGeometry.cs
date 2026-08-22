using RealRoadBuilder.Core.Vertical;

namespace RealRoadBuilder.Core.Fitting;

public sealed class VerticalCornerGeometry
{
    public VerticalCornerGeometry(
        ProfilePoint previousPoint,
        ProfilePoint intersection,
        ProfilePoint nextPoint,
        ProfilePoint curveStart,
        ParabolicVerticalCurveElement curve,
        double requiredRadiusMeters)
    {
        PreviousPoint = previousPoint;
        Intersection = intersection;
        NextPoint = nextPoint;
        CurveStart = curveStart;
        Curve = curve;
        RequiredRadiusMeters = requiredRadiusMeters;
    }

    public ProfilePoint PreviousPoint { get; }

    public ProfilePoint Intersection { get; }

    public ProfilePoint NextPoint { get; }

    public ProfilePoint CurveStart { get; }

    public ParabolicVerticalCurveElement Curve { get; }

    public ProfilePoint CurveEnd => Curve.End;

    public double CurveLengthMeters => Curve.LengthMeters;

    public double HalfCurveLengthMeters => Curve.LengthMeters / 2.0;

    public double RequiredRadiusMeters { get; }
}
