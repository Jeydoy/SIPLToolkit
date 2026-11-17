namespace Sipl.LanguageServer.Handlers;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Sipl.Parser;

public class SiplCompletionHandler : CompletionHandlerBase
{
    private readonly DocumentStore _store;
    private static readonly string[] Keywords =
        ["begin", "end", "while", "do", "if", "then", "else", "skip", "div", "mod", "not", "or"];

    public SiplCompletionHandler(DocumentStore store) => _store = store;

    public override Task<CompletionList> Handle(CompletionParams request, CancellationToken ct)
    {
        var items = Keywords.Select(kw => new CompletionItem
        {
            Label = kw,
            Kind = CompletionItemKind.Keyword,
            Detail = "SIPL keyword"
        }).ToList();

        if (_store.TryGet(request.TextDocument.Uri, out var entry))
        {
            var variables = entry.Analysis.Tokens
                .Where(t => t.Type == TokenType.Identifier)
                .Select(t => t.Value)
                .Distinct();

            items.AddRange(variables.Select(v => new CompletionItem
            {
                Label = v,
                Kind = CompletionItemKind.Variable,
                Detail = "SIPL variable (int)"
            }));
        }

        return Task.FromResult(new CompletionList(items));
    }

    public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken ct)
        => Task.FromResult(request);

    protected override CompletionRegistrationOptions CreateRegistrationOptions(
        CompletionCapability capability, ClientCapabilities clientCapabilities)
        => new() { DocumentSelector = TextDocumentSelector.ForLanguage("sipl") };
}

public class SiplHoverHandler : HoverHandlerBase
{
    private readonly DocumentStore _store;
    public SiplHoverHandler(DocumentStore store) => _store = store;

    public override Task<Hover?> Handle(HoverParams request, CancellationToken ct)
    {
        if (!_store.TryGet(request.TextDocument.Uri, out var entry))
            return Task.FromResult<Hover?>(null);

        int targetLine = request.Position.Line + 1;
        int targetCol = request.Position.Character + 1;

        var token = entry.Analysis.Tokens.FirstOrDefault(t =>
            t.Span.Line == targetLine &&
            targetCol >= t.Span.Column &&
            targetCol <= t.Span.Column + Math.Max(1, t.Span.Length));

        if (token is null) return Task.FromResult<Hover?>(null);

        string info = token.Type switch
        {
            TokenType.Identifier => $"**Змінна** `{token.Value}` : `int`",
            TokenType.Number => $"**Константа** `{token.Value}` : `int`",
            _ => $"**Токен SIPL**: `{token.Value}` ({token.Type})"
        };

        return Task.FromResult<Hover?>(new Hover
        {
            Contents = new MarkedStringsOrMarkupContent(new MarkupContent
            {
                Kind = MarkupKind.Markdown,
                Value = info
            })
        });
    }

    protected override HoverRegistrationOptions CreateRegistrationOptions(
        HoverCapability capability, ClientCapabilities clientCapabilities)
        => new() { DocumentSelector = TextDocumentSelector.ForLanguage("sipl") };
}

public class SiplDocumentSymbolHandler : DocumentSymbolHandlerBase
{
    private readonly DocumentStore _store;
    public SiplDocumentSymbolHandler(DocumentStore store) => _store = store;

    public override Task<SymbolInformationOrDocumentSymbolContainer?> Handle(
        DocumentSymbolParams request, CancellationToken ct)
    {
        if (!_store.TryGet(request.TextDocument.Uri, out var entry))
            return Task.FromResult<SymbolInformationOrDocumentSymbolContainer?>(null);

        var symbols = entry.Analysis.Tokens
            .Where(t => t.Type == TokenType.Identifier)
            .GroupBy(t => t.Value)
            .Select(g =>
            {
                var first = g.First();
                int line = Math.Max(0, first.Span.Line - 1);
                int col = Math.Max(0, first.Span.Column - 1);
                var range = new Range(new Position(line, col), new Position(line, col + first.Span.Length));

                return new SymbolInformationOrDocumentSymbol(new DocumentSymbol
                {
                    Name = g.Key,
                    Detail = "int",
                    Kind = SymbolKind.Variable,
                    Range = range,
                    SelectionRange = range
                });
            }).ToList();

        return Task.FromResult<SymbolInformationOrDocumentSymbolContainer?>(
            new SymbolInformationOrDocumentSymbolContainer(symbols));
    }

    protected override DocumentSymbolRegistrationOptions CreateRegistrationOptions(
        DocumentSymbolCapability capability, ClientCapabilities clientCapabilities)
        => new() { DocumentSelector = TextDocumentSelector.ForLanguage("sipl") };
}

public class SiplSemanticTokensHandler : SemanticTokensHandlerBase
{
    private readonly DocumentStore _store;
    public SiplSemanticTokensHandler(DocumentStore store) => _store = store;

    protected override Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier, CancellationToken ct)
    {
        if (!_store.TryGet(identifier.TextDocument.Uri, out var entry))
            return Task.CompletedTask;

        foreach (var token in entry.Analysis.Tokens)
        {
            SemanticTokenType? tokenType = token.Type switch
            {
                TokenType.Begin or TokenType.End or TokenType.While or TokenType.Do
                    or TokenType.If or TokenType.Then or TokenType.Else or TokenType.Skip
                    or TokenType.Not or TokenType.Or or TokenType.Div or TokenType.Mod => SemanticTokenType.Keyword,
                TokenType.Identifier => SemanticTokenType.Variable,
                TokenType.Number => SemanticTokenType.Number,
                TokenType.Assign or TokenType.Equals or TokenType.Greater or TokenType.GreaterOrEqual
                    or TokenType.Plus or TokenType.Minus or TokenType.Multiply => SemanticTokenType.Operator,
                _ => null
            };

            if (tokenType is not null && token.Span.Line > 0 && token.Span.Column > 0)
            {
                builder.Push(
                    token.Span.Line - 1,
                    token.Span.Column - 1,
                    token.Span.Length,
                    tokenType,
                    Array.Empty<SemanticTokenModifier>()
                );
            }
        }

        return Task.CompletedTask;
    }

    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(
        ITextDocumentIdentifierParams @params, CancellationToken ct)
        => Task.FromResult(new SemanticTokensDocument(RegistrationOptions.Legend));

    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(
        SemanticTokensCapability capability, ClientCapabilities clientCapabilities)
        => new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("sipl"),
            Full = new SemanticTokensCapabilityRequestFull { Delta = false },
            Legend = new SemanticTokensLegend
            {
                TokenTypes = new Container<SemanticTokenType>(
                    SemanticTokenType.Keyword,
                    SemanticTokenType.Variable,
                    SemanticTokenType.Number,
                    SemanticTokenType.Operator
                ),
                TokenModifiers = new Container<SemanticTokenModifier>()
            }
        };
}