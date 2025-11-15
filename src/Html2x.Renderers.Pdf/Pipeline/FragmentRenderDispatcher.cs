using Html2x.Abstractions.Layout.Fragments;
using Html2x.Renderers.Pdf.Rendering;
using Html2x.Renderers.Pdf.Visitors;

namespace Html2x.Renderers.Pdf.Pipeline;

internal sealed class FragmentRenderDispatcher(
    IFragmentRenderer renderer)
    : IFragmentVisitor
{
    private readonly IFragmentRenderer _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));

    public void Visit(BlockFragment fragment)
    {
        _renderer.RenderBlock(fragment, (child, childRenderer) =>
        {
            child.VisitWith(new FragmentRenderDispatcher(childRenderer));
        });
    }

    public void Visit(LineBoxFragment fragment)
    {
        _renderer.RenderLine(fragment);
    }

    public void Visit(ImageFragment fragment)
    {
        _renderer.RenderImage(fragment);
    }

    public void Visit(RuleFragment fragment)
    {
        _renderer.RenderRule(fragment);
    }
}
