using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Options;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Style;

public interface IStyleTreeBuilder
{
    Task<StyleTree> BuildAsync(
        string html,
        LayoutOptions options,
        DiagnosticsSession? diagnosticsSession = null,
        CancellationToken cancellationToken = default);
}
