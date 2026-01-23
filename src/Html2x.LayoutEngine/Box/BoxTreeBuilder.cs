using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

public class BoxTreeBuilder : IBoxTreeBuilder
{
    private readonly ITextMeasurer? _textMeasurer;

    public BoxTreeBuilder()
    {
    }

    public BoxTreeBuilder(ITextMeasurer textMeasurer)
    {
        _textMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
    }

    public BoxTree Build(StyleTree styles, DiagnosticsSession? diagnosticsSession = null)
    {
        var displayTreeBuilder = new DisplayTreeBuilder();
        var displayRoot = displayTreeBuilder.Build(styles);

        var blockEngine = new BlockLayoutEngine(
            new InlineLayoutEngine(new FontMetricsProvider(), _textMeasurer, new DefaultLineHeightStrategy()),
            new TableLayoutEngine(),
            new FloatLayoutEngine(),
            diagnosticsSession);

        var page = new PageBox
        {
            Margin = styles.Page.Margin
        };

        return blockEngine.Layout(displayRoot, page);
    }
}
