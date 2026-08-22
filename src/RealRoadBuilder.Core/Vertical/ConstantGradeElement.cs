using System;

namespace RealRoadBuilder.Core.Vertical;

public sealed class ConstantGradeElement : VerticalAlignmentElement
{
    public ConstantGradeElement(ProfilePoint start, ProfilePoint end)
        : base(start.StationMeters, end.StationMeters)
    {
        Start = start;
        End = end;
        GradePercent = 100.0 *
            (end.ElevationMeters - start.ElevationMeters) /
            (end.StationMeters - start.StationMeters);
    }

    public ProfilePoint Start { get; }

    public ProfilePoint End { get; }

    public double GradePercent { get; }

    public override double StartElevationMeters => Start.ElevationMeters;

    public override double EndElevationMeters => End.ElevationMeters;

    public override double ElevationAt(double stationMeters)
    {
        double localStationMeters = ValidateAndGetLocalStation(stationMeters);
        return StartElevationMeters + ((GradePercent / 100.0) * localStationMeters);
    }

    public override double GradePercentAt(double stationMeters)
    {
        ValidateAndGetLocalStation(stationMeters);
        return GradePercent;
    }
}
