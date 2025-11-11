using Html2x.Abstractions.Layout;
using Html2x.Pdf.Rendering;
using Html2x.Pdf.Visitors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Html2x.Renderers.Pdf.Pipeline;

internal sealed class FragmentRenderDispatcher(
    IFragmentRenderer renderer,
    ILogger<FragmentRenderDispatcher>? logger = null)
    : IFragmentVisitor
{
    private readonly IFragmentRenderer _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    private readonly ILogger<FragmentRenderDispatcher> _logger = logger ?? NullLogger<FragmentRenderDispatcher>.Instance;

    public void Visit(BlockFragment fragment)
    {
        _renderer.RenderBlock(fragment, (child, childRenderer) =>
        {
            child.VisitWith(new FragmentRenderDispatcher(childRenderer, _logger));
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


