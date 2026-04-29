using AngleSharp;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Options;
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
        DiagnosticsSession? diagnosticsSession = null,
        CancellationToken cancellationToken = default)
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
            diagnosticsSession);

        return RunStage(
            "stage/style",
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return _styleComputer.Compute(document, diagnosticsSession);
            },
            diagnosticsSession);
    }

    private static T RunStage<T>(string stage, Func<T> action, DiagnosticsSession? diagnosticsSession = null)
    {
        diagnosticsSession?.Events.Add(DiagnosticsEventFactory.StageStarted(stage));
        try
        {
            var result = action();
            diagnosticsSession?.Events.Add(DiagnosticsEventFactory.StageSucceeded(stage));
            return result;
        }
        catch (Exception exception)
        {
            diagnosticsSession?.Events.Add(DiagnosticsEventFactory.StageFailed(stage, exception.Message));
            throw;
        }
    }

    private static async Task<T> RunStageAsync<T>(
        string stage,
        Func<Task<T>> action,
        DiagnosticsSession? diagnosticsSession = null)
    {
        diagnosticsSession?.Events.Add(DiagnosticsEventFactory.StageStarted(stage));
        try
        {
            var result = await action();
            diagnosticsSession?.Events.Add(DiagnosticsEventFactory.StageSucceeded(stage));
            return result;
        }
        catch (Exception exception)
        {
            diagnosticsSession?.Events.Add(DiagnosticsEventFactory.StageFailed(stage, exception.Message));
            throw;
        }
    }
}
