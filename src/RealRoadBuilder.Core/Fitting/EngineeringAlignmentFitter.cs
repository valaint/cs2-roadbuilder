using System;
using System.Collections.Generic;
using RealRoadBuilder.Core.Design;
using RealRoadBuilder.Core.Terrain;

namespace RealRoadBuilder.Core.Fitting;

public static class EngineeringAlignmentFitter
{
    public static EngineeringAlignmentFitResult Fit(
        CorridorRoute route,
        CorridorVerticalProfile preliminaryVerticalProfile,
        RoadDesignRule designRule,
        EngineeringAlignmentFitOptions? options = null)
    {
        if (route == null)
        {
            throw new ArgumentNullException(nameof(route));
        }

        if (preliminaryVerticalProfile == null)
        {
            throw new ArgumentNullException(nameof(preliminaryVerticalProfile));
        }

        if (designRule == null)
        {
            throw new ArgumentNullException(nameof(designRule));
        }

        options ??= new EngineeringAlignmentFitOptions();
        List<EngineeringDiagnostic> diagnostics = new();

        HorizontalAlignmentFitResult horizontal = CorridorHorizontalAlignmentFitter.Fit(
            route,
            designRule,
            options.Horizontal);
        diagnostics.AddRange(horizontal.Diagnostics);

        if (!horizontal.IsSuccessful || horizontal.Alignment == null)
        {
            diagnostics.Add(new EngineeringDiagnostic(
                code: "ENGINEERING_FIT_STOPPED_AFTER_HORIZONTAL",
                severity: EngineeringDiagnosticSeverity.Error,
                message:
                    "The horizontal corridor could not be converted into standards-valid engineering geometry, " +
                    "so vertical fitting was not attempted."));

            return new EngineeringAlignmentFitResult(
                horizontal,
                vertical: null,
                diagnostics);
        }

        VerticalAlignmentFitResult vertical = SampledVerticalAlignmentFitter.Fit(
            preliminaryVerticalProfile,
            horizontal.Alignment.TotalLengthMeters,
            designRule,
            options.Vertical);
        diagnostics.AddRange(vertical.Diagnostics);

        if (vertical.Alignment != null)
        {
            double lengthDifferenceMeters = Math.Abs(
                horizontal.Alignment.TotalLengthMeters - vertical.Alignment.TotalLengthMeters);

            if (lengthDifferenceMeters > 1e-4)
            {
                diagnostics.Add(new EngineeringDiagnostic(
                    code: "ENGINEERING_STATION_LENGTH_MISMATCH",
                    severity: EngineeringDiagnosticSeverity.Error,
                    message:
                        $"Horizontal length {horizontal.Alignment.TotalLengthMeters:0.###} m and " +
                        $"vertical station length {vertical.Alignment.TotalLengthMeters:0.###} m differ by " +
                        $"{lengthDifferenceMeters:0.######} m."));
            }
        }

        return new EngineeringAlignmentFitResult(
            horizontal,
            vertical,
            diagnostics);
    }
}
