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
        var blockContainer = ApplyBlockDecorations(_container, fragment.Style);

        blockContainer.Column(inner =>
        {
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
            fragment.Style?.Borders?.Top?.Color ?? new ColorRgba(0, 0, 0, 255));
        var width = fragment.Style?.Borders?.Top?.Width ?? 1f;

        _container.LineHorizontal(width).LineColor(color);
    }

    private IContainer ApplyBlockDecorations(IContainer container, VisualStyle? style)
    {
        if (style is null)
        {
            return container;
        }

        var decorated = container;

        if (style.BackgroundColor is { } background)
        {
            decorated = decorated.Background(QuestPdfStyleMapper.Map(background));
        }

        if (BorderRendering.GetUniformBorder(style.Borders) is { } border)
        {
            if (border.LineStyle is BorderLineStyle.Dashed or BorderLineStyle.Dotted)
            {
                _logger.LogDebug("Border style {BorderStyle} not supported, rendering as solid.", border.LineStyle);
            }

            decorated = decorated.Border(border.Width)
                .BorderColor(QuestPdfStyleMapper.Map(border.Color));
        }

        return decorated;
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
