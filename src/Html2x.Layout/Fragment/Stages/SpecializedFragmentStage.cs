using System.Drawing;
using Html2x.Core.Layout;

namespace Html2x.Layout.Fragment.Stages;

public sealed class SpecializedFragmentStage : IFragmentBuildStage
{
    public void Execute(FragmentBuildContext context)
    {
        foreach (var block in context.BoxTree.Blocks)
        {
            if (!context.Map.TryGetValue(block, out var frag) || frag is not BlockFragment blockFrag)
            {
                continue;
            }

            // Example: detect <hr> or <img>
            foreach (var child in block.Children)
            {
                var tag = child.Element?.TagName?.ToLowerInvariant();

                switch (tag)
                {
                    case "hr":
                    {
                        var rule = new RuleFragment
                        {
                            Rect = new RectangleF(block.X, block.Y + block.Height / 2, block.Width, 1),
                            Style = StyleConverter.FromComputed(child.Style)
                        };
                        blockFrag.Children.Add(rule);
                        break;
                    }
                    case "img":
                    {
                        var img = new ImageFragment
                        {
                            Rect = new RectangleF(block.X, block.Y, 100, 80),
                            Image = new ImageRef(child.Element?.GetAttribute("src") ?? ""),
                            ObjectFit = ObjectFit.Contain,
                            Align = Alignment.Center,
                            Style = StyleConverter.FromComputed(child.Style)
                        };
                        blockFrag.Children.Add(img);
                        break;
                    }
                }
            }
        }
    }
}