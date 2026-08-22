using System;

namespace RealRoadBuilder.Core.Vertical;

/// <summary>
/// Symmetric parabolic vertical curve with linearly changing grade.
/// </summary>
public sealed class ParabolicVerticalCurveElement : VerticalAlignmentElement
{
    private const double GradeChangeTolerance = 1e-12;

    public ParabolicVerticalCurveElement(
        ProfilePoint start,
        double lengthMeters,
        double startGradePercent,
        double endGradePercent)
        : base(start.StationMeters, start.StationMeters + ValidateLength(lengthMeters))
    {
        if (double.IsNaN(startGradePercent) || double.IsInfinity(startGradePercent))
        {
            throw new ArgumentOutOfRangeException(nameof(startGradePercent));
        }

        if (double.IsNaN(endGradePercent) || double.IsInfinity(endGradePercent))
        {
            throw new ArgumentOutOfRangeException(nameof(endGradePercent));
        }

        if (Math.Abs(endGradePercent - startGradePercent) < GradeChangeTolerance)
        {
            throw new ArgumentException(
                "A vertical curve requires a change in grade. Use a constant grade element when the grade does not change.");
        }

        Start = start;
        StartGradePercent = startGradePercent;
        EndGradePercent = endGradePercent;

        double averageGradeDecimal =
            ((startGradePercent + endGradePercent) * 0.5) / 100.0;

        End = new ProfilePoint(
            EndStationMeters,
            start.ElevationMeters + (averageGradeDecimal * LengthMeters));
    }

    public ProfilePoint Start { get; }

    public ProfilePoint End { get; }

    public double StartGradePercent { get; }

    public double EndGradePercent { get; }

    public VerticalCurveType CurveType =>
        EndGradePercent < StartGradePercent
            ? VerticalCurveType.Crest
            : VerticalCurveType.Sag;

    public override double StartElevationMeters => Start.ElevationMeters;

    public override double EndElevationMeters => End.ElevationMeters;

    public double GradeChangeDecimal =>
        (EndGradePercent - StartGradePercent) / 100.0;

    public double GradeRatePerMeter => GradeChangeDecimal / LengthMeters;

    /// <summary>
    /// Minimum geometric radius of curvature along the parabola.
    /// For highway grades this is very close to L / |delta grade|, while
    /// retaining the exact (1 + grade^2)^(3/2) curvature factor.
    /// </summary>
    public double MinimumRadiusMeters
    {
        get
        {
            double minimumAbsoluteGradeDecimal = GetMinimumAbsoluteGradeDecimal();
            double numerator = Math.Pow(
                1.0 + (minimumAbsoluteGradeDecimal * minimumAbsoluteGradeDecimal),
                1.5);

            return numerator / Math.Abs(GradeRatePerMeter);
        }
    }

    public override double ElevationAt(double stationMeters)
    {
        double localStationMeters = ValidateAndGetLocalStation(stationMeters);
        double startGradeDecimal = StartGradePercent / 100.0;

        return StartElevationMeters
            + (startGradeDecimal * localStationMeters)
            + (0.5 * GradeRatePerMeter * localStationMeters * localStationMeters);
    }

    public override double GradePercentAt(double stationMeters)
    {
        double localStationMeters = ValidateAndGetLocalStation(stationMeters);
        double gradeDecimal =
            (StartGradePercent / 100.0) + (GradeRatePerMeter * localStationMeters);

        return gradeDecimal * 100.0;
    }

    private double GetMinimumAbsoluteGradeDecimal()
    {
        double startGradeDecimal = StartGradePercent / 100.0;
        double endGradeDecimal = EndGradePercent / 100.0;

        if ((startGradeDecimal <= 0.0 && endGradeDecimal >= 0.0) ||
            (startGradeDecimal >= 0.0 && endGradeDecimal <= 0.0))
        {
            return 0.0;
        }

        return Math.Min(Math.Abs(startGradeDecimal), Math.Abs(endGradeDecimal));
    }

    private static double ValidateLength(double lengthMeters)
    {
        if (double.IsNaN(lengthMeters) ||
            double.IsInfinity(lengthMeters) ||
            lengthMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(lengthMeters));
        }

        return lengthMeters;
    }
}
