using System.Drawing;
using Html2x.Core.Layout;
using Html2x.Layout.Box;

namespace Html2x.Layout.Fragment.Stages;

public sealed class InlineFragmentStage : IFragmentBuildStage
{
    private readonly TextRunFactory _textRunFactory = new();

    public void Execute(FragmentBuildContext context)
    {
        foreach (var block in context.BoxTree.Blocks)
        {
            if (!context.Map.TryGetValue(block, out var frag) || frag is not BlockFragment blockFragment)
            {
                continue;
            }

            foreach (var inline in block.Children.OfType<InlineBox>())
            {
                if (string.IsNullOrWhiteSpace(inline.TextContent))
                {
                    continue;
                }

                var run = _textRunFactory.Create(inline);
                var line = new LineBoxFragment
                {
                    Rect = new RectangleF(block.X, block.Y, run.AdvanceWidth, run.Ascent + run.Descent),
                    BaselineY = block.Y + run.Ascent,
                    LineHeight = run.Ascent + run.Descent,
                    Runs = [run]
                };

                blockFragment.Children.Add(line);
                context.Map[inline] = line;
            }
        }
    }
}