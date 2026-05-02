using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Style;

public interface IStyleTreeBuilder
{
    Task<StyleTree> BuildAsync(
        string html,
        StyleBuildSettings settings,
        CancellationToken cancellationToken = default,
        IDiagnosticsSink? diagnosticsSink = null);
}
