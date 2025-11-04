using System;
using Html2x.Core.Layout;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Html2x.Pdf;

internal sealed class QuestPdfFragmentRenderer : IFragmentRenderer
{
    private readonly IContainer _container;
    private readonly PdfOptions _options;

    public QuestPdfFragmentRenderer(IContainer container, PdfOptions options)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public void RenderBlock(BlockFragment fragment, Action<Fragment, IFragmentRenderer> _)
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
                        var childRenderer = new QuestPdfFragmentRenderer(inner.Item(), _options);
                        child.VisitWith(new FragmentRenderDispatcher(childRenderer));
                        break;
                    default:
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
        // Placeholder for image support
    }

    public void RenderRule(RuleFragment fragment)
    {
        var color = QuestPdfStyleMapper.Map(
            fragment.Style?.BorderTop?.Color ?? new ColorRgba(0, 0, 0, 255));
        var width = fragment.Style?.BorderTop?.Width ?? 1f;

        _container.LineHorizontal(width).LineColor(color);
    }

    private void RenderSingleLine(IContainer container, LineBoxFragment line)
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
