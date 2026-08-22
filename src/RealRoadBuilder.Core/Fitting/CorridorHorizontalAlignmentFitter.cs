using System;
using System.Collections.Generic;
using System.Linq;
using RealRoadBuilder.Core.Alignment;
using RealRoadBuilder.Core.Design;
using RealRoadBuilder.Core.Geometry;
using RealRoadBuilder.Core.Terrain;
using RealRoadBuilder.Core.Validation;

namespace RealRoadBuilder.Core.Fitting;

public static class CorridorHorizontalAlignmentFitter
{
    private const double GeometryToleranceMeters = 1e-5;
    private const double CollinearCrossTolerance = 1e-8;

    public static HorizontalAlignmentFitResult Fit(
        CorridorRoute route,
        RoadDesignRule designRule,
        HorizontalFitOptions? options = null)
    {
        if (route == null)
        {
            throw new ArgumentNullException(nameof(route));
        }

        if (designRule == null)
        {
            throw new ArgumentNullException(nameof(designRule));
        }

        options ??= new HorizontalFitOptions();

        List<EngineeringDiagnostic> diagnostics = new();
        IReadOnlyList<PlanarPoint> reducedPoints = CorridorControlPointReducer.Reduce(
            route,
            options.ControlPointToleranceMeters);
        IReadOnlyList<PlanarPoint> controlPoints = RemoveCollinearPoints(reducedPoints);

        if (controlPoints.Count == 2)
        {
            HorizontalAlignment straightAlignment = new(
                new[] { new TangentElement(controlPoints[0], controlPoints[1]) });

            AppendValidationDiagnostics(
                straightAlignment,
                designRule,
                options.AllowExceptionalCurveRadius,
                diagnostics);

            return new HorizontalAlignmentFitResult(
                straightAlignment,
                controlPoints,
                Array.Empty<SpiralCornerGeometry>(),
                diagnostics);
        }

        double requiredMinimumRadiusMeters = options.AllowExceptionalCurveRadius
            ? designRule.ExceptionalMinimumCurveRadiusMeters
            : designRule.MinimumCurveRadiusMeters;
        double transitionLengthMeters = designRule.MinimumTransitionLengthMeters;

        List<SpiralCornerGeometry> corners = new();

        for (int index = 1; index < controlPoints.Count - 1; index++)
        {
            PlanarPoint previousPoint = controlPoints[index - 1];
            PlanarPoint intersection = controlPoints[index];
            PlanarPoint nextPoint = controlPoints[index + 1];

            PlanarVector incomingDirection = (intersection - previousPoint).Normalized();
            PlanarVector outgoingDirection = (nextPoint - intersection).Normalized();
            double deflectionAngleRadians = PlanarVector.AngleBetween(
                incomingDirection,
                outgoingDirection);

            if (deflectionAngleRadians <= 1e-9)
            {
                continue;
            }

            double transitionGeometryMinimumRadiusMeters =
                (transitionLengthMeters / deflectionAngleRadians) * 1.001;
            double selectedRadiusMeters = Math.Max(
                requiredMinimumRadiusMeters,
                transitionGeometryMinimumRadiusMeters);

            try
            {
                SpiralCornerGeometry corner = SpiralCornerBuilder.Build(
                    previousPoint,
                    intersection,
                    nextPoint,
                    selectedRadiusMeters,
                    transitionLengthMeters);
                corners.Add(corner);

                if (selectedRadiusMeters > requiredMinimumRadiusMeters + 1e-6)
                {
                    diagnostics.Add(new EngineeringDiagnostic(
                        code: "HORIZONTAL_RADIUS_INCREASED_FOR_TRANSITION",
                        severity: EngineeringDiagnosticSeverity.Info,
                        message:
                            $"PI {index} uses radius {selectedRadiusMeters:0.###} m instead of the " +
                            $"minimum {requiredMinimumRadiusMeters:0.###} m because the " +
                            $"{transitionLengthMeters:0.###} m transition requirement would otherwise consume the bend."));
                }
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is InvalidOperationException)
            {
                diagnostics.Add(new EngineeringDiagnostic(
                    code: "HORIZONTAL_CORNER_FIT_FAILED",
                    severity: EngineeringDiagnosticSeverity.Error,
                    message: $"PI {index} cannot fit a compliant spiral-arc-spiral curve: {exception.Message}"));
            }
        }

        if (diagnostics.Any(diagnostic => diagnostic.Severity == EngineeringDiagnosticSeverity.Error))
        {
            return new HorizontalAlignmentFitResult(
                alignment: null,
                controlPoints,
                corners,
                diagnostics);
        }

        for (int index = 0; index < corners.Count - 1; index++)
        {
            SpiralCornerGeometry currentCorner = corners[index];
            SpiralCornerGeometry nextCorner = corners[index + 1];
            double tangentBetweenPisMeters =
                currentCorner.Intersection.DistanceTo(nextCorner.Intersection);
            double requiredTrimMeters =
                currentCorner.TangentLengthMeters + nextCorner.TangentLengthMeters;

            if (requiredTrimMeters > tangentBetweenPisMeters + GeometryToleranceMeters)
            {
                diagnostics.Add(new EngineeringDiagnostic(
                    code: "HORIZONTAL_ADJACENT_CURVES_OVERLAP",
                    severity: EngineeringDiagnosticSeverity.Error,
                    message:
                        $"Adjacent curves around PIs {index + 1} and {index + 2} require " +
                        $"{requiredTrimMeters:0.###} m of tangent but only " +
                        $"{tangentBetweenPisMeters:0.###} m is available. The corridor must be widened, " +
                        "the PIs moved farther apart, or the route re-searched."));
            }
        }

        if (diagnostics.Any(diagnostic => diagnostic.Severity == EngineeringDiagnosticSeverity.Error))
        {
            return new HorizontalAlignmentFitResult(
                alignment: null,
                controlPoints,
                corners,
                diagnostics);
        }

        List<HorizontalAlignmentElement> elements = new();
        PlanarPoint currentPoint = controlPoints[0];

        foreach (SpiralCornerGeometry corner in corners)
        {
            double tangentLengthMeters = currentPoint.DistanceTo(corner.TransitionStart);
            if (tangentLengthMeters > GeometryToleranceMeters)
            {
                elements.Add(new TangentElement(currentPoint, corner.TransitionStart));
            }

            elements.Add(corner.EntrySpiral);
            elements.Add(corner.CircularArc);
            elements.Add(corner.ExitSpiral);
            currentPoint = corner.TransitionEnd;
        }

        PlanarPoint finalPoint = controlPoints[controlPoints.Count - 1];
        if (currentPoint.DistanceTo(finalPoint) > GeometryToleranceMeters)
        {
            elements.Add(new TangentElement(currentPoint, finalPoint));
        }

        HorizontalAlignment alignment;
        try
        {
            alignment = new HorizontalAlignment(elements);
        }
        catch (ArgumentException exception)
        {
            diagnostics.Add(new EngineeringDiagnostic(
                code: "HORIZONTAL_ALIGNMENT_CONTINUITY_FAILED",
                severity: EngineeringDiagnosticSeverity.Error,
                message: exception.Message));

            return new HorizontalAlignmentFitResult(
                alignment: null,
                controlPoints,
                corners,
                diagnostics);
        }

        AppendValidationDiagnostics(
            alignment,
            designRule,
            options.AllowExceptionalCurveRadius,
            diagnostics);

        return new HorizontalAlignmentFitResult(
            alignment,
            controlPoints,
            corners,
            diagnostics);
    }

