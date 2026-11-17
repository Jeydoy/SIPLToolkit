using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Server;
using Sipl.LanguageServer;
using Sipl.LanguageServer.Handlers;

var server = await LanguageServer.From(options =>
    options
        .WithInput(Console.OpenStandardInput())
        .WithOutput(Console.OpenStandardOutput())
        .WithServices(services => services.AddSingleton<DocumentStore>())
        .WithHandler<TextDocumentSyncHandler>()
        .WithHandler<SiplCompletionHandler>()
        .WithHandler<SiplHoverHandler>()
        .WithHandler<SiplDocumentSymbolHandler>()
        .WithHandler<SiplSemanticTokensHandler>()
);

await server.WaitForExit;