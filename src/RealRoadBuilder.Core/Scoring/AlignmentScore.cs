namespace RealRoadBuilder.Core.Scoring;

public sealed class AlignmentScore
{
    public AlignmentScore(
        double lengthCost,
        double curvatureCost,
        double totalCost)
    {
        LengthCost = lengthCost;
        CurvatureCost = curvatureCost;
        TotalCost = totalCost;
    }

    public double LengthCost { get; }

    public double CurvatureCost { get; }

    public double TotalCost { get; }
}
