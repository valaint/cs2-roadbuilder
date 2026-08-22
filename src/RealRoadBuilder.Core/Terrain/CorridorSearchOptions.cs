using System;

namespace RealRoadBuilder.Core.Terrain;

public sealed class CorridorSearchOptions
{
    public CorridorSearchOptions(
        double cellSizeMeters = 40.0,
        double searchMarginMeters = 1200.0,
        int maximumExpandedStates = 250000,
        double gradePenaltyWeight = 8.0,
        double turnPenaltyWeight = 20.0,
        double additionalCostWeight = 1.0)
    {
        if (cellSizeMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSizeMeters));
        }

        if (searchMarginMeters < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(searchMarginMeters));
        }

        if (maximumExpandedStates <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumExpandedStates));
        }

        if (gradePenaltyWeight < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(gradePenaltyWeight));
        }

        if (turnPenaltyWeight < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(turnPenaltyWeight));
        }

        if (additionalCostWeight < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(additionalCostWeight));
        }

        CellSizeMeters = cellSizeMeters;
        SearchMarginMeters = searchMarginMeters;
        MaximumExpandedStates = maximumExpandedStates;
        GradePenaltyWeight = gradePenaltyWeight;
        TurnPenaltyWeight = turnPenaltyWeight;
        AdditionalCostWeight = additionalCostWeight;
    }

    public double CellSizeMeters { get; }

    public double SearchMarginMeters { get; }

    public int MaximumExpandedStates { get; }

    public double GradePenaltyWeight { get; }

    public double TurnPenaltyWeight { get; }

    public double AdditionalCostWeight { get; }
}
