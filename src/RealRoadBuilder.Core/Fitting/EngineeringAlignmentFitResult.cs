using System;
using System.Collections.Generic;
using System.Linq;

namespace RealRoadBuilder.Core.Fitting;

public sealed class EngineeringAlignmentFitResult
{
    public EngineeringAlignmentFitResult(
        HorizontalAlignmentFitResult horizontal,
        VerticalAlignmentFitResult? vertical,
        IEnumerable<EngineeringDiagnostic> diagnostics)
    {
        Horizontal = horizontal ?? throw new ArgumentNullException(nameof(horizontal));
        Vertical = vertical;
        Diagnostics = (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics)))
            .ToList()
            .AsReadOnly();
    }

    public HorizontalAlignmentFitResult Horizontal { get; }

    public VerticalAlignmentFitResult? Vertical { get; }

    public IReadOnlyList<EngineeringDiagnostic> Diagnostics { get; }

    public bool IsSuccessful =>
        Horizontal.IsSuccessful &&
        Vertical?.IsSuccessful == true &&
        !Diagnostics.Any(diagnostic => diagnostic.Severity == EngineeringDiagnosticSeverity.Error);
}
