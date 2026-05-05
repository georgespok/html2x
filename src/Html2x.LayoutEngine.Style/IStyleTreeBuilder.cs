using Html2x.Diagnostics.Contracts;

namespace Html2x.LayoutEngine.Style;

internal interface IStyleTreeBuilder
{
    Task<StyleTree> BuildAsync(
        string html,
        StyleBuildSettings settings,
        CancellationToken cancellationToken = default,
        IDiagnosticsSink? diagnosticsSink = null);
}
