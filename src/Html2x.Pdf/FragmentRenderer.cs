using Html2x.Core.Layout;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Html2x.Pdf;

/// <summary>
///     The concrete PDF renderer that implements fragment rendering
///     logic using QuestPDF.
/// </summary>
internal sealed class FragmentRenderer(IContainer container, PdfOptions options) : IFragmentVisitor
{
    public void Visit(BlockFragment fragment)
    {
        container.Column(inner =>
        {
            // Optional MVP background fill
            if (fragment.Style?.BackgroundColor is { } bg)
            {
                inner.Item().Background(QuestPdfStyleMapper.Map(bg));
            }

            foreach (var child in fragment.Children)
            {
                child.VisitWith(new FragmentRenderer(inner.Item(), options));
            }
        });
    }

    public void Visit(LineBoxFragment fragment)
    {
        container.Text(text =>
        {
            foreach (var run in fragment.Runs)
            {
                QuestPdfStyleMapper.ApplyTextStyle(text, run);
            }
        });
    }

    public void Visit(ImageFragment fragment)
    {
        //if (string.IsNullOrEmpty(fragment.ImageSource))
        //{
        //    container.Text("🖼️ [image]");
        //    return;
        //}

        //container.Image(fragment.ImageSource).FitWidth();
    }

    public void Visit(RuleFragment fragment)
    {
        var color = QuestPdfStyleMapper.Map(
            fragment.Style?.BorderTop?.Color ?? new ColorRgba(0, 0, 0, 255));
        var width = fragment.Style?.BorderTop?.Width ?? 1f;

        container.LineHorizontal(width).LineColor(color);
    }
}