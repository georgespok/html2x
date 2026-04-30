using AngleSharp;
using Html2x.Abstractions.Options;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Dom;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Style;

public sealed class StyleTreeBuilder : IStyleTreeBuilder
{
    private readonly AngleSharpDomProvider _domProvider;
    private readonly CssStyleComputer _styleComputer;

    public StyleTreeBuilder()
        : this(new AngleSharpDomProvider(Configuration.Default.WithCss()), new CssStyleComputer())
    {
    }

    internal StyleTreeBuilder(AngleSharpDomProvider domProvider, CssStyleComputer styleComputer)
    {
        _domProvider = domProvider ?? throw new ArgumentNullException(nameof(domProvider));
        _styleComputer = styleComputer ?? throw new ArgumentNullException(nameof(styleComputer));
    }

    public async Task<StyleTree> BuildAsync(
        string html,
        LayoutOptions options,
        CancellationToken cancellationToken = default,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(options);

        var document = await RunStageAsync(
            "stage/dom",
            async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var loaded = await _domProvider.LoadAsync(html, options);
                cancellationToken.ThrowIfCancellationRequested();
                return loaded;
            },
            diagnosticsSink);

        return RunStage(
            "stage/style",
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return _styleComputer.Compute(document, diagnosticsSink);
            },
            diagnosticsSink);
    }

    private static T RunStage<T>(
        string stage,
        Func<T> action,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        EmitStageStarted(diagnosticsSink, stage);
        try
        {
            var result = action();
            EmitStageSucceeded(diagnosticsSink, stage);
            return result;
        }
        catch (Exception exception)
        {
            EmitStageFailed(diagnosticsSink, stage, exception.Message);
            throw;
        }
    }

    private static async Task<T> RunStageAsync<T>(
        string stage,
        Func<Task<T>> action,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        EmitStageStarted(diagnosticsSink, stage);
        try
        {
            var result = await action();
            EmitStageSucceeded(diagnosticsSink, stage);
            return result;
        }
        catch (Exception exception)
        {
            EmitStageFailed(diagnosticsSink, stage, exception.Message);
            throw;
        }
    }

    private static void EmitStageStarted(IDiagnosticsSink? diagnosticsSink, string stage) =>
        EmitDiagnosticsRecord(diagnosticsSink, stage, "stage/started", DiagnosticSeverity.Info, null);

    private static void EmitStageSucceeded(IDiagnosticsSink? diagnosticsSink, string stage) =>
        EmitDiagnosticsRecord(diagnosticsSink, stage, "stage/succeeded", DiagnosticSeverity.Info, null);

    private static void EmitStageFailed(IDiagnosticsSink? diagnosticsSink, string stage, string message) =>
        EmitDiagnosticsRecord(diagnosticsSink, stage, "stage/failed", DiagnosticSeverity.Error, message);

    private static void EmitDiagnosticsRecord(
        IDiagnosticsSink? diagnosticsSink,
        string stage,
        string name,
        DiagnosticSeverity severity,
        string? message)
    {
        diagnosticsSink?.Emit(new DiagnosticRecord(
            Stage: stage,
            Name: name,
            Severity: severity,
            Message: message,
            Context: null,
            Fields: DiagnosticFields.Empty,
            Timestamp: DateTimeOffset.UtcNow));
    }
}