    private static IReadOnlyList<PlanarPoint> RemoveCollinearPoints(
        IReadOnlyList<PlanarPoint> points)
    {
        if (points.Count <= 2)
        {
            return points;
        }

        List<PlanarPoint> cleaned = new() { points[0] };

        for (int index = 1; index < points.Count - 1; index++)
        {
            PlanarVector incoming = points[index] - cleaned[cleaned.Count - 1];
            PlanarVector outgoing = points[index + 1] - points[index];

            if (incoming.Length <= GeometryToleranceMeters ||
                outgoing.Length <= GeometryToleranceMeters)
            {
                continue;
            }

            double normalizedCross = Math.Abs(
                PlanarVector.Cross(incoming.Normalized(), outgoing.Normalized()));

            if (normalizedCross > CollinearCrossTolerance)
            {
                cleaned.Add(points[index]);
            }
        }

        cleaned.Add(points[points.Count - 1]);
        return cleaned.AsReadOnly();
    }

    private static void AppendValidationDiagnostics(
        HorizontalAlignment alignment,
        RoadDesignRule designRule,
        bool allowExceptionalCurveRadius,
        ICollection<EngineeringDiagnostic> diagnostics)
    {
        AlignmentValidationResult validation = HorizontalAlignmentValidator.Validate(
            alignment,
            designRule,
            allowExceptionalCurveRadius);

        foreach (string error in validation.Errors)
        {
            diagnostics.Add(new EngineeringDiagnostic(
                code: "HORIZONTAL_VALIDATION_ERROR",
                severity: EngineeringDiagnosticSeverity.Error,
                message: error));
        }

        foreach (string warning in validation.Warnings)
        {
            diagnostics.Add(new EngineeringDiagnostic(
                code: "HORIZONTAL_VALIDATION_WARNING",
                severity: EngineeringDiagnosticSeverity.Warning,
                message: warning));
        }
    }
}
