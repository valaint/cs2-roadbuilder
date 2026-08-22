using System;

namespace RealRoadBuilder.Core.Terrain;

public sealed class RerouteFeedbackOptions
{
    private double _zoneRadiusMeters = 100.0;
    private double _minimumZoneSpacingMeters = 50.0;
    private double _bridgePenalty = 80.0;
    private double _tunnelPenalty = 140.0;
    private double _fitFailurePenalty = 220.0;

    public double ZoneRadiusMeters
    {
        get => _zoneRadiusMeters;
        set => _zoneRadiusMeters = ValidatePositive(value, nameof(ZoneRadiusMeters));
    }

    public double MinimumZoneSpacingMeters
    {
        get => _minimumZoneSpacingMeters;
        set => _minimumZoneSpacingMeters = ValidatePositive(value, nameof(MinimumZoneSpacingMeters));
    }

    public double BridgePenalty
    {
        get => _bridgePenalty;
        set => _bridgePenalty = ValidateNonNegative(value, nameof(BridgePenalty));
    }

    public double TunnelPenalty
    {
        get => _tunnelPenalty;
        set => _tunnelPenalty = ValidateNonNegative(value, nameof(TunnelPenalty));
    }

    public double FitFailurePenalty
    {
        get => _fitFailurePenalty;
        set => _fitFailurePenalty = ValidateNonNegative(value, nameof(FitFailurePenalty));
    }

    /// <summary>
    /// If true, failed horizontal fit locations become hard exclusion zones.
    /// Default false keeps feedback advisory and allows the router to reuse the
    /// area when no better corridor exists.
    /// </summary>
    public bool BlockHorizontalFitFailures { get; set; }

    private static double ValidatePositive(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(name);
        }

        return value;
    }

    private static double ValidateNonNegative(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
        {
            throw new ArgumentOutOfRangeException(name);
        }

        return value;
    }
}
