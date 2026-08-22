using System;
using System.Collections.Generic;
using System.Linq;

namespace RealRoadBuilder.Core.Planning;

public sealed class HighwayPlanningResult
{
    public HighwayPlanningResult(IEnumerable<HighwayPlanningAttempt> attempts)
    {
        if (attempts == null)
        {
            throw new ArgumentNullException(nameof(attempts));
        }

        List<HighwayPlanningAttempt> materializedAttempts = attempts.ToList();
        if (materializedAttempts.Count == 0)
        {
            throw new ArgumentException("Planning must produce at least one attempt.", nameof(attempts));
        }

        Attempts = materializedAttempts.AsReadOnly();
        BestAttempt = materializedAttempts
            .Where(attempt => attempt.IsSuccessful)
            .OrderBy(attempt => attempt.ComparisonScore)
            .FirstOrDefault();
    }

    public IReadOnlyList<HighwayPlanningAttempt> Attempts { get; }

    public HighwayPlanningAttempt? BestAttempt { get; }

    public bool IsSuccessful => BestAttempt != null;
}
