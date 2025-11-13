using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Renderers.Pdf.Mapping;
using Html2x.Renderers.Pdf.Options;
using Html2x.Renderers.Pdf.Pipeline;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Html2x.Renderers.Pdf.Rendering;

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
            var cursorY = 0f;
            var children = fragment.Children;

            for (var i = 0; i < children.Count;)
            {
                var child = children[i];
                var relativeTop = child.Rect.Y - fragment.Rect.Y;
                var topSpacing = relativeTop - cursorY;
                if (topSpacing > 0)
                {
                    inner.Item().Height(topSpacing);
                    cursorY += topSpacing;
                }

                var childHeight = Math.Max(child.Rect.Height, 0);
                var relativeLeft = child.Rect.X - fragment.Rect.X;

                switch (child)
                {
                    case LineBoxFragment line:
                        inner.Item().MinHeight(childHeight).Element(item =>
                        {
                            item.Row(row =>
                            {
                                if (relativeLeft > 0)
                                {
                                    row.ConstantItem(relativeLeft).Element(_ => { });
                                }

                                row.RelativeItem().Element(box =>
                                {
                                    RenderSingleLine(box, line);
                                });
                            });
                        });
                        i++;
                        break;
                    default:
                        i++;
                        PdfRendererLog.FragmentStart(_logger, child);
                        inner.Item().MinHeight(childHeight).Element(item =>
                        {
                            item.Row(row =>
                            {
                                if (relativeLeft > 0)
                                {
                                    row.ConstantItem(relativeLeft).Element(_ => { });
                                }

                                var childWidth = Math.Max(Math.Min(child.Rect.Width, fragment.Rect.Width - relativeLeft), 0);
                                row.ConstantItem(childWidth).Element(box =>
                                {
                                    var childRenderer = new QuestPdfFragmentRenderer(box, _options, _logger);
                                    renderChild(child, childRenderer);
                                });
                            });
                        });
                        break;
                }

                cursorY = Math.Max(cursorY, relativeTop + childHeight);
            }
        });
    }

    public void RenderLine(LineBoxFragment fragment)
    {
        RenderSingleLine(_container, fragment);
    }

    public void RenderImage(ImageFragment fragment)
    {
        PdfRendererLog.FragmentUnsupported(_logger, fragment);
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

        if (BorderPainter.GetUniformBorder(style.Borders) is { } border)
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




