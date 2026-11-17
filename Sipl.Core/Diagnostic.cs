namespace Sipl.Core.Models;

public enum DiagnosticSeverity
{
    Error = 1,
    Warning = 2,
    Information = 3
}

public record Diagnostic(
    string Message,
    SourceSpan Span,
    DiagnosticSeverity Severity = DiagnosticSeverity.Error
);