using System;
using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Alignment;

/// <summary>
/// Plan-view Euler spiral (clothoid) with curvature changing linearly with station.
/// Positive curvature turns left; negative curvature turns right.
/// </summary>
public sealed class TransitionSpiralElement : HorizontalAlignmentElement
{
    private const double CurvatureTolerance = 1e-12;

    public TransitionSpiralElement(
        PlanarPoint start,
        double startHeadingRadians,
        double lengthMeters,
        double startCurvaturePerMeter,
        double endCurvaturePerMeter)
        : base(
            start,
            CalculatePointAt(
                start,
                startHeadingRadians,
                lengthMeters,
                startCurvaturePerMeter,
                endCurvaturePerMeter,
                lengthMeters))
    {
        if (lengthMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(lengthMeters));
        }

        if (double.IsNaN(startHeadingRadians) || double.IsInfinity(startHeadingRadians))
        {
            throw new ArgumentOutOfRangeException(nameof(startHeadingRadians));
        }

        if (double.IsNaN(startCurvaturePerMeter) || double.IsInfinity(startCurvaturePerMeter))
        {
            throw new ArgumentOutOfRangeException(nameof(startCurvaturePerMeter));
        }

        if (double.IsNaN(endCurvaturePerMeter) || double.IsInfinity(endCurvaturePerMeter))
        {
            throw new ArgumentOutOfRangeException(nameof(endCurvaturePerMeter));
        }

        if (Math.Abs(endCurvaturePerMeter - startCurvaturePerMeter) < CurvatureTolerance)
        {
            throw new ArgumentException(
                "A transition spiral requires changing curvature. Use a tangent or circular arc for constant curvature.");
        }

        StartHeadingRadians = startHeadingRadians;
        LengthMeters = lengthMeters;
        StartCurvaturePerMeter = startCurvaturePerMeter;
        EndCurvaturePerMeter = endCurvaturePerMeter;
    }

    public double StartHeadingRadians { get; }

    public override double LengthMeters { get; }

    public double StartCurvaturePerMeter { get; }

    public double EndCurvaturePerMeter { get; }

    public double CurvatureRatePerSquareMeter =>
        (EndCurvaturePerMeter - StartCurvaturePerMeter) / LengthMeters;

    public double EndHeadingRadians => HeadingAt(LengthMeters);

    public double CurvatureAt(double stationMeters)
    {
        ValidateStation(stationMeters);
        return StartCurvaturePerMeter + (CurvatureRatePerSquareMeter * stationMeters);
    }

    public double HeadingAt(double stationMeters)
    {
        ValidateStation(stationMeters);

        return StartHeadingRadians
            + (StartCurvaturePerMeter * stationMeters)
            + (0.5 * CurvatureRatePerSquareMeter * stationMeters * stationMeters);
    }

    public PlanarPoint PointAt(double stationMeters)
    {
        ValidateStation(stationMeters);

        return CalculatePointAt(
            Start,
            StartHeadingRadians,
            LengthMeters,
            StartCurvaturePerMeter,
            EndCurvaturePerMeter,
            stationMeters);
    }

    public double MinimumRadiusMeters
    {
        get
        {
            double maximumAbsoluteCurvature = Math.Max(
                Math.Abs(StartCurvaturePerMeter),
                Math.Abs(EndCurvaturePerMeter));

            return maximumAbsoluteCurvature < CurvatureTolerance
                ? double.PositiveInfinity
                : 1.0 / maximumAbsoluteCurvature;
        }
    }

    private void ValidateStation(double stationMeters)
    {
        if (stationMeters < 0.0 || stationMeters > LengthMeters)
        {
            throw new ArgumentOutOfRangeException(nameof(stationMeters));
        }
    }

    private static PlanarPoint CalculatePointAt(
        PlanarPoint start,
        double startHeadingRadians,
        double lengthMeters,
        double startCurvaturePerMeter,
        double endCurvaturePerMeter,
        double stationMeters)
    {
        if (lengthMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(lengthMeters));
        }

        if (stationMeters < 0.0 || stationMeters > lengthMeters)
        {
            throw new ArgumentOutOfRangeException(nameof(stationMeters));
        }

        if (stationMeters == 0.0)
        {
            return start;
        }

        double curvatureRatePerSquareMeter =
            (endCurvaturePerMeter - startCurvaturePerMeter) / lengthMeters;

        int intervalCount = Math.Max(16, (int)Math.Ceiling(stationMeters / 2.0));
        if ((intervalCount & 1) != 0)
        {
            intervalCount++;
        }

        intervalCount = Math.Min(intervalCount, 512);
        double intervalLength = stationMeters / intervalCount;
        double xIntegral = 0.0;
        double yIntegral = 0.0;

        for (int index = 0; index <= intervalCount; index++)
        {
            double station = intervalLength * index;
            double heading = startHeadingRadians
                + (startCurvaturePerMeter * station)
                + (0.5 * curvatureRatePerSquareMeter * station * station);

            double weight;
            if (index == 0 || index == intervalCount)
            {
                weight = 1.0;
            }
            else
            {
                weight = (index & 1) == 0 ? 2.0 : 4.0;
            }

            xIntegral += weight * Math.Cos(heading);
            yIntegral += weight * Math.Sin(heading);
        }

        double scale = intervalLength / 3.0;
        return new PlanarPoint(
            start.X + (xIntegral * scale),
            start.Y + (yIntegral * scale));
    }
}
