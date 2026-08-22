using System;
using System.Collections.Generic;
using System.Linq;
using RealRoadBuilder.Core.Alignment;
using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Fitting;

public sealed class HorizontalAlignmentFitResult
{
    public HorizontalAlignmentFitResult(
        HorizontalAlignment? alignment,
        IEnumerable<PlanarPoint> controlPoints,
        IEnumerable<SpiralCornerGeometry> corners,
        IEnumerable<EngineeringDiagnostic> diagnostics)
    {
        ControlPoints = (controlPoints ?? throw new ArgumentNullException(nameof(controlPoints)))
            .ToList()
            .AsReadOnly();
        Corners = (corners ?? throw new ArgumentNullException(nameof(corners)))
            .ToList()
            .AsReadOnly();
        Diagnostics = (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics)))
            .ToList()
            .AsReadOnly();
        Alignment = alignment;
    }

    public HorizontalAlignment? Alignment { get; }

    public IReadOnlyList<PlanarPoint> ControlPoints { get; }

    public IReadOnlyList<SpiralCornerGeometry> Corners { get; }

    public IReadOnlyList<EngineeringDiagnostic> Diagnostics { get; }

    public bool IsSuccessful =>
        Alignment != null &&
        !Diagnostics.Any(diagnostic => diagnostic.Severity == EngineeringDiagnosticSeverity.Error);
}
