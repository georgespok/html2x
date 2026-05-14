using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Test;

internal static class TableBoxTree
{
    public static TableBox Create(float? widthPt = null, params int[] rowCellCounts) =>
        CreateTable(new() { WidthPt = widthPt }, null, rowCellCounts);

    public static TableBox CreateWithStyle(ComputedStyle style, params int[] rowCellCounts)
    {
        ArgumentNullException.ThrowIfNull(style);

        return CreateTable(style, null, rowCellCounts);
    }

    public static TableBox AddTable(BlockBox parent, float? widthPt = null, params int[] rowCellCounts)
    {
        ArgumentNullException.ThrowIfNull(parent);

        var table = CreateTable(new() { WidthPt = widthPt }, parent, rowCellCounts);
        parent.AddChild(table);

        return table;
    }

    public static TableBox AddTableWithStyle(BlockBox parent, ComputedStyle style, params int[] rowCellCounts)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(style);

        var table = CreateTable(style, parent, rowCellCounts);
        parent.AddChild(table);

        return table;
    }

    public static TableSectionBox AddSection(
        TableBox table,
        string tagName = HtmlCssVocabulary.HtmlTags.Tbody,
        ComputedStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(table);

        var section = new TableSectionBox(BoxRole.TableSection)
        {
            Parent = table,
            Element = StyledElementFacts.Create(tagName),
            Style = style ?? new()
        };
        table.AddChild(section);

        return section;
    }

    public static TableRowBox AddRow(TableBox table, int cellCount = 0, ComputedStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(table);

        var row = new TableRowBox(BoxRole.TableRow)
        {
            Parent = table,
            Element = StyledElementFacts.Create(HtmlCssVocabulary.HtmlTags.Tr),
            Style = style ?? new()
        };
        table.AddChild(row);
        AddCells(row, cellCount);

        return row;
    }

    public static TableRowBox AddRow(TableSectionBox section, int cellCount = 0, ComputedStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(section);

        var row = new TableRowBox(BoxRole.TableRow)
        {
            Parent = section,
            Element = StyledElementFacts.Create(HtmlCssVocabulary.HtmlTags.Tr),
            Style = style ?? new()
        };
        section.AddChild(row);
        AddCells(row, cellCount);

        return row;
    }

    public static TableCellBox AddCell(
        TableRowBox row,
        ComputedStyle? style = null,
        bool isHeader = false,
        string? colspan = null,
        string? rowspan = null)
    {
        ArgumentNullException.ThrowIfNull(row);

        var cell = new TableCellBox(BoxRole.TableCell)
        {
            Parent = row,
            Element = CreateCellElement(isHeader, colspan, rowspan),
            Style = style ?? new()
        };
        row.AddChild(cell);

        return cell;
    }

    public static BlockBox AddBlock(TableCellBox parent, ComputedStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(parent);

        var block = new BlockBox(BoxRole.Block)
        {
            Parent = parent,
            Style = style ?? new()
        };
        parent.AddChild(block);

        return block;
    }

    public static InlineBox AddInline(BlockBox parent, string textContent, ComputedStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(parent);

        var inline = new InlineBox(BoxRole.Inline)
        {
            Parent = parent,
            TextContent = textContent,
            Style = style ?? parent.Style
        };
        parent.AddChild(inline);

        return inline;
    }

    private static TableBox CreateTable(ComputedStyle style, BoxNode? parent, int[] rowCellCounts)
    {
        var table = new TableBox(BoxRole.Table)
        {
            Parent = parent,
            Element = StyledElementFacts.Create(HtmlCssVocabulary.HtmlTags.Table),
            Style = style
        };

        foreach (var cellCount in rowCellCounts)
        {
            AddRow(table, cellCount);
        }

        return table;
    }

    private static void AddCells(TableRowBox row, int cellCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cellCount);

        for (var i = 0; i < cellCount; i++)
        {
            AddCell(row);
        }
    }

    private static StyledElementFacts CreateCellElement(bool isHeader, string? colspan, string? rowspan)
    {
        var attributes = new List<(string Name, string Value)>();
        if (colspan is not null)
        {
            attributes.Add((HtmlCssVocabulary.HtmlAttributes.Colspan, colspan));
        }

        if (rowspan is not null)
        {
            attributes.Add((HtmlCssVocabulary.HtmlAttributes.Rowspan, rowspan));
        }

        var tagName = isHeader
            ? HtmlCssVocabulary.HtmlTags.Th
            : HtmlCssVocabulary.HtmlTags.Td;

        return StyledElementFacts.Create(tagName, attributes.ToArray());
    }
}
