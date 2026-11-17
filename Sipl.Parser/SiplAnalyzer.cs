namespace Sipl.Parser;

using Sipl.Core.Ast;
using Sipl.Core.Models;

public record AnalysisResult(
    ProgramNode? Ast,
    IReadOnlyList<Token> Tokens,
    IReadOnlyList<Diagnostic> Diagnostics
)
{
    public bool IsSuccess => Diagnostics.Count == 0 && Ast is not null;
}

public static class SiplAnalyzer
{
    public static AnalysisResult Analyze(string source)
    {
        var diagnostics = new List<Diagnostic>();
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();

        foreach (var token in tokens)
        {
            if (token.Type == TokenType.Error)
            {
                diagnostics.Add(new Diagnostic(
                    $"Невідомий символ '{token.Value}'",
                    token.Span
                ));
            }
        }

        if (diagnostics.Count > 0)
            return new AnalysisResult(null, tokens, diagnostics);

        try
        {
            var parser = new Parser(tokens);
            var ast = parser.Parse();
            return new AnalysisResult(ast, tokens, diagnostics);
        }
        catch (Exception ex)
        {
            // Беремо позицію поточного токена або кінця файлу, якщо виняток не містить Span
            var fallbackSpan = tokens.Count > 0 ? tokens[^1].Span : new SourceSpan(1, 1, 1);
            diagnostics.Add(new Diagnostic(ex.Message, fallbackSpan));
            return new AnalysisResult(null, tokens, diagnostics);
        }
    }
}