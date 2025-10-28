using System.Drawing;
using Html2x.Core.Layout;

namespace Html2x.Layout.Fragment.Stages;

public sealed class BlockFragmentStage : IFragmentBuildStage
{
    public void Execute(FragmentBuildContext context)
    {
        foreach (var block in context.BoxTree.Blocks)
        {
            var fragment = new BlockFragment
            {
                Rect = new RectangleF(block.X, block.Y, block.Width, block.Height),
                Style = StyleConverter.FromComputed(block.Style)
            };

            context.Result.Blocks.Add(fragment);
            context.Map[block] = fragment;
        }
    }
}