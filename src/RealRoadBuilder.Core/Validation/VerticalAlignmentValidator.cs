using System;
using System.Collections.Generic;
using RealRoadBuilder.Core.Design;
using RealRoadBuilder.Core.Vertical;

namespace RealRoadBuilder.Core.Validation;

public static class VerticalAlignmentValidator
{
    private const double GradeContinuityTolerancePercent = 1e-6;

    public static AlignmentValidationResult Validate(
        VerticalAlignment alignment,
        RoadDesignRule designRule,
        bool allowExceptionalGrade = false)
    {
        if (alignment == null)
        {
            throw new ArgumentNullException(nameof(alignment));
        }

        if (designRule == null)
        {
            throw new ArgumentNullException(nameof(designRule));
        }

        List<string> errors = new();
        List<string> warnings = new();

        double maximumAllowedGradePercent = allowExceptionalGrade
            ? designRule.ExceptionalMaximumGradePercent
            : designRule.MaximumGradePercent;

        for (int index = 0; index < alignment.Elements.Count; index++)
        {
            VerticalAlignmentElement element = alignment.Elements[index];

            ValidateGradeLimit(
                element.GradePercentAt(element.StartStationMeters),
                maximumAllowedGradePercent,
                index,
                "start",
                errors);

            ValidateGradeLimit(
                element.GradePercentAt(element.EndStationMeters),
                maximumAllowedGradePercent,
                index,
                "end",
                errors);

            if (element is ParabolicVerticalCurveElement curve)
            {
                if (curve.LengthMeters + 1e-6 < designRule.MinimumVerticalCurveLengthMeters)
                {
                    errors.Add(
                        $"Vertical curve {index + 1} length {curve.LengthMeters:0.###} m is below the required {designRule.MinimumVerticalCurveLengthMeters:0.###} m for {designRule.DesignSpeedKph} km/h.");
                }

                double requiredRadiusMeters = curve.CurveType == VerticalCurveType.Crest
                    ? designRule.MinimumCrestVerticalCurveRadiusMeters
                    : designRule.MinimumSagVerticalCurveRadiusMeters;

                if (curve.MinimumRadiusMeters + 1e-6 < requiredRadiusMeters)
                {
                    errors.Add(
                        $"{curve.CurveType} vertical curve {index + 1} minimum radius {curve.MinimumRadiusMeters:0.###} m is below the required {requiredRadiusMeters:0.###} m for {designRule.DesignSpeedKph} km/h.");
                }
            }

            if (index == 0)
            {
                continue;
            }

            VerticalAlignmentElement previous = alignment.Elements[index - 1];
            double previousEndGradePercent = previous.GradePercentAt(previous.EndStationMeters);
            double currentStartGradePercent = element.GradePercentAt(element.StartStationMeters);

            if (Math.Abs(previousEndGradePercent - currentStartGradePercent) > GradeContinuityTolerancePercent)
            {
                errors.Add(
                    $"Grade discontinuity at station {element.StartStationMeters:0.###} m: " +
                    $"{previousEndGradePercent:0.###}% changes immediately to {currentStartGradePercent:0.###}%. " +
                    "A vertical curve must provide a continuous grade transition.");
            }
        }

        return new AlignmentValidationResult(errors.AsReadOnly(), warnings.AsReadOnly());
    }

    private static void ValidateGradeLimit(
        double gradePercent,
        double maximumAllowedGradePercent,
        int elementIndex,
        string endpointName,
        ICollection<string> errors)
    {
        if (Math.Abs(gradePercent) <= maximumAllowedGradePercent + 1e-6)
        {
            return;
        }

        errors.Add(
            $"Vertical element {elementIndex + 1} {endpointName} grade {gradePercent:0.###}% exceeds the allowed ±{maximumAllowedGradePercent:0.###}%.");
    }
}
