using System.Text;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry.Diagnostics;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Publishing;

/// <summary>
///     Adapts resolved mutable box layout state into published layout facts.
/// </summary>
internal static class PublishedBlockFacts
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

        return new(
            identity,
            CreateDisplay(source, geometry),
            CreateStyle(source.Style),
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

        return new(
            BoxNodePath.Build(source),
            BuildElementIdentity(source),
            sourceOrder,
            source.SourceIdentity);
    }

    public static PublishedInlineSource CreateInlineSource(InlineBox source, int sourceOrder)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new(
            BoxNodePath.Build(source),
            BuildElementIdentity(source),
            sourceOrder,
            source.SourceIdentity);
    }

    public static PublishedDisplayFacts CreateDisplay(BlockBox source, UsedGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new(
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
                image.Status)
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

        return new(
            ResolveDerivedColumnCount(source),
            ResolveRowIndex(source),
            ResolveColumnIndex(source),
            ResolveIsHeader(source),
            ResolveColumnSpan(source));
    }

    private static VisualStyle CreateStyle(ComputedStyle style)
    {
        var hasBorders = style.Borders?.HasAny == true;

        return new(
            style.BackgroundColor,
            hasBorders ? style.Borders : null,
            style.Color,
            style.Margin,
            style.Padding,
            style.WidthPt,
            style.HeightPt,
            style.Display);
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
        var id = element.GetAttribute(HtmlCssVocabulary.HtmlAttributes.Id);
        if (!string.IsNullOrWhiteSpace(id))
        {
            builder.Append('#');
            builder.Append(id.Trim());
        }

        var classAttribute = element.GetAttribute(HtmlCssVocabulary.HtmlAttributes.Class);
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

    private static FormattingContextKind ResolveFormattingContext(BlockBox source) =>
        source.EstablishesInlineBlockFormattingContext
            ? FormattingContextKind.InlineBlock
            : FormattingContextKind.Block;

    private static int? ResolveDerivedColumnCount(BlockBox source)
    {
        return source is TableBox { DerivedColumnCount: >= 0 } tableBox
            ? tableBox.DerivedColumnCount
            : null;
    }

    private static int? ResolveRowIndex(BlockBox source)
    {
        return source is TableRowBox rowBox && rowBox.RowIndex >= 0
            ? rowBox.RowIndex
            : null;
    }

    private static int? ResolveColumnIndex(BlockBox source)
    {
        return source is TableCellBox cellBox && cellBox.ColumnIndex >= 0
            ? cellBox.ColumnIndex
            : null;
    }

    private static bool? ResolveIsHeader(BlockBox source)
    {
        return source is TableCellBox cellBox
            ? cellBox.IsHeader
            : null;
    }

    private static int ResolveColumnSpan(BlockBox source)
    {
        if (source.Role != BoxRole.TableCell)
        {
            return 1;
        }

        return source is TableCellBox { ColumnSpan: > 0 } cellBox
            ? cellBox.ColumnSpan
            : 1;
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
