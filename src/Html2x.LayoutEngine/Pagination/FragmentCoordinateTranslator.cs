using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;
using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Pagination;

internal static class FragmentCoordinateTranslator
{
    private static readonly OffsetCloneVisitor Visitor = new();

    internal static LayoutFragment CloneWithPlacement(LayoutFragment source, int pageNumber, float x, float y)
    {
        var deltaX = x - source.Rect.X;
        var deltaY = y - source.Rect.Y;
        return Visitor.Visit(source, pageNumber, deltaX, deltaY);
    }

    internal static BlockFragment CloneBlockWithPlacement(BlockFragment source, int pageNumber, float x, float y)
    {
        var deltaX = x - source.Rect.X;
        var deltaY = y - source.Rect.Y;

        return (BlockFragment)Visitor.Visit(source, pageNumber, deltaX, deltaY);
    }

    internal static void RegisterExtensionTranslator<TFragment>(
        Func<TFragment, int, float, float, LayoutFragment> translator)
        where TFragment : LayoutFragment
    {
        Visitor.RegisterExtensionTranslator(translator);
    }

    internal static IReadOnlyCollection<Type> GetBuiltInSupportedTypes()
    {
        return OffsetCloneVisitor.BuiltInSupportedTypes;
    }

    private sealed class OffsetCloneVisitor
    {
        private static readonly IReadOnlyDictionary<Type, Func<LayoutFragment, int, float, float, LayoutFragment>> BuiltInTranslators =
            new Dictionary<Type, Func<LayoutFragment, int, float, float, LayoutFragment>>
            {
                [typeof(BlockFragment)] = static (fragment, pageNumber, deltaX, deltaY) =>
                    CloneNestedBlockWithOffset((BlockFragment)fragment, pageNumber, deltaX, deltaY),
                [typeof(TableFragment)] = static (fragment, pageNumber, deltaX, deltaY) =>
                    CloneTableWithOffset((TableFragment)fragment, pageNumber, deltaX, deltaY),
                [typeof(TableRowFragment)] = static (fragment, pageNumber, deltaX, deltaY) =>
                    CloneTableRowWithOffset((TableRowFragment)fragment, pageNumber, deltaX, deltaY),
                [typeof(TableCellFragment)] = static (fragment, pageNumber, deltaX, deltaY) =>
                    CloneTableCellWithOffset((TableCellFragment)fragment, pageNumber, deltaX, deltaY),
                [typeof(LineBoxFragment)] = static (fragment, pageNumber, deltaX, deltaY) =>
                    CloneLineWithOffset((LineBoxFragment)fragment, pageNumber, deltaX, deltaY),
                [typeof(ImageFragment)] = static (fragment, pageNumber, deltaX, deltaY) =>
                    CloneImageWithOffset((ImageFragment)fragment, pageNumber, deltaX, deltaY),
                [typeof(RuleFragment)] = static (fragment, pageNumber, deltaX, deltaY) =>
                    CloneRuleWithOffset((RuleFragment)fragment, pageNumber, deltaX, deltaY)
            };

        private readonly Dictionary<Type, Func<LayoutFragment, int, float, float, LayoutFragment>> _extensionTranslators = [];

        internal static IReadOnlyCollection<Type> BuiltInSupportedTypes => BuiltInTranslators.Keys.ToArray();

        internal LayoutFragment Visit(LayoutFragment source, int pageNumber, float deltaX, float deltaY)
        {
            var sourceType = source.GetType();
            if (BuiltInTranslators.TryGetValue(sourceType, out var builtInTranslator))
            {
                return builtInTranslator(source, pageNumber, deltaX, deltaY);
            }

            if (_extensionTranslators.TryGetValue(sourceType, out var extensionTranslator))
            {
                return extensionTranslator(source, pageNumber, deltaX, deltaY);
            }

            throw new NotSupportedException(
                $"No pagination translator registered for fragment type '{sourceType.Name}'. " +
                $"Register one with {nameof(FragmentCoordinateTranslator)}.{nameof(RegisterExtensionTranslator)}.");
        }

        internal void RegisterExtensionTranslator<TFragment>(
            Func<TFragment, int, float, float, LayoutFragment> translator)
            where TFragment : LayoutFragment
        {
            _extensionTranslators[typeof(TFragment)] =
                (fragment, page, x, y) => translator((TFragment)fragment, page, x, y);
        }
    }

