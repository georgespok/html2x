using Html2x.Core.Layout;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Html2x.Pdf;

internal sealed class QuestPdfFragmentRenderer(
    IContainer container,
    PdfOptions options,
    ILogger<QuestPdfFragmentRenderer>? logger = null)
    : IFragmentRenderer
{
    private readonly IContainer _container = container ?? throw new ArgumentNullException(nameof(container));
    private readonly PdfOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<QuestPdfFragmentRenderer> _logger = logger ?? NullLogger<QuestPdfFragmentRenderer>.Instance;

    public void RenderBlock(BlockFragment fragment, Action<Fragment, IFragmentRenderer> renderChild)
    {
        _container.Column(inner =>
        {
            if (fragment.Style?.BackgroundColor is { } bg)
            {
                inner.Item().Background(QuestPdfStyleMapper.Map(bg));
            }

            var children = fragment.Children;
            for (var i = 0; i < children.Count;)
            {
                switch (children[i])
                {
                    case LineBoxFragment line:
                        RenderSingleLine(inner.Item(), line);
                        i++;
                        break;
                    case Fragment child:
                        i++;
                        RendererLog.FragmentStart(_logger, child);
                        var childRenderer = new QuestPdfFragmentRenderer(inner.Item(), _options, _logger);
                        renderChild(child, childRenderer);
                        break;
                    default:
                        RendererLog.FragmentUnsupported(_logger, children[i]);
                        i++;
                        break;
                }
            }
        });
    }

    public void RenderLine(LineBoxFragment fragment)
    {
        RenderSingleLine(_container, fragment);
    }

    public void RenderImage(ImageFragment fragment)
    {
        RendererLog.FragmentUnsupported(_logger, fragment);
    }

    public void RenderRule(RuleFragment fragment)
    {
        var color = QuestPdfStyleMapper.Map(
            fragment.Style?.BorderTop?.Color ?? new ColorRgba(0, 0, 0, 255));
        var width = fragment.Style?.BorderTop?.Width ?? 1f;

        _container.LineHorizontal(width).LineColor(color);
    }

    private static void RenderSingleLine(IContainer container, LineBoxFragment line)
    {
        container.Row(row =>
        {
            row.AutoItem().Text(text =>
            {
                foreach (var run in line.Runs)
                {
                    QuestPdfStyleMapper.ApplyTextStyle(text, run);
                }
            });
        });
    }
}
