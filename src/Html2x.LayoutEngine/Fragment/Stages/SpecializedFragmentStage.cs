using System.Drawing;
using System.Globalization;
using Html2x.Abstractions.Images;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Fragment.Stages;

public sealed class SpecializedFragmentStage : IFragmentBuildStage
{
    public FragmentBuildState Execute(FragmentBuildState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (state.BlockBindings.Count == 0)
        {
            return state;
        }

        var lookup = state.BlockBindings.ToDictionary(b => b.Source, b => b.Fragment);

        foreach (var binding in state.BlockBindings)
        {
            AppendSpecializedFragments(state, binding.Source, binding.Fragment, lookup, state.Observers, state.Context);
        }

        return state;
    }

    private void AppendSpecializedFragments(
        FragmentBuildState state,
        BlockBox blockBox,
        BlockFragment blockFragment,
        IReadOnlyDictionary<BlockBox, BlockFragment> lookup,
        IReadOnlyList<IFragmentBuildObserver> observers,
        FragmentBuildContext context)
    {
        foreach (var child in blockBox.Children)
        {
            var fragment = CreateSpecialFragment(state, child, blockBox, context);

            if (fragment is not null)
            {
                blockFragment.Children.Add(fragment);
                NotifySpecial(child, fragment, observers);
            }

            if (child is BlockBox nested && lookup.TryGetValue(nested, out var nestedFragment))
            {
                AppendSpecializedFragments(state, nested, nestedFragment, lookup, observers, context);
            }
        }
    }

    private Abstractions.Layout.Fragments.Fragment? CreateSpecialFragment(
        FragmentBuildState state,
        DisplayNode child,
        BlockBox parent,
        FragmentBuildContext context)
    {
        var tag = child.Element?.TagName?.ToLowerInvariant();

        return tag switch
        {
            HtmlCssConstants.HtmlTags.Hr => CreateRuleFragment(state, parent),
            HtmlCssConstants.HtmlTags.Img => CreateImageFragment(state, child, parent, context),
            _ => null
        };
    }

    private static RuleFragment CreateRuleFragment(FragmentBuildState state, BlockBox box) =>
        new()
        {
            FragmentId = state.ReserveFragmentId(),
            PageNumber = state.PageNumber,
            Rect = new RectangleF(box.X, box.Y + box.Height / 2, box.Width, 1),
            Style = StyleConverter.FromComputed(box.Style)
        };

    private static ImageFragment CreateImageFragment(
        FragmentBuildState state,
        DisplayNode child,
        BlockBox parent,
        FragmentBuildContext context)
    {
        var el = child.Element;
        var src = el?.GetAttribute(HtmlCssConstants.HtmlAttributes.Src) ?? string.Empty;
        var authoredWidth = ParsePxAttr(el, HtmlCssConstants.HtmlAttributes.Width);
        var authoredHeight = ParsePxAttr(el, HtmlCssConstants.HtmlAttributes.Height);
        var loadResult = context.ImageProvider.Load(src, context.HtmlDirectory, context.MaxImageSizeBytes);

        var authoredSize = new SizePx(authoredWidth, authoredHeight);
        var sizePx = StyleConverter.ResolveImageSize(authoredSize, loadResult.IntrinsicSizePx)
            .ClampMin(0d, 0d);

        var size = new SizePt((float)sizePx.WidthOrZero, (float)sizePx.HeightOrZero)
            .Safe()
            .ClampMin(0f, 0f);

        if (parent.Width > 0 && size.Width > parent.Width)
        {
            var scale = parent.Width / size.Width;
            size = size.Scale(scale);
        }

        var padding = child.Style.Padding.Safe();
        var border = Spacing.FromBorderEdges(child.Style.Borders).Safe();

        var contentSize = size.ClampMin(0f, 0f);
        var outerSize = contentSize
            .Inflate(padding.Horizontal + border.Horizontal, padding.Vertical + border.Vertical)
            .ClampMin(0f, 0f);

        var outerRect = new RectangleF(parent.X, parent.Y, outerSize.Width, outerSize.Height);
        var contentRect = padding.Add(border).Inset(outerRect);

        return new ImageFragment
        {
            FragmentId = state.ReserveFragmentId(),
            PageNumber = state.PageNumber,
            Src = src,
            AuthoredSizePx = authoredSize,
            IntrinsicSizePx = new SizePx(contentSize.Width, contentSize.Height),
            IsMissing = loadResult.Status == ImageLoadStatus.Missing,
            IsOversize = loadResult.Status == ImageLoadStatus.Oversize,
            Rect = outerRect,
            ContentRect = contentRect,
            Style = StyleConverter.FromComputed(child.Style)
        };
    }

    private static void NotifySpecial(DisplayNode source, Abstractions.Layout.Fragments.Fragment fragment,
        IReadOnlyList<IFragmentBuildObserver> observers)
    {
        foreach (var observer in observers)
        {
            observer.OnSpecialFragmentCreated(source, fragment);
        }
    }

    private static double? ParsePxAttr(AngleSharp.Dom.IElement? el, string attr)
    {
        var value = el?.GetAttribute(attr);
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
        {
            return d;
        }
        return null;
    }

}
