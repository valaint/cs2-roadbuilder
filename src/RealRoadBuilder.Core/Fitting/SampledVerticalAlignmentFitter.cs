using System;
using System.Collections.Generic;
using System.Linq;
using RealRoadBuilder.Core.Design;
using RealRoadBuilder.Core.Terrain;
using RealRoadBuilder.Core.Validation;
using RealRoadBuilder.Core.Vertical;

namespace RealRoadBuilder.Core.Fitting;

public static class SampledVerticalAlignmentFitter
{
    private const double GeometryToleranceMeters = 1e-5;

    public static VerticalAlignmentFitResult Fit(
        CorridorVerticalProfile profile,
        double targetLengthMeters,
        RoadDesignRule designRule,
        VerticalFitOptions? options = null)
    {
        if (profile == null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        if (designRule == null)
        {
            throw new ArgumentNullException(nameof(designRule));
        }

        if (double.IsNaN(targetLengthMeters) ||
            double.IsInfinity(targetLengthMeters) ||
            targetLengthMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetLengthMeters));
        }

        options ??= new VerticalFitOptions();
        List<EngineeringDiagnostic> diagnostics = new();

        double sourceStartStationMeters = profile.Samples[0].StationMeters;
        double sourceEndStationMeters = profile.Samples[profile.Samples.Count - 1].StationMeters;
        double sourceLengthMeters = sourceEndStationMeters - sourceStartStationMeters;
        double stationScale = targetLengthMeters / sourceLengthMeters;

        if (Math.Abs(stationScale - 1.0) > 0.005)
        {
            diagnostics.Add(new EngineeringDiagnostic(
                code: "VERTICAL_STATION_RESCALED",
                severity: EngineeringDiagnosticSeverity.Info,
                message:
                    $"The preliminary profile stationing was rescaled by {stationScale:0.####} to match the " +
                    $"final horizontal alignment length of {targetLengthMeters:0.###} m."));
        }

        IReadOnlyList<ProfilePoint> controlPoints = VerticalProfileControlPointReducer.Reduce(
            profile,
            targetLengthMeters,
            options.ElevationToleranceMeters);

        if (controlPoints.Count == 2)
        {
            VerticalAlignment straightProfile = new(
                new[] { new ConstantGradeElement(controlPoints[0], controlPoints[1]) });

            AppendValidationDiagnostics(
                straightProfile,
                designRule,
                options.AllowExceptionalGrade,
                diagnostics);

            return new VerticalAlignmentFitResult(
                straightProfile,
                controlPoints,
                Array.Empty<VerticalCornerGeometry>(),
                diagnostics);
        }

        List<VerticalCornerGeometry> corners = new();

        for (int index = 1; index < controlPoints.Count - 1; index++)
        {
            ProfilePoint previousPoint = controlPoints[index - 1];
            ProfilePoint intersection = controlPoints[index];
            ProfilePoint nextPoint = controlPoints[index + 1];

            double incomingGradeDecimal =
                (intersection.ElevationMeters - previousPoint.ElevationMeters) /
                (intersection.StationMeters - previousPoint.StationMeters);
            double outgoingGradeDecimal =
                (nextPoint.ElevationMeters - intersection.ElevationMeters) /
                (nextPoint.StationMeters - intersection.StationMeters);

            if (Math.Abs(outgoingGradeDecimal - incomingGradeDecimal) <= 1e-10)
            {
                continue;
            }

            try
            {
                corners.Add(VerticalCornerBuilder.Build(
                    previousPoint,
                    intersection,
                    nextPoint,
                    designRule));
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is InvalidOperationException)
            {
                diagnostics.Add(new EngineeringDiagnostic(
                    code: "VERTICAL_CURVE_FIT_FAILED",
                    severity: EngineeringDiagnosticSeverity.Error,
                    message: $"Vertical PVI {index} cannot fit a compliant parabolic curve: {exception.Message}"));
            }
        }

        if (diagnostics.Any(diagnostic => diagnostic.Severity == EngineeringDiagnosticSeverity.Error))
        {
            return new VerticalAlignmentFitResult(
                alignment: null,
                controlPoints,
                corners,
                diagnostics);
        }

