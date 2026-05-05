using AngleSharp;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Style.Document;
using Html2x.LayoutEngine.Style.Style;

namespace Html2x.LayoutEngine.Style;

internal sealed class StyleTreeBuilder : IStyleTreeBuilder
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
        StyleBuildSettings settings,
        CancellationToken cancellationToken = default,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(settings);

        var document = await DiagnosticStage.RunAsync(
            diagnosticsSink,
            StyleDiagnosticNames.Stages.Dom,
            async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var loaded = await _domProvider.LoadAsync(html, settings);
                cancellationToken.ThrowIfCancellationRequested();
                return loaded;
            },
            cancellationToken);

        return DiagnosticStage.Run(
            diagnosticsSink,
            StyleDiagnosticNames.Stages.Style,
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return _styleComputer.Compute(document, diagnosticsSink);
            },
            cancellationToken);
    }
}
