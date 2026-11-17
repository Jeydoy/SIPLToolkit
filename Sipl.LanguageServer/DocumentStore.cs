namespace Sipl.LanguageServer;

using System.Collections.Concurrent;
using OmniSharp.Extensions.LanguageServer.Protocol;
using Sipl.Parser;

public class DocumentStore
{
    private readonly ConcurrentDictionary<DocumentUri, (string Text, AnalysisResult Analysis)> _docs = new();

    public AnalysisResult Update(DocumentUri uri, string text)
    {
        var result = SiplAnalyzer.Analyze(text);
        _docs[uri] = (text, result);
        return result;
    }

    public bool TryGet(DocumentUri uri, out (string Text, AnalysisResult Analysis) entry)
        => _docs.TryGetValue(uri, out entry);

    public void Remove(DocumentUri uri) => _docs.TryRemove(uri, out _);
}