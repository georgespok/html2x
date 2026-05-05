using Html2x.LayoutEngine.Contracts.Published;
using Html2x.RenderModel.Fragments;
using LayoutFragment = Html2x.RenderModel.Fragments.Fragment;

namespace Html2x.LayoutEngine.Fragments;

internal sealed class PublishedLayoutToFragmentProjector
{
    internal const string MetadataOwnerName = nameof(FragmentBuilder);

    public BlockFragment CreateBlockFragment(PublishedBlock source, int fragmentId, int pageNumber)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Display.Role switch
        {
            FragmentDisplayRole.Table => CreateTableFragment(source, fragmentId, pageNumber),
            FragmentDisplayRole.TableRow => CreateTableRowFragment(source, fragmentId, pageNumber),
            FragmentDisplayRole.TableCell => CreateTableCellFragment(source, fragmentId, pageNumber),
            _ => CreateStandardBlockFragment(source, fragmentId, pageNumber)
        };
    }

    public LayoutFragment? CreateSpecialFragment(
        PublishedBlock source,
        int fragmentId,
        int pageNumber)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.Rule is not null)
        {
            return CreateRuleFragment(source, fragmentId, pageNumber);
        }

        if (source.Image is not null)
        {
            return CreateImageFragment(source, fragmentId, pageNumber);
        }

        return null;
    }

    private static TableFragment CreateTableFragment(PublishedBlock source, int fragmentId, int pageNumber)
    {
        return new TableFragment
        {
            FragmentId = fragmentId,
            PageNumber = pageNumber,
            Rect = source.Geometry.BorderBoxRect,
            Style = source.Style,
            DisplayRole = source.Display.Role,
            FormattingContext = source.Display.FormattingContext,
            MarkerOffset = source.Display.MarkerOffset,
            DerivedColumnCount = source.Table?.DerivedColumnCount ?? 0
        };
    }

    private static TableRowFragment CreateTableRowFragment(PublishedBlock source, int fragmentId, int pageNumber)
    {
        return new TableRowFragment
        {
            FragmentId = fragmentId,
            PageNumber = pageNumber,
            Rect = source.Geometry.BorderBoxRect,
            Style = source.Style,
            DisplayRole = source.Display.Role,
            FormattingContext = source.Display.FormattingContext,
            MarkerOffset = source.Display.MarkerOffset,
            RowIndex = source.Table?.RowIndex ?? 0
        };
    }

    private static TableCellFragment CreateTableCellFragment(PublishedBlock source, int fragmentId, int pageNumber)
    {
        return new TableCellFragment
        {
            FragmentId = fragmentId,
            PageNumber = pageNumber,
            Rect = source.Geometry.BorderBoxRect,
            Style = source.Style,
            DisplayRole = source.Display.Role,
            FormattingContext = source.Display.FormattingContext,
            MarkerOffset = source.Display.MarkerOffset,
            ColumnIndex = source.Table?.ColumnIndex ?? 0,
            IsHeader = source.Table?.IsHeader == true
        };
    }

    private static BlockFragment CreateStandardBlockFragment(PublishedBlock source, int fragmentId, int pageNumber)
    {
        return new BlockFragment
        {
            FragmentId = fragmentId,
            PageNumber = pageNumber,
            Rect = source.Geometry.BorderBoxRect,
            Style = source.Style,
            DisplayRole = source.Display.Role,
            FormattingContext = source.Display.FormattingContext,
            MarkerOffset = source.Display.MarkerOffset
        };
    }

    private static RuleFragment CreateRuleFragment(PublishedBlock source, int fragmentId, int pageNumber)
    {
        return new RuleFragment
        {
            FragmentId = fragmentId,
            PageNumber = pageNumber,
            Rect = source.Geometry.BorderBoxRect,
            Style = source.Style
        };
    }

    private static ImageFragment CreateImageFragment(PublishedBlock source, int fragmentId, int pageNumber)
    {
        var image = source.Image ?? throw new InvalidOperationException(
            "Image fragment projection requires published image facts.");

        return new ImageFragment
        {
            FragmentId = fragmentId,
            PageNumber = pageNumber,
            Src = image.Src,
            AuthoredSizePx = image.AuthoredSizePx,
            IntrinsicSizePx = image.IntrinsicSizePx,
            Status = image.Status,
            Rect = source.Geometry.BorderBoxRect,
            ContentRect = source.Geometry.ContentBoxRect,
            Style = source.Style
        };
    }
}
