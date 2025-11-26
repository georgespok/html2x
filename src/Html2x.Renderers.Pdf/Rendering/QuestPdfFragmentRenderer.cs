using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Options;
using Html2x.Renderers.Pdf.Drawing;
using Html2x.Renderers.Pdf.Mapping;
using Html2x.Renderers.Pdf.SkiaSharpIntegration;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Rendering;

internal sealed class QuestPdfFragmentRenderer(
    IContainer container,
    PdfOptions options)
    : IFragmentRenderer
{
    private readonly IContainer _container = container ?? throw new ArgumentNullException(nameof(container));
    private readonly PdfOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly BorderShapeDrawer _borderShapeDrawer = new();
    
    public void RenderBlock(BlockFragment fragment, Action<Fragment, IFragmentRenderer> renderChild)
    {
        _container.Layers(layers =>
        {
            // 1. Primary Layer (Background + Content)
            layers.PrimaryLayer().Element(primary =>
            {
                var blockContainer = ApplyBlockDecorations(primary, fragment.Style);

                blockContainer.Column(inner =>
                {
                    RenderChildren(inner, fragment, renderChild);
                });
            });

            // 2. Custom Borders Layer
            if (fragment.Style?.Borders != null)
            {
                layers.Layer().SkiaSharpSvgCanvas((canvas, size) =>
                {
                    _borderShapeDrawer.Draw(canvas, new SKSize(size.Width, size.Height), fragment.Style.Borders);
                });
            }
        });
    }

    private void RenderChildren(ColumnDescriptor inner, BlockFragment fragment, Action<Fragment, IFragmentRenderer> renderChild)
    {
        var cursorY = 0f;
        var children = fragment.Children;

        foreach (var child in children)
        {
            var relativeTop = child.Rect.Y - fragment.Rect.Y;
            var topSpacing = relativeTop - cursorY;
            if (topSpacing > 0)
            {
                inner.Item().Height(topSpacing);
                cursorY += topSpacing;
            }

            var childHeight = Math.Max(child.Rect.Height, 0);
            var relativeLeft = child.Rect.X - fragment.Rect.X;

            if (child is LineBoxFragment line)
            {
                RenderLineBoxChild(inner, line, childHeight, relativeLeft);
            }
            else
            {
                RenderBlockChild(inner, child, childHeight, relativeLeft, fragment.Rect.Width, renderChild);
            }

            cursorY = Math.Max(cursorY, relativeTop + childHeight);
        }
    }

    private static void RenderLineBoxChild(ColumnDescriptor inner, LineBoxFragment line, float height, float relativeLeft)
    {
        inner.Item().MinHeight(height).Element(item =>
        {
            RenderRowWithOffset(item, relativeLeft, row =>
            {
                row.RelativeItem().Element(box =>
                {
                    RenderSingleLine(box, line);
                });
            });
        });
    }

    private void RenderBlockChild(
        ColumnDescriptor inner,
        Fragment child,
        float height,
        float relativeLeft,
        float parentWidth,
        Action<Fragment, IFragmentRenderer> renderChild)
    {
        inner.Item().MinHeight(height).Element(item =>
        {
            RenderRowWithOffset(item, relativeLeft, row =>
            {
                var childWidth = Math.Max(Math.Min(child.Rect.Width, parentWidth - relativeLeft), 0);
                row.ConstantItem(childWidth).Element(box =>
                {
                    var childRenderer = new QuestPdfFragmentRenderer(box, _options);
                    renderChild(child, childRenderer);
                });
            });
        });
    }

    public void RenderLine(LineBoxFragment fragment)
    {
        RenderSingleLine(_container, fragment);
    }

    public void RenderImage(ImageFragment fragment)
    {
        // Image rendering not yet supported
    }

    public void RenderRule(RuleFragment fragment)
    {
        var color = QuestPdfStyleMapper.Map(
            fragment.Style?.Borders?.Top?.Color ?? ColorRgba.Black);
        var width = fragment.Style?.Borders?.Top?.Width ?? 1f;

        _container.LineHorizontal(width).LineColor(color);
    }

    private static IContainer ApplyBlockDecorations(IContainer container, VisualStyle? style)
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

        return decorated;
    }

    private static void RenderSingleLine(IContainer container, LineBoxFragment line)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                ApplyAlignment(text, line.TextAlign);

                foreach (var run in line.Runs)
                {
                    QuestPdfStyleMapper.ApplyTextStyle(text, run);
                }
            });
        });
    }

    private static void ApplyAlignment(TextDescriptor descriptor, string? align)
    {
        var normalized = align?.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "center":
                descriptor.AlignCenter();
                break;
            case "right":
                descriptor.AlignRight();
                break;
            default:
                descriptor.AlignLeft();
                break;
        }
    }
    private static void RenderRowWithOffset(IContainer container, float relativeLeft, Action<RowDescriptor> configureRow)
    {
        container.Row(row =>
        {
            if (relativeLeft > 0)
            {
                row.ConstantItem(relativeLeft).Element(_ => { });
            }

            configureRow(row);
        });
    }
}
