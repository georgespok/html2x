using System.Drawing;
using System.Globalization;
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
            AppendSpecializedFragments(binding.Source, binding.Fragment, lookup, state.Observers);
        }

        return state;
    }

    private static void AppendSpecializedFragments(BlockBox blockBox, BlockFragment blockFragment,
        IReadOnlyDictionary<BlockBox, BlockFragment> lookup, IReadOnlyList<IFragmentBuildObserver> observers)
    {
        foreach (var child in blockBox.Children)
        {
            var fragment = CreateSpecialFragment(child, blockBox);

            if (fragment is not null)
            {
                blockFragment.Children.Add(fragment);
                NotifySpecial(child, fragment, observers);
            }

            if (child is BlockBox nested && lookup.TryGetValue(nested, out var nestedFragment))
            {
                AppendSpecializedFragments(nested, nestedFragment, lookup, observers);
            }
        }
    }

    private static Abstractions.Layout.Fragments.Fragment? CreateSpecialFragment(DisplayNode child, BlockBox parent)
    {
        var tag = child.Element?.TagName?.ToLowerInvariant();

        return tag switch
        {
            HtmlCssConstants.HtmlTags.Hr => CreateRuleFragment(parent),
            HtmlCssConstants.HtmlTags.Img => CreateImageFragment(child, parent),
            _ => null
        };
    }

    private static RuleFragment CreateRuleFragment(BlockBox box) =>
        new()
        {
            Rect = new RectangleF(box.X, box.Y + box.Height / 2, box.Width, 1),
            Style = StyleConverter.FromComputed(box.Style)
        };

    private static ImageFragment CreateImageFragment(DisplayNode child, BlockBox parent)
    {
        var el = child.Element;
        var src = el?.GetAttribute(HtmlCssConstants.HtmlAttributes.Src) ?? string.Empty;
        var authoredWidth = ParsePxAttr(el, HtmlCssConstants.HtmlAttributes.Width);
        var authoredHeight = ParsePxAttr(el, HtmlCssConstants.HtmlAttributes.Height);

        return new ImageFragment
        {
            Src = src,
            AuthoredWidthPx = authoredWidth,
            AuthoredHeightPx = authoredHeight,
            IntrinsicWidthPx = authoredWidth ?? 0,
            IntrinsicHeightPx = authoredHeight ?? 0,
            IsMissing = false,
            IsOversize = false,
            // Sizing will be refined later in box/fragment layout; keep placeholder rect for now.
            Rect = new RectangleF(parent.X, parent.Y, 0, 0),
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
