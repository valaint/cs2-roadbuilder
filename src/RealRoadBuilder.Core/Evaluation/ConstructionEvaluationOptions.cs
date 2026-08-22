using System;

namespace RealRoadBuilder.Core.Evaluation;

public sealed class ConstructionEvaluationOptions
{
    private double _sampleIntervalMeters = 10.0;
    private double _formationWidthMeters = 24.0;
    private double _sideSlopeHorizontalToVertical = 1.5;
    private double _atGradeToleranceMeters = 0.5;
    private double _bridgeDepthThresholdMeters = 8.0;
    private double _tunnelDepthThresholdMeters = 12.0;
    private double _minimumBridgeRunMeters = 40.0;
    private double _minimumTunnelRunMeters = 60.0;
    private double _surfaceCostPerMeter = 1.0;
    private double _earthworkCostPerCubicMeter = 0.02;
    private double _bridgeCostPerMeter = 20.0;
    private double _tunnelCostPerMeter = 45.0;

    public double SampleIntervalMeters
    {
        get => _sampleIntervalMeters;
        set => _sampleIntervalMeters = ValidatePositive(value, nameof(SampleIntervalMeters));
    }

    /// <summary>
    /// Approximate full formation width used only for preliminary earthwork
    /// volume estimation. It is not yet a standards-derived cross section.
    /// </summary>
    public double FormationWidthMeters
    {
        get => _formationWidthMeters;
        set => _formationWidthMeters = ValidatePositive(value, nameof(FormationWidthMeters));
    }

    public double SideSlopeHorizontalToVertical
    {
        get => _sideSlopeHorizontalToVertical;
        set => _sideSlopeHorizontalToVertical = ValidateNonNegative(value, nameof(SideSlopeHorizontalToVertical));
    }

    public double AtGradeToleranceMeters
    {
        get => _atGradeToleranceMeters;
        set => _atGradeToleranceMeters = ValidateNonNegative(value, nameof(AtGradeToleranceMeters));
    }

    public double BridgeDepthThresholdMeters
    {
        get => _bridgeDepthThresholdMeters;
        set => _bridgeDepthThresholdMeters = ValidatePositive(value, nameof(BridgeDepthThresholdMeters));
    }

    public double TunnelDepthThresholdMeters
    {
        get => _tunnelDepthThresholdMeters;
        set => _tunnelDepthThresholdMeters = ValidatePositive(value, nameof(TunnelDepthThresholdMeters));
    }

    public double MinimumBridgeRunMeters
    {
        get => _minimumBridgeRunMeters;
        set => _minimumBridgeRunMeters = ValidateNonNegative(value, nameof(MinimumBridgeRunMeters));
    }

    public double MinimumTunnelRunMeters
    {
        get => _minimumTunnelRunMeters;
        set => _minimumTunnelRunMeters = ValidateNonNegative(value, nameof(MinimumTunnelRunMeters));
    }

    public double SurfaceCostPerMeter
    {
        get => _surfaceCostPerMeter;
        set => _surfaceCostPerMeter = ValidateNonNegative(value, nameof(SurfaceCostPerMeter));
    }

    public double EarthworkCostPerCubicMeter
    {
        get => _earthworkCostPerCubicMeter;
        set => _earthworkCostPerCubicMeter = ValidateNonNegative(value, nameof(EarthworkCostPerCubicMeter));
    }

    public double BridgeCostPerMeter
    {
        get => _bridgeCostPerMeter;
        set => _bridgeCostPerMeter = ValidateNonNegative(value, nameof(BridgeCostPerMeter));
    }

    public double TunnelCostPerMeter
    {
        get => _tunnelCostPerMeter;
        set => _tunnelCostPerMeter = ValidateNonNegative(value, nameof(TunnelCostPerMeter));
    }

    private static double ValidatePositive(double value, string parameterName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        return value;
    }

    private static double ValidateNonNegative(double value, string parameterName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        return value;
    }
}
