using Html2x.RenderModel;

namespace Html2x.Renderers.Pdf.Paint;

/// <summary>
/// Encodes the current fragment traversal and layering rules as explicit paint command order.
/// </summary>
internal sealed class PaintOrderResolver
{
    private static readonly ColorRgba DefaultPageBackground = new(255, 255, 255, 255);

    public IReadOnlyList<PaintCommand> Resolve(LayoutPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        var commands = new PaintCommandAccumulator();
        commands.AddPageBackground(page, page.PageBackground ?? DefaultPageBackground);

        foreach (var fragment in page.Children)
        {
            AddFragmentCommands(page.PageNumber, fragment, commands);
        }

        return commands.ToPaintOrder();
    }

    private static void AddFragmentCommands(
        int pageNumber,
        Fragment fragment,
        PaintCommandAccumulator commands)
    {
        switch (fragment)
        {
            case TableFragment table:
                AddTableCommands(pageNumber, table, commands);
                break;
            case TableRowFragment row:
                AddBlockBackgroundCommand(pageNumber, row, commands);
                AddBlockBorderCommand(pageNumber, row, commands);
                break;
            case TableCellFragment cell:
                AddBlockBackgroundCommand(pageNumber, cell, commands);
                AddBlockBorderCommand(pageNumber, cell, commands);
                AddTableCellContentCommands(pageNumber, cell, commands);
                break;
            case BlockFragment block:
                AddBlockBackgroundCommand(pageNumber, block, commands);
                AddBlockBorderCommand(pageNumber, block, commands);
                foreach (var child in block.Children)
                {
                    AddFragmentCommands(pageNumber, child, commands);
                }

                break;
            case LineBoxFragment line:
                AddLineCommands(pageNumber, line, commands);
                break;
            case ImageFragment image:
                AddImageCommands(pageNumber, image, commands);
                break;
            case RuleFragment rule:
                commands.Add(new RulePaintCommand(
                    pageNumber,
                    rule.FragmentId,
                    rule.Rect,
                    rule.ZOrder,
                    commands.NextIndex(),
                    rule.Style?.Borders?.Top));
                break;
            default:
                throw new NotSupportedException($"Unsupported fragment type: {fragment.GetType().Name}");
        }
    }

    private static void AddTableCommands(
        int pageNumber,
        TableFragment table,
        PaintCommandAccumulator commands)
    {
        AddBlockBackgroundCommand(pageNumber, table, commands);

        foreach (var row in table.Rows)
        {
            AddBlockBackgroundCommand(pageNumber, row, commands);
        }

        foreach (var row in table.Rows)
        {
            foreach (var cell in row.Cells)
            {
                AddBlockBackgroundCommand(pageNumber, cell, commands);
            }
        }

        AddBlockBorderCommand(pageNumber, table, commands);

        foreach (var row in table.Rows)
        {
            AddBlockBorderCommand(pageNumber, row, commands);

            foreach (var cell in row.Cells)
            {
                AddBlockBorderCommand(pageNumber, cell, commands);
            }
        }

        foreach (var row in table.Rows)
        {
            foreach (var cell in row.Cells)
            {
                AddTableCellContentCommands(pageNumber, cell, commands);
            }
        }
    }

    private static void AddTableCellContentCommands(
        int pageNumber,
        TableCellFragment cell,
        PaintCommandAccumulator commands)
    {
        foreach (var child in cell.Children)
        {
            AddFragmentCommands(pageNumber, child, commands);
        }
    }

    private static void AddBlockBackgroundCommand(
        int pageNumber,
        BlockFragment block,
        PaintCommandAccumulator commands)
    {
        var background = block.Style?.BackgroundColor;
        if (background is null || background.Value.A == 0)
        {
            return;
        }

        commands.Add(new BackgroundPaintCommand(
            pageNumber,
            block.FragmentId,
            block.Rect,
            block.ZOrder,
            commands.NextIndex(),
            background.Value));
    }

    private static void AddBlockBorderCommand(
        int pageNumber,
        BlockFragment block,
        PaintCommandAccumulator commands)
    {
        var borders = block.Style?.Borders;
        if (!ShouldPaintBorder(block.Rect, borders))
        {
            return;
        }

        commands.Add(new BorderPaintCommand(
            pageNumber,
            block.FragmentId,
            block.Rect,
            block.ZOrder,
            commands.NextIndex(),
            borders!));
    }

    private static void AddLineCommands(
        int pageNumber,
        LineBoxFragment line,
        PaintCommandAccumulator commands)
    {
        foreach (var run in line.Runs)
        {
            commands.Add(new TextPaintCommand(
                pageNumber,
                line.FragmentId,
                line.Rect,
                line.ZOrder,
                commands.NextIndex(),
                run));
        }
    }

    private static void AddImageCommands(
        int pageNumber,
        ImageFragment image,
        PaintCommandAccumulator commands)
    {
        commands.Add(new ImagePaintCommand(
            pageNumber,
            image.FragmentId,
            image.Rect,
            image.ZOrder,
            commands.NextIndex(),
            image.Style ?? new VisualStyle(),
            image.Src,
            image.ContentRect,
            image.AuthoredSizePx,
            image.IntrinsicSizePx,
            image.Status,
            image.IsMissing,
            image.IsOversize));

        var borders = image.Style?.Borders;
        if (!ShouldPaintBorder(image.Rect, borders))
        {
            return;
        }

        commands.Add(new BorderPaintCommand(
            pageNumber,
            image.FragmentId,
            image.Rect,
            image.ZOrder,
            commands.NextIndex(),
            borders!));
    }

    private static bool ShouldPaintBorder(RectPt rect, BorderEdges? borders)
    {
        return borders is { HasAny: true } && rect.Width > 0f && rect.Height > 0f;
    }

    /// <summary>
    /// Assigns stable command indexes while collecting commands for a single page.
    /// </summary>
    private sealed class PaintCommandAccumulator
    {
        private readonly List<PaintCommand> _commands = [];
        private int _nextIndex;

        public int NextIndex()
        {
            return _nextIndex++;
        }

        public void Add(PaintCommand command)
        {
            _commands.Add(command);
        }

        public void AddPageBackground(LayoutPage page, ColorRgba color)
        {
            Add(new PageBackgroundPaintCommand(
                page.PageNumber,
                page.PageSize,
                color,
                NextIndex()));
        }

        public IReadOnlyList<PaintCommand> ToPaintOrder()
        {
            return _commands
                .OrderBy(static command => command.ZOrder)
                .ThenBy(static command => command.CommandIndex)
                .ToArray();
        }
    }
}