        for (int index = 0; index < corners.Count - 1; index++)
        {
            VerticalCornerGeometry currentCorner = corners[index];
            VerticalCornerGeometry nextCorner = corners[index + 1];
            double pviSpacingMeters =
                nextCorner.Intersection.StationMeters - currentCorner.Intersection.StationMeters;
            double requiredHalfLengthsMeters =
                currentCorner.HalfCurveLengthMeters + nextCorner.HalfCurveLengthMeters;

            if (requiredHalfLengthsMeters > pviSpacingMeters + GeometryToleranceMeters)
            {
                diagnostics.Add(new EngineeringDiagnostic(
                    code: "VERTICAL_ADJACENT_CURVES_OVERLAP",
                    severity: EngineeringDiagnosticSeverity.Error,
                    message:
                        $"Adjacent vertical curves around stations " +
                        $"{currentCorner.Intersection.StationMeters:0.###} m and " +
                        $"{nextCorner.Intersection.StationMeters:0.###} m need " +
                        $"{requiredHalfLengthsMeters:0.###} m of shared tangent, but only " +
                        $"{pviSpacingMeters:0.###} m is available."));
            }
        }

        if (diagnostics.Any(diagnostic => diagnostic.Severity == EngineeringDiagnosticSeverity.Error))
        {
            return new VerticalAlignmentFitResult(
                alignment: null,
                controlPoints,
                corners,
                diagnostics);
        }

        List<VerticalAlignmentElement> elements = new();
        ProfilePoint currentPoint = controlPoints[0];

        foreach (VerticalCornerGeometry corner in corners)
        {
            double tangentLengthMeters =
                corner.CurveStart.StationMeters - currentPoint.StationMeters;

            if (tangentLengthMeters > GeometryToleranceMeters)
            {
                elements.Add(new ConstantGradeElement(currentPoint, corner.CurveStart));
            }
            else if (tangentLengthMeters < -GeometryToleranceMeters)
            {
                diagnostics.Add(new EngineeringDiagnostic(
                    code: "VERTICAL_CURVE_ORDERING_FAILED",
                    severity: EngineeringDiagnosticSeverity.Error,
                    message:
                        $"Vertical curve at station {corner.Intersection.StationMeters:0.###} m begins " +
                        "before the previous fitted element ends."));
                break;
            }

            elements.Add(corner.Curve);
            currentPoint = corner.CurveEnd;
        }

        if (diagnostics.Any(diagnostic => diagnostic.Severity == EngineeringDiagnosticSeverity.Error))
        {
            return new VerticalAlignmentFitResult(
                alignment: null,
                controlPoints,
                corners,
                diagnostics);
        }

        ProfilePoint finalPoint = controlPoints[controlPoints.Count - 1];
        double finalTangentLengthMeters = finalPoint.StationMeters - currentPoint.StationMeters;
        if (finalTangentLengthMeters > GeometryToleranceMeters)
        {
            elements.Add(new ConstantGradeElement(currentPoint, finalPoint));
        }
        else if (finalTangentLengthMeters < -GeometryToleranceMeters)
        {
            diagnostics.Add(new EngineeringDiagnostic(
                code: "VERTICAL_CURVE_ORDERING_FAILED",
                severity: EngineeringDiagnosticSeverity.Error,
                message: "The final fitted vertical curve extends beyond the target alignment endpoint."));

            return new VerticalAlignmentFitResult(
                alignment: null,
                controlPoints,
                corners,
                diagnostics);
        }

        VerticalAlignment alignment;
        try
        {
            alignment = new VerticalAlignment(elements);
        }
        catch (ArgumentException exception)
        {
            diagnostics.Add(new EngineeringDiagnostic(
                code: "VERTICAL_ALIGNMENT_CONTINUITY_FAILED",
                severity: EngineeringDiagnosticSeverity.Error,
                message: exception.Message));

            return new VerticalAlignmentFitResult(
                alignment: null,
                controlPoints,
                corners,
                diagnostics);
        }

        AppendValidationDiagnostics(
            alignment,
            designRule,
            options.AllowExceptionalGrade,
            diagnostics);

        return new VerticalAlignmentFitResult(
            alignment,
            controlPoints,
            corners,
            diagnostics);
    }

    private static void AppendValidationDiagnostics(
        VerticalAlignment alignment,
        RoadDesignRule designRule,
        bool allowExceptionalGrade,
        ICollection<EngineeringDiagnostic> diagnostics)
    {
        AlignmentValidationResult validation = VerticalAlignmentValidator.Validate(
            alignment,
            designRule,
            allowExceptionalGrade);

        foreach (string error in validation.Errors)
        {
            diagnostics.Add(new EngineeringDiagnostic(
                code: "VERTICAL_VALIDATION_ERROR",
                severity: EngineeringDiagnosticSeverity.Error,
                message: error));
        }

        foreach (string warning in validation.Warnings)
        {
            diagnostics.Add(new EngineeringDiagnostic(
                code: "VERTICAL_VALIDATION_WARNING",
                severity: EngineeringDiagnosticSeverity.Warning,
                message: warning));
        }
    }
}
