namespace Html2x.LayoutEngine.Box.Publishing;

using System.Text;
using Html2x.RenderModel;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Geometry.Published;
using Html2x.LayoutEngine.Models;

/// <summary>
/// Adapts resolved mutable box layout state into published layout facts.
/// </summary>
internal static class PublishedBlockFactory
{
    public static PublishedBlock CreateBlock(
        BlockBox source,
        PublishedBlockIdentity identity,
        UsedGeometry geometry,
        PublishedInlineLayout? inlineLayout,
        IReadOnlyList<PublishedBlock> children,
        IReadOnlyList<PublishedBlockFlowItem>? flow = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(identity);

        return new PublishedBlock(
            identity,
            CreateDisplay(source, geometry),
            source.Style,
            geometry,
            inlineLayout,
            CreateImage(source),
            CreateRule(source),
            CreateTable(source),
            children,
            flow);
    }

    public static PublishedBlockIdentity CreateIdentity(BoxNode source, int sourceOrder)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new PublishedBlockIdentity(
            BoxNodePathBuilder.Build(source),
            BuildElementIdentity(source),
            sourceOrder,
            source.SourceIdentity);
    }

    public static PublishedInlineSource CreateInlineSource(InlineBox source, int sourceOrder)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new PublishedInlineSource(
            BoxNodePathBuilder.Build(source),
            BuildElementIdentity(source),
            sourceOrder,
            source.SourceIdentity);
    }

    public static PublishedDisplayFacts CreateDisplay(BlockBox source, UsedGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new PublishedDisplayFacts(
            MapRole(source.Role),
            ResolveFormattingContext(source),
            geometry.MarkerOffset > 0f ? geometry.MarkerOffset : null);
    }

    public static PublishedImageFacts? CreateImage(BlockBox source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source is ImageBox image
            ? new PublishedImageFacts(
                image.Src,
                image.AuthoredSizePx,
                image.IntrinsicSizePx,
                image.IsMissing,
                image.IsOversize)
            : null;
    }

    public static PublishedRuleFacts? CreateRule(BlockBox source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source is RuleBox
            ? new PublishedRuleFacts()
            : null;
    }

    public static PublishedTableFacts? CreateTable(BlockBox source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.Role is not (BoxRole.Table or BoxRole.TableRow or BoxRole.TableCell))
        {
            return null;
        }

        return new PublishedTableFacts(
            ResolveDerivedColumnCount(source),
            ResolveRowIndex(source),
            ResolveColumnIndex(source),
            ResolveIsHeader(source));
    }

    private static string? BuildElementIdentity(BoxNode source)
    {
        if (!string.IsNullOrWhiteSpace(source.SourceIdentity.ElementIdentity))
        {
            return source.SourceIdentity.ElementIdentity;
        }

        var element = source.Element;
        if (element is null || string.IsNullOrWhiteSpace(element.TagName))
        {
            return null;
        }

        var builder = new StringBuilder(element.TagName.Trim().ToLowerInvariant());
        var id = element.GetAttribute(HtmlCssConstants.HtmlAttributes.Id);
        if (!string.IsNullOrWhiteSpace(id))
        {
            builder.Append('#');
            builder.Append(id.Trim());
        }

        var classAttribute = element.GetAttribute(HtmlCssConstants.HtmlAttributes.Class);
        if (!string.IsNullOrWhiteSpace(classAttribute))
        {
            foreach (var className in classAttribute.Split(
                [' ', '\t', '\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                builder.Append('.');
                builder.Append(className);
            }
        }

        return builder.ToString();
    }

    private static FormattingContextKind ResolveFormattingContext(BlockBox source)
    {
        return source.IsInlineBlockContext
            ? FormattingContextKind.InlineBlock
            : FormattingContextKind.Block;
    }

    private static int? ResolveDerivedColumnCount(BlockBox source)
    {
        if (source.Role != BoxRole.Table)
        {
            return null;
        }

        if (source is TableBox tableBox && tableBox.DerivedColumnCount >= 0)
        {
            return tableBox.DerivedColumnCount;
        }

        return source.Children
            .OfType<BlockBox>()
            .Select(static row => row.Children.OfType<BlockBox>().Count())
            .DefaultIfEmpty()
            .Max();
    }

    private static int? ResolveRowIndex(BlockBox source)
    {
        if (source.Role != BoxRole.TableRow)
        {
            return null;
        }

        return source is TableRowBox rowBox && rowBox.RowIndex >= 0
            ? rowBox.RowIndex
            : ResolveSiblingIndex(source, BoxRole.TableRow);
    }

    private static int? ResolveColumnIndex(BlockBox source)
    {
        if (source.Role != BoxRole.TableCell)
        {
            return null;
        }

        return source is TableCellBox cellBox && cellBox.ColumnIndex >= 0
            ? cellBox.ColumnIndex
            : ResolveSiblingIndex(source, BoxRole.TableCell);
    }

    private static bool? ResolveIsHeader(BlockBox source)
    {
        if (source.Role != BoxRole.TableCell)
        {
            return null;
        }

        return (source as TableCellBox)?.IsHeader == true
               || string.Equals(source.Element?.TagName, "th", StringComparison.OrdinalIgnoreCase);
    }

    private static int ResolveSiblingIndex(BlockBox source, BoxRole role)
    {
        if (source.Parent is null)
        {
            return 0;
        }

        return source.Parent.Children
            .TakeWhile(child => !ReferenceEquals(child, source))
            .Count(child => child is BlockBox sibling && sibling.Role == role);
    }

    private static FragmentDisplayRole MapRole(BoxRole role)
    {
        return role switch
        {
            BoxRole.Block => FragmentDisplayRole.Block,
            BoxRole.Inline => FragmentDisplayRole.Inline,
            BoxRole.InlineBlock => FragmentDisplayRole.InlineBlock,
            BoxRole.ListItem => FragmentDisplayRole.ListItem,
            BoxRole.Table => FragmentDisplayRole.Table,
            BoxRole.TableRow => FragmentDisplayRole.TableRow,
            BoxRole.TableCell => FragmentDisplayRole.TableCell,
            _ => FragmentDisplayRole.Block
        };
    }
}
