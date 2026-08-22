using System;
using System.Collections.Generic;
using System.Linq;
using RealRoadBuilder.Core.Alignment;
using RealRoadBuilder.Core.Design;

namespace RealRoadBuilder.Core.Validation;

public static class HorizontalAlignmentValidator
{
    public static AlignmentValidationResult Validate(
        HorizontalAlignment alignment,
        RoadDesignRule designRule,
        bool allowExceptionalCurveRadius = false)
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

        double requiredRadiusMeters = allowExceptionalCurveRadius
            ? designRule.ExceptionalMinimumCurveRadiusMeters
            : designRule.MinimumCurveRadiusMeters;

        List<CircularArcElement> curves = alignment.Curves.ToList();
        for (int index = 0; index < curves.Count; index++)
        {
            CircularArcElement curve = curves[index];
            if (curve.RadiusMeters + 1e-6 < requiredRadiusMeters)
            {
                errors.Add(
                    $"Curve {index + 1} radius {curve.RadiusMeters:0.###} m is below the required {requiredRadiusMeters:0.###} m for {designRule.DesignSpeedKph} km/h.");
            }
        }

        if (curves.Count > 0)
        {
            warnings.Add(
                $"Transition curves are not modeled yet. The selected rule requires at least {designRule.MinimumTransitionLengthMeters:0.###} m of transition section at {designRule.DesignSpeedKph} km/h.");
        }

        warnings.Add(
            $"Vertical profile validation is not implemented yet; the normal grade limit for this rule is {designRule.MaximumGradePercent:0.###}%.");

        return new AlignmentValidationResult(errors.AsReadOnly(), warnings.AsReadOnly());
    }
}
