using System;
using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Fitting;

public sealed class EngineeringDiagnostic
{
    public EngineeringDiagnostic(
        string code,
        EngineeringDiagnosticSeverity severity,
        string message,
        PlanarPoint? position = null,
        double? stationMeters = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("A diagnostic code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("A diagnostic message is required.", nameof(message));
        }

        if (stationMeters.HasValue &&
            (double.IsNaN(stationMeters.Value) || double.IsInfinity(stationMeters.Value)))
        {
            throw new ArgumentOutOfRangeException(nameof(stationMeters));
        }

        Code = code;
        Severity = severity;
        Message = message;
        Position = position;
        StationMeters = stationMeters;
    }

    public string Code { get; }

    public EngineeringDiagnosticSeverity Severity { get; }

    public string Message { get; }

    /// <summary>
    /// Optional plan location for preview markers and corridor re-search feedback.
    /// </summary>
    public PlanarPoint? Position { get; }

    /// <summary>
    /// Optional alignment station for profile diagnostics.
    /// </summary>
    public double? StationMeters { get; }
}
