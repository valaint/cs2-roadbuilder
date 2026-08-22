using System;

namespace RealRoadBuilder.Core.Fitting;

public sealed class EngineeringAlignmentFitOptions
{
    public EngineeringAlignmentFitOptions()
    {
        Horizontal = new HorizontalFitOptions();
        Vertical = new VerticalFitOptions();
    }

    public HorizontalFitOptions Horizontal { get; }

    public VerticalFitOptions Vertical { get; }

    public void UseExceptionalGeometry(bool enabled)
    {
        Horizontal.AllowExceptionalCurveRadius = enabled;
        Vertical.AllowExceptionalGrade = enabled;
    }
}
