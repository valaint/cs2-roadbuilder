using System;

namespace RealRoadBuilder.Core.Fitting;

public sealed class HorizontalFitOptions
{
    private double _controlPointToleranceMeters = 20.0;

    public double ControlPointToleranceMeters
    {
        get => _controlPointToleranceMeters;
        set
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _controlPointToleranceMeters = value;
        }
    }

    public bool AllowExceptionalCurveRadius { get; set; }
}
