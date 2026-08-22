using System;
using RealRoadBuilder.Core.Alignment;
using RealRoadBuilder.Core.Design;
using RealRoadBuilder.Core.Generation;

namespace RealRoadBuilder.Core.Scoring;

public static class AlignmentScorer
{
    public static AlignmentScore Score(
        HorizontalAlignment alignment,
        RoadDesignRule designRule,
        AlignmentGenerationMode generationMode)
    {
        if (alignment == null)
        {
            throw new ArgumentNullException(nameof(alignment));
        }

        if (designRule == null)
        {
            throw new ArgumentNullException(nameof(designRule));
        }

        double curvatureWeight = generationMode switch
        {
            AlignmentGenerationMode.Shortest => 0.10,
            AlignmentGenerationMode.Balanced => 0.40,
            AlignmentGenerationMode.HighStandard => 1.00,
            _ => throw new ArgumentOutOfRangeException(nameof(generationMode)),
        };

        double curvatureCost = 0.0;
        foreach (CircularArcElement curve in alignment.Curves)
        {
            double radiusUtilization = designRule.MinimumCurveRadiusMeters / curve.RadiusMeters;
            curvatureCost += curve.LengthMeters * radiusUtilization * curvatureWeight;
        }

        double lengthCost = alignment.TotalLengthMeters;
        return new AlignmentScore(
            lengthCost: lengthCost,
            curvatureCost: curvatureCost,
            totalCost: lengthCost + curvatureCost);
    }
}
