using System;
using System.Collections.Generic;
using RealRoadBuilder.Core.Alignment;
using RealRoadBuilder.Core.Design;

namespace RealRoadBuilder.Core.Validation;

public static class HorizontalAlignmentValidator
{
    private const double CurvatureTolerance = 1e-8;

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

        int circularCurveNumber = 0;
        int transitionNumber = 0;

        for (int index = 0; index < alignment.Elements.Count; index++)
        {
            HorizontalAlignmentElement element = alignment.Elements[index];

            if (element is CircularArcElement curve)
            {
                circularCurveNumber++;

                if (curve.RadiusMeters + 1e-6 < requiredRadiusMeters)
                {
                    errors.Add(
                        $"Curve {circularCurveNumber} radius {curve.RadiusMeters:0.###} m is below the required {requiredRadiusMeters:0.###} m for {designRule.DesignSpeedKph} km/h.");
                }

                bool hasEntryTransition = index > 0 &&
                    alignment.Elements[index - 1] is TransitionSpiralElement entryTransition &&
                    CurvaturesConnect(entryTransition.EndCurvaturePerMeter, curve);

                bool hasExitTransition = index + 1 < alignment.Elements.Count &&
                    alignment.Elements[index + 1] is TransitionSpiralElement exitTransition &&
                    CurvaturesConnect(exitTransition.StartCurvaturePerMeter, curve);

                if (!hasEntryTransition || !hasExitTransition)
                {
                    warnings.Add(
                        $"Curve {circularCurveNumber} does not yet have matching transition spirals on both sides. " +
                        $"The selected rule requires at least {designRule.MinimumTransitionLengthMeters:0.###} m of transition section at {designRule.DesignSpeedKph} km/h.");
                }
            }
            else if (element is TransitionSpiralElement transition)
            {
                transitionNumber++;

                if (transition.LengthMeters + 1e-6 < designRule.MinimumTransitionLengthMeters)
                {
                    errors.Add(
                        $"Transition {transitionNumber} length {transition.LengthMeters:0.###} m is below the required {designRule.MinimumTransitionLengthMeters:0.###} m for {designRule.DesignSpeedKph} km/h.");
                }

                if (transition.MinimumRadiusMeters + 1e-6 < requiredRadiusMeters)
                {
                    errors.Add(
                        $"Transition {transitionNumber} reaches an equivalent radius of {transition.MinimumRadiusMeters:0.###} m, below the required {requiredRadiusMeters:0.###} m.");
                }
            }
        }

        return new AlignmentValidationResult(errors.AsReadOnly(), warnings.AsReadOnly());
    }

    private static bool CurvaturesConnect(double transitionCurvaturePerMeter, CircularArcElement curve)
    {
        double arcCurvatureMagnitude = 1.0 / curve.RadiusMeters;
        return Math.Abs(Math.Abs(transitionCurvaturePerMeter) - arcCurvatureMagnitude) <= CurvatureTolerance;
    }
}
