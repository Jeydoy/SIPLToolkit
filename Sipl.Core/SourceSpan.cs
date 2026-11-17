namespace Sipl.Core.Models;

public record SourceSpan(int Line, int Column, int Length)
{
    public static SourceSpan Empty => new(0, 0, 0);
}