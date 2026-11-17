namespace Sipl.LanguageServer.Handlers;

using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using Diagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;
using DiagnosticSeverity = OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity;

public class TextDocumentSyncHandler : TextDocumentSyncHandlerBase
{
    private readonly ILanguageServerFacade _server;
    private readonly DocumentStore _store;

    public TextDocumentSyncHandler(ILanguageServerFacade server, DocumentStore store)
    {
        _server = server;
        _store = store;
    }

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
        => new(uri, "sipl");

    public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken ct)
    {
        ValidateAndPublish(request.TextDocument.Uri, request.TextDocument.Text);
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken ct)
    {
        var text = request.ContentChanges.LastOrDefault()?.Text ?? string.Empty;
        ValidateAndPublish(request.TextDocument.Uri, text);
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken ct) => Unit.Task;

    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken ct)
    {
        _store.Remove(request.TextDocument.Uri);
        return Unit.Task;
    }

    private void ValidateAndPublish(DocumentUri uri, string text)
    {
        var result = _store.Update(uri, text);
        var lspDiagnostics = result.Diagnostics.Select(d =>
        {
            int line = Math.Max(0, d.Span.Line - 1);
            int col = Math.Max(0, d.Span.Column - 1);
            int len = Math.Max(1, d.Span.Length);

            return new Diagnostic
            {
                Message = d.Message,
                Severity = DiagnosticSeverity.Error,
                Source = "sipl",
                Range = new Range(new Position(line, col), new Position(line, col + len))
            };
        }).ToList();

        _server.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = uri,
            Diagnostics = new Container<Diagnostic>(lspDiagnostics)
        });
    }

   protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(
        TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
        => new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("sipl"),
            Change = TextDocumentSyncKind.Full,
            Save = new SaveOptions { IncludeText = true }
        };
}