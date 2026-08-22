using System;

namespace RealRoadBuilder.Core.Fitting;

public sealed class EngineeringDiagnostic
{
    public EngineeringDiagnostic(
        string code,
        EngineeringDiagnosticSeverity severity,
        string message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("A diagnostic code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("A diagnostic message is required.", nameof(message));
        }

        Code = code;
        Severity = severity;
        Message = message;
    }

    public string Code { get; }

    public EngineeringDiagnosticSeverity Severity { get; }

    public string Message { get; }
}
