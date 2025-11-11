using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Html2x.Core.Layout;
using Html2x.LayoutEngine.Box;

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
            var tag = child.Element?.TagName?.ToLowerInvariant();

            switch (tag)
            {
                case HtmlCssConstants.HtmlTags.Hr:
                {
                    var rule = new RuleFragment
                    {
                        Rect = new RectangleF(blockBox.X, blockBox.Y + blockBox.Height / 2, blockBox.Width, 1),
                        Style = StyleConverter.FromComputed(child.Style)
                    };
                    blockFragment.Children.Add(rule);
                    NotifySpecial(child, rule, observers);
                    break;
                }
                case HtmlCssConstants.HtmlTags.Img:
                {
                    var img = new ImageFragment
                    {
                        Rect = new RectangleF(blockBox.X, blockBox.Y, 100, 80),
                        Image = new ImageRef(child.Element?.GetAttribute(HtmlCssConstants.HtmlAttributes.Src) ?? string.Empty),
                        ObjectFit = ObjectFit.Contain,
                        Align = Alignment.Center,
                        Style = StyleConverter.FromComputed(child.Style)
                    };
                    blockFragment.Children.Add(img);
                    NotifySpecial(child, img, observers);
                    break;
                }
            }

            if (child is BlockBox nested && lookup.TryGetValue(nested, out var nestedFragment))
            {
                AppendSpecializedFragments(nested, nestedFragment, lookup, observers);
            }
        }
    }

    private static void NotifySpecial(DisplayNode source, Core.Layout.Fragment fragment,
        IReadOnlyList<IFragmentBuildObserver> observers)
    {
        foreach (var observer in observers)
        {
            observer.OnSpecialFragmentCreated(source, fragment);
        }
    }
}
