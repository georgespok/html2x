using AngleSharp;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Style.Document;
using Html2x.LayoutEngine.Style.Computation;

namespace Html2x.LayoutEngine.Style;

internal sealed class StyleTreeBuilder
{
    private readonly AngleSharpDocumentLoader _documentLoader;
    private readonly CssStyleComputer _styleComputer;

    public StyleTreeBuilder()
        : this(new(Configuration.Default.WithCss()), new())
    {
    }

    internal StyleTreeBuilder(AngleSharpDocumentLoader documentLoader, CssStyleComputer styleComputer)
    {
        _documentLoader = documentLoader ?? throw new ArgumentNullException(nameof(documentLoader));
        _styleComputer = styleComputer ?? throw new ArgumentNullException(nameof(styleComputer));
    }

    public async Task<StyleTree> BuildAsync(
        string html,
        StyleBuildSettings settings,
        CancellationToken cancellationToken = default,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(settings);

        var document = await DiagnosticStageRunner.RunAsync(
            diagnosticsSink,
            StyleDiagnosticNames.Stages.Dom,
            async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var loaded = await _documentLoader.LoadAsync(html, settings, cancellationToken);
                return loaded;
            },
            cancellationToken);

        return DiagnosticStageRunner.Run(
            diagnosticsSink,
            StyleDiagnosticNames.Stages.Style,
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return _styleComputer.Compute(document, diagnosticsSink, cancellationToken);
            },
            cancellationToken);
    }
}
