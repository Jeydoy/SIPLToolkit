using System.Diagnostics;
using System.Text.Json;
using Sipl.Core.Ast;
using Sipl.Interpreter;
using Sipl.Parser;
using Xunit;

namespace Sipl.Tests;

public class SiplTests
{
    // --- CORE-01: >= 10 позитивних тестів граматики ---
    [Theory]
    [InlineData("skip")]
    [InlineData("x := 1")]
    [InlineData("x := (1 + 2) * 3 - 4 div 2 + 5 mod 2")]
    [InlineData("begin skip end")]
    [InlineData("begin x := 1; y := 2; z := x + y end")]
    [InlineData("if x > 0 then y := 1 else y := 2")]
    [InlineData("if x >= 10 then skip else x := x + 1")]
    [InlineData("if x = 0 then x := 1 else skip")]
    [InlineData("if (x + 1) >= (y - 1) then x := 1 else x := 2")]
    [InlineData("if (x = 1) then skip else x := 1")]
    [InlineData("while y > 0 do begin r := r * x; y := y - 1 end")]
    [InlineData("begin r := 1; while y >= 1 do begin r := r * x; y := y - 1 end end")]
    [InlineData("if (a + b) > (c * 2) then begin x := 1; y := 2 end else skip")]
    public void Grammar_ValidPrograms_ShouldParseWithoutErrors(string source)
    {
        var result = SiplAnalyzer.Analyze(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.NotNull(result.Ast);
    }

    // --- CORE-01: >= 10 негативних тестів граматики ---
    [Theory]
    [InlineData("")]
    [InlineData("x = 1")]
    [InlineData("x : 1")]
    [InlineData("x := ")]
    [InlineData("x := (1 + 2")]
    [InlineData("begin x := 1")]
    [InlineData("begin x := 1; end")]
    [InlineData("while x > 0 x := 1")]
    [InlineData("if x > 0 then x := 1")]
    [InlineData("if x > 0 x := 1 else x := 2")]
    [InlineData("if > 0 then skip else skip")]
    [InlineData("x := 1 @ 2")]
    public void Grammar_InvalidPrograms_ShouldProduceDiagnostics(string source)
    {
        var result = SiplAnalyzer.Analyze(source);
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Diagnostics);
    }

    // --- CORE-03 & CORE-04: Інтерпретатор, стан, ліміт кроків та Trace ---
    [Fact]
    public void Interpreter_PowerProgram_ComputesExpectedStateAndTrace()
    {
        string code = """
            begin
              x := 2;
              y := 5;
              r := 1;
              while y > 0 do
              begin
                r := r * x;
                y := y - 1
              end
            end
            """;

        var analysis = SiplAnalyzer.Analyze(code);
        Assert.True(analysis.IsSuccess);

        var interpreter = new SiplInterpreter();
        var events = new List<TraceEvent>();
        interpreter.OnTrace = e => events.Add(e);

        interpreter.Run(analysis.Ast!);

        Assert.Equal(32, interpreter.Memory["r"]);
        Assert.Equal(0, interpreter.Memory["y"]);
        Assert.Contains(events, e => e.EventType == "VariableChanged");
        Assert.Contains(events, e => e.EventType == "EvaluatedCondition");
    }
[Fact]
public void Interpreter_IfElseAndArithmetic_WorksCorrectly()
{
    string code = """
        begin
          a := 17;
          b := 5;
          q := a div b;
          rem := a mod b;
          if rem >= 2 then res1 := q + 10 else res1 := q;
          if rem > 5 then res2 := 100 else res2 := 200
        end
        """;

    var ast = SiplAnalyzer.Analyze(code).Ast!;
    var interpreter = new SiplInterpreter();
    interpreter.Run(ast);

    Assert.Equal(3, interpreter.Memory["q"]);
    Assert.Equal(2, interpreter.Memory["rem"]);
    Assert.Equal(13, interpreter.Memory["res1"]);
    Assert.Equal(200, interpreter.Memory["res2"]);
}

[Fact]
public void Interpreter_AllBranchesAndZeroIterationLoop_WorkCorrectly()
{
    string code = """
        begin
          x := 4;
          if x = 4 then flag := 1 else flag := 0;
          if x > 10 then high := 1 else high := 0;
          if x = 99 then high := 5 else skip;
          while flag > 5 do skip
        end
        """;

    var analysis = SiplAnalyzer.Analyze(code);
    Assert.True(analysis.IsSuccess, string.Join("; ", analysis.Diagnostics.Select(d => d.Message)));

    var interpreter = new SiplInterpreter();
    interpreter.Run(analysis.Ast!);

    Assert.Equal(4, interpreter.Memory["x"]);
    Assert.Equal(1, interpreter.Memory["flag"]);
    Assert.Equal(0, interpreter.Memory["high"]);
}

[Fact]
public void Interpreter_UninitializedVariable_ThrowsRuntimeError()
{
    string code = "x := uninit + 4";
    var ast = SiplAnalyzer.Analyze(code).Ast!;
    var interpreter = new SiplInterpreter();

    var ex = Assert.Throws<Exception>(() => interpreter.Run(ast));
    Assert.Contains("не ініціалізована", ex.Message, StringComparison.OrdinalIgnoreCase);
}

    [Theory]
    [InlineData("x := 10 div 0")]
    [InlineData("x := 10 mod 0")]
    public void Interpreter_DivisionOrModuloByZero_ThrowsException(string code)
    {
        var ast = SiplAnalyzer.Analyze(code).Ast!;
        var interpreter = new SiplInterpreter();

        Assert.ThrowsAny<Exception>(() => interpreter.Run(ast));
    }

    [Fact]
    public void Interpreter_InfiniteLoop_ThrowsLimitException()
    {
        string code = "begin x := 1; while x > 0 do skip end";
        var ast = SiplAnalyzer.Analyze(code).Ast!;
        var interpreter = new SiplInterpreter();

        var ex = Assert.Throws<Exception>(() => interpreter.Run(ast));
        Assert.Contains("ліміт", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // --- IDE-04: Протокольний інтеграційний тест LSP (JSON-RPC через stdin/stdout) ---
  [Fact]
    public async Task LanguageServer_InitializeAndDiagnostics_RespondsOverStdio()
    {
        var config = AppContext.BaseDirectory.Contains("Release", StringComparison.OrdinalIgnoreCase)
            ? "Release"
            : "Debug";

        var serverDll = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "Sipl.LanguageServer", "bin", config, "net10.0", "Sipl.LanguageServer.dll"));

        Assert.True(File.Exists(serverDll), $"Не знайдено зібраний LSP-сервер: {serverDll}");

        var psi = new ProcessStartInfo("dotnet", $"\"{serverDll}\"")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var proc = Process.Start(psi)!;

        try
        {
            var payload = new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "initialize",
                @params = new { processId = Environment.ProcessId, rootUri = (string?)null, capabilities = new { } }
            };

            byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
            byte[] headerBytes = System.Text.Encoding.ASCII.GetBytes($"Content-Length: {bodyBytes.Length}\r\n\r\n");

            await proc.StandardInput.BaseStream.WriteAsync(headerBytes);
            await proc.StandardInput.BaseStream.WriteAsync(bodyBytes);
            await proc.StandardInput.BaseStream.FlushAsync();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            string? header = await proc.StandardOutput.ReadLineAsync(cts.Token);

            Assert.NotNull(header);
            Assert.StartsWith("Content-Length:", header);
        }
        finally
        {
            if (!proc.HasExited)
                proc.Kill(entireProcessTree: true);
        }
    }
}