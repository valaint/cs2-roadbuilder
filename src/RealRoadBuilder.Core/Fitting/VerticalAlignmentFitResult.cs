using System;
using System.Collections.Generic;
using System.Linq;
using RealRoadBuilder.Core.Vertical;

namespace RealRoadBuilder.Core.Fitting;

public sealed class VerticalAlignmentFitResult
{
    public VerticalAlignmentFitResult(
        VerticalAlignment? alignment,
        IEnumerable<ProfilePoint> controlPoints,
        IEnumerable<VerticalCornerGeometry> corners,
        IEnumerable<EngineeringDiagnostic> diagnostics)
    {
        Alignment = alignment;
        ControlPoints = (controlPoints ?? throw new ArgumentNullException(nameof(controlPoints)))
            .ToList()
            .AsReadOnly();
        Corners = (corners ?? throw new ArgumentNullException(nameof(corners)))
            .ToList()
            .AsReadOnly();
        Diagnostics = (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics)))
            .ToList()
            .AsReadOnly();
    }

    public VerticalAlignment? Alignment { get; }

    public IReadOnlyList<ProfilePoint> ControlPoints { get; }

    public IReadOnlyList<VerticalCornerGeometry> Corners { get; }

    public IReadOnlyList<EngineeringDiagnostic> Diagnostics { get; }

    public bool IsSuccessful =>
        Alignment != null &&
        !Diagnostics.Any(diagnostic => diagnostic.Severity == EngineeringDiagnosticSeverity.Error);
}
