using System;
using RealRoadBuilder.Core.Evaluation;
using RealRoadBuilder.Core.Fitting;
using RealRoadBuilder.Core.Terrain;

namespace RealRoadBuilder.Core.Planning;

public sealed class HighwayPlanningAttempt
{
    public HighwayPlanningAttempt(
        int iterationNumber,
        CorridorRoute route,
        CorridorVerticalProfile? preliminaryVerticalProfile,
        EngineeringAlignmentFitResult? fitResult,
        ConstructionEvaluationResult? constructionEvaluation,
        double comparisonScore,
        string? failureReason = null)
    {
        if (iterationNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(iterationNumber));
        }

        if (double.IsNaN(comparisonScore) || comparisonScore < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(comparisonScore));
        }

        IterationNumber = iterationNumber;
        Route = route ?? throw new ArgumentNullException(nameof(route));
        PreliminaryVerticalProfile = preliminaryVerticalProfile;
        FitResult = fitResult;
        ConstructionEvaluation = constructionEvaluation;
        ComparisonScore = comparisonScore;
        FailureReason = failureReason;
    }

    public int IterationNumber { get; }

    public CorridorRoute Route { get; }

    public CorridorVerticalProfile? PreliminaryVerticalProfile { get; }

    public EngineeringAlignmentFitResult? FitResult { get; }

    public ConstructionEvaluationResult? ConstructionEvaluation { get; }

    public double ComparisonScore { get; }

    public string? FailureReason { get; }

    public bool IsSuccessful =>
        FitResult?.IsSuccessful == true &&
        ConstructionEvaluation != null &&
        string.IsNullOrEmpty(FailureReason);
}
