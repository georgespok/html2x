using Html2x.Core.Layout;

namespace Html2x.Pdf;

internal sealed class FragmentRenderDispatcher(IFragmentRenderer renderer) : IFragmentVisitor
{
    public void Visit(BlockFragment fragment)
    {
        renderer.RenderBlock(fragment, (child, childRenderer) =>
        {
            child.VisitWith(new FragmentRenderDispatcher(childRenderer));
        });
    }

    public void Visit(LineBoxFragment fragment)
    {
        renderer.RenderLine(fragment);
    }

    public void Visit(ImageFragment fragment)
    {
        renderer.RenderImage(fragment);
    }

    public void Visit(RuleFragment fragment)
    {
        renderer.RenderRule(fragment);
    }
}
