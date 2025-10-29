using Html2x.Layout.Style;

namespace Html2x.Layout.Box;

/// <summary>
///     Converts Style Tree → Box Tree (MVP: h1/p single-line blocks).
/// </summary>
public sealed class SimpleBoxTreeBuilder : IBoxTreeBuilder
{
    public BoxTree Build(StyleTree styles)
    {
        var bt = new BoxTree();
        if (styles.Root is null)
        {
            return bt;
        }

        // Map page margins
        bt.Page.MarginTopPt = styles.Page.MarginTopPt;
        bt.Page.MarginRightPt = styles.Page.MarginRightPt;
        bt.Page.MarginBottomPt = styles.Page.MarginBottomPt;
        bt.Page.MarginLeftPt = styles.Page.MarginLeftPt;

        // Naive vertical flow
        var cursorY = bt.Page.MarginTopPt;
        var contentWidth = bt.Page.PageWidthPt - bt.Page.MarginLeftPt - bt.Page.MarginRightPt;

        foreach (var child in styles.Root.Children)
        {
            var s = child.Style;

            // line-height approximated as 1.2 * font-size
            var lineHeight = MathF.Round((float)(s.FontSizePt * 1.2), 2);

            var block = new BlockBox
            {
                Margin = new Spacing
                {
                    Top = s.MarginTopPt == 0 ? 4 : s.MarginTopPt,
                    Right = s.MarginRightPt,
                    Bottom = s.MarginBottomPt == 0 ? 4 : s.MarginBottomPt,
                    Left = s.MarginLeftPt
                },
                TextAlign = s.TextAlign
                //Inline = new InlineTextBox
                //{
                //    Text = child.Element.TextContent?.Trim() ?? "",
                //    FontFamily = s.FontFamily,
                //    FontSizePt = s.FontSizePt,
                //    Bold = s.Bold,
                //    Italic = s.Italic,
                //    Color = s.Color
                //}
            };

            var x = bt.Page.MarginLeftPt + block.Margin.Left;
            var y = cursorY + block.Margin.Top;

            block.X = x;
            block.Y = y;
            block.Width = MathF.Max(0, contentWidth - block.Margin.Left - block.Margin.Right);
            block.Height = lineHeight;

            bt.Blocks.Add(block);

            cursorY = y + block.Height + block.Margin.Bottom;
        }

        return bt;
    }
}