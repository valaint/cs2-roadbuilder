using System;
using RealRoadBuilder.Core.Fitting;
using RealRoadBuilder.Core.Terrain;

namespace RealRoadBuilder.Core.Evaluation;

public static class FinalAlignmentConstructionEvaluator
{
    public static ConstructionEvaluationResult Evaluate(
        EngineeringAlignmentFitResult fitResult,
        ITerrainSampler terrainSampler,
        ConstructionEvaluationOptions? options = null,
        IStructureRequirementProvider? requirementProvider = null)
    {
        if (fitResult == null)
        {
            throw new ArgumentNullException(nameof(fitResult));
        }

        if (!fitResult.IsSuccessful ||
            fitResult.Horizontal.Alignment == null ||
            fitResult.Vertical?.Alignment == null)
        {
            throw new InvalidOperationException(
                "Construction evaluation requires a successful horizontal and vertical engineering fit.");
        }

        return ConstructionEvaluator.Evaluate(
            fitResult.Horizontal.Alignment,
            fitResult.Vertical.Alignment,
            terrainSampler,
            options,
            requirementProvider);
    }
}
