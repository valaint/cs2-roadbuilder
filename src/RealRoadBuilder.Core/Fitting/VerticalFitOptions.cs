using System;

namespace RealRoadBuilder.Core.Fitting;

public sealed class VerticalFitOptions
{
    private double _elevationToleranceMeters = 0.25;

    public double ElevationToleranceMeters
    {
        get => _elevationToleranceMeters;
        set
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _elevationToleranceMeters = value;
        }
    }

    public bool AllowExceptionalGrade { get; set; }
}
