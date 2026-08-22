using System.Collections.Generic;

namespace RealRoadBuilder.Core.Validation;

public sealed class AlignmentValidationResult
{
    public AlignmentValidationResult(
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings)
    {
        Errors = errors;
        Warnings = warnings;
    }

    public IReadOnlyList<string> Errors { get; }

    public IReadOnlyList<string> Warnings { get; }

    public bool IsValid => Errors.Count == 0;
}
