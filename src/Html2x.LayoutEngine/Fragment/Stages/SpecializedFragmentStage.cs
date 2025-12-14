using System.Drawing;
using System.Globalization;
using Html2x.Abstractions.Images;
using Html2x.Abstractions.Layout.Fragments;
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

    private ImageFragment CreateImageFragment(
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

        var intrinsicW = loadResult.IntrinsicWidth > 0 ? loadResult.IntrinsicWidth : 0;
        var intrinsicH = loadResult.IntrinsicHeight > 0 ? loadResult.IntrinsicHeight : 0;

        var (width, height) = StyleConverter.ResolveImageSize(authoredWidth, authoredHeight, intrinsicW, intrinsicH);

        if (parent.Width > 0 && width > parent.Width)
        {
            var scale = parent.Width / width;
            width *= scale;
            height *= scale;
        }

        return new ImageFragment
        {
            FragmentId = state.ReserveFragmentId(),
            PageNumber = state.PageNumber,
            Src = src,
            AuthoredWidthPx = authoredWidth,
            AuthoredHeightPx = authoredHeight,
            IntrinsicWidthPx = width,
            IntrinsicHeightPx = height,
            IsMissing = loadResult.Status == ImageLoadStatus.Missing,
            IsOversize = loadResult.Status == ImageLoadStatus.Oversize,
            Rect = new RectangleF(parent.X, parent.Y, (float)width, (float)height),
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