    private static LineBoxFragment CloneLineWithOffset(LineBoxFragment source, int pageNumber, float deltaX, float deltaY)
    {
        return new LineBoxFragment
        {
            FragmentId = source.FragmentId,
            PageNumber = pageNumber,
            Rect = OffsetRect(source.Rect, deltaX, deltaY),
            ZOrder = source.ZOrder,
            Style = source.Style,
            BaselineY = source.BaselineY + deltaY,
            LineHeight = source.LineHeight,
            Runs = source.Runs
                .Select(run => run with
                {
                    Origin = new PointF(run.Origin.X + deltaX, run.Origin.Y + deltaY)
                })
                .ToList(),
            TextAlign = source.TextAlign
        };
    }

    private static BlockFragment CloneNestedBlockWithOffset(BlockFragment source, int pageNumber, float deltaX, float deltaY)
    {
        return new BlockFragment(
            source.Children.Select(child => Visitor.Visit(child, pageNumber, deltaX, deltaY)).ToList())
        {
            FragmentId = source.FragmentId,
            PageNumber = pageNumber,
            Rect = OffsetRect(source.Rect, deltaX, deltaY),
            ZOrder = source.ZOrder,
            Style = source.Style,
            DisplayRole = source.DisplayRole,
            FormattingContext = source.FormattingContext,
            MarkerOffset = source.MarkerOffset
        };
    }

    private static TableFragment CloneTableWithOffset(TableFragment source, int pageNumber, float deltaX, float deltaY)
    {
        return new TableFragment(
            source.Rows
                .Select(row => CloneTableRowWithOffset(row, pageNumber, deltaX, deltaY))
                .ToList())
        {
            FragmentId = source.FragmentId,
            PageNumber = pageNumber,
            Rect = OffsetRect(source.Rect, deltaX, deltaY),
            ZOrder = source.ZOrder,
            Style = source.Style,
            DisplayRole = source.DisplayRole,
            FormattingContext = source.FormattingContext,
            MarkerOffset = source.MarkerOffset,
            DerivedColumnCount = source.DerivedColumnCount
        };
    }

    private static TableRowFragment CloneTableRowWithOffset(TableRowFragment source, int pageNumber, float deltaX, float deltaY)
    {
        return new TableRowFragment(
            source.Cells
                .Select(cell => CloneTableCellWithOffset(cell, pageNumber, deltaX, deltaY))
                .ToList())
        {
            FragmentId = source.FragmentId,
            PageNumber = pageNumber,
            Rect = OffsetRect(source.Rect, deltaX, deltaY),
            ZOrder = source.ZOrder,
            Style = source.Style,
            DisplayRole = source.DisplayRole,
            FormattingContext = source.FormattingContext,
            MarkerOffset = source.MarkerOffset,
            RowIndex = source.RowIndex
        };
    }

    private static TableCellFragment CloneTableCellWithOffset(TableCellFragment source, int pageNumber, float deltaX, float deltaY)
    {
        return new TableCellFragment(
            source.Children
                .Select(child => Visitor.Visit(child, pageNumber, deltaX, deltaY))
                .ToList())
        {
            FragmentId = source.FragmentId,
            PageNumber = pageNumber,
            Rect = OffsetRect(source.Rect, deltaX, deltaY),
            ZOrder = source.ZOrder,
            Style = source.Style,
            DisplayRole = source.DisplayRole,
            FormattingContext = source.FormattingContext,
            MarkerOffset = source.MarkerOffset,
            ColumnIndex = source.ColumnIndex,
            IsHeader = source.IsHeader
        };
    }

    private static ImageFragment CloneImageWithOffset(ImageFragment source, int pageNumber, float deltaX, float deltaY)
    {
        return new ImageFragment
        {
            FragmentId = source.FragmentId,
            PageNumber = pageNumber,
            Rect = OffsetRect(source.Rect, deltaX, deltaY),
            ZOrder = source.ZOrder,
            Style = source.Style,
            Src = source.Src,
            ContentRect = OffsetRect(source.ContentRect, deltaX, deltaY),
            AuthoredSizePx = source.AuthoredSizePx,
            IntrinsicSizePx = source.IntrinsicSizePx,
            IsMissing = source.IsMissing,
            IsOversize = source.IsOversize
        };
    }

    private static RuleFragment CloneRuleWithOffset(RuleFragment source, int pageNumber, float deltaX, float deltaY)
    {
        return new RuleFragment
        {
            FragmentId = source.FragmentId,
            PageNumber = pageNumber,
            Rect = OffsetRect(source.Rect, deltaX, deltaY),
            ZOrder = source.ZOrder,
            Style = source.Style
        };
    }

    private static RectangleF OffsetRect(RectangleF rect, float deltaX, float deltaY)
    {
        return new RectangleF(rect.X + deltaX, rect.Y + deltaY, rect.Width, rect.Height);
    }
}
