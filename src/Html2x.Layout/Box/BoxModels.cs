using AngleSharp.Dom;
using Html2x.Layout.Style;

namespace Html2x.Layout.Box;

public sealed class BoxTree
{
    public List<BlockBox> Blocks { get; } = [];
    public PageBox Page { get; } = new();
}

public sealed class PageBox
{
    public float MarginTopPt { get; set; }
    public float MarginRightPt { get; set; }
    public float MarginBottomPt { get; set; }

    public float MarginLeftPt { get; set; }

    // (MVP) fixed page size A4
    public float PageWidthPt { get; set; } = 595;
    public float PageHeightPt { get; set; } = 842;
}

public sealed class Spacing
{
    public float Top { get; set; }
    public float Right { get; set; }
    public float Bottom { get; set; }
    public float Left { get; set; }
}

// new models

public abstract class DisplayNode
{
    public DisplayNode? Parent { get; init; }
    public List<DisplayNode> Children { get; } = [];
    public IElement? Element { get; init; }
    public ComputedStyle Style { get; init; } = new();
}

public sealed class BlockBox : DisplayNode
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public Spacing Margin { get; set; } = new();
    public Spacing Padding { get; set; } = new();
    public string TextAlign { get; set; } = HtmlCssConstants.Defaults.TextAlign;
}

public sealed class InlineBox : DisplayNode
{
    public string? TextContent { get; init; } // For inline text nodes
}

public sealed class FloatBox : DisplayNode
{
    public string FloatDirection { get; init; } = HtmlCssConstants.Defaults.FloatDirection;
}

public sealed class TableBox : DisplayNode
{
}

public sealed class TableSectionBox : DisplayNode
{
}

public sealed class TableRowBox : DisplayNode
{
}

public sealed class TableCellBox : DisplayNode
{
}

public enum DisplayRole
{
    Block,
    Inline,
    Float,
    Table,
    TableRow,
    TableCell
}