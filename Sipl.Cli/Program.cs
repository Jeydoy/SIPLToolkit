using System.Text.Json;
using Sipl.Parser;
using Sipl.Interpreter;

namespace Sipl.Cli;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Використання: Sipl.Cli <parse|check|run|trace> <file.sipl> [--json]");
            return 1;
        }

        string command = args[0].ToLowerInvariant();
        string filePath = args[1];
        bool jsonMode = args.Contains("--json", StringComparer.OrdinalIgnoreCase);
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        if (!File.Exists(filePath))
        {
            WriteError($"Файл '{filePath}' не знайдено.", jsonMode, jsonOptions);
            return 1;
        }

        string source = File.ReadAllText(filePath);
        var analysis = SiplAnalyzer.Analyze(source);

        if (!analysis.IsSuccess)
        {
            if (jsonMode)
                Console.WriteLine(JsonSerializer.Serialize(new { ok = false, diagnostics = analysis.Diagnostics }, jsonOptions));
            else
                foreach (var d in analysis.Diagnostics)
                    Console.Error.WriteLine($"[{d.Span.Line}:{d.Span.Column}] {d.Message}");
            return 1;
        }

        if (command == "check")
        {
            if (jsonMode)
                Console.WriteLine(JsonSerializer.Serialize(new { ok = true, diagnostics = Array.Empty<object>() }, jsonOptions));
            else
                Console.WriteLine("OK: синтаксичних помилок не знайдено.");
            return 0;
        }

        if (command == "parse")
        {
            Console.WriteLine(JsonSerializer.Serialize(analysis.Ast, jsonOptions));
            return 0;
        }

        try
        {
            var interpreter = new SiplInterpreter();
            var traces = new List<TraceEvent>();

            if (command == "trace")
            {
                interpreter.OnTrace = e =>
                {
                    if (jsonMode) traces.Add(e);
                    else Console.WriteLine($"[{e.EventType}] {e.Message}");
                };
            }

            interpreter.Run(analysis.Ast!);

            if (command == "trace")
            {
               if (jsonMode)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { ok = true, memory = interpreter.Memory, trace = traces }, jsonOptions));
                }
                else
                {
                    Console.WriteLine("--- Фінальний стан пам'яті ---");
                    foreach (var kvp in interpreter.Memory)
                        Console.WriteLine($"{kvp.Key} = {kvp.Value}");
                }
                return 0;
            }

            if (command == "run")
            {
                if (jsonMode)
                    Console.WriteLine(JsonSerializer.Serialize(new { ok = true, memory = interpreter.Memory }, jsonOptions));
                else
                    foreach (var kvp in interpreter.Memory)
                        Console.WriteLine($"{kvp.Key} = {kvp.Value}");
                return 0;
            }

            WriteError($"Невідома команда '{command}'.", jsonMode, jsonOptions);
            return 1;
        }
        catch (Exception ex)
        {
            WriteError(ex.Message, jsonMode, jsonOptions);
            return 1;
        }
    }

    private static void WriteError(string message, bool jsonMode, JsonSerializerOptions options)
    {
        if (jsonMode)
            Console.WriteLine(JsonSerializer.Serialize(new { ok = false, error = message }, options));
        else
            Console.Error.WriteLine(message);
    }
}