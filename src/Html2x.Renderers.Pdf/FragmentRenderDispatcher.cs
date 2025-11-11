using Html2x.Core.Layout;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Html2x.Pdf;

internal sealed class FragmentRenderDispatcher : IFragmentVisitor
{
    private readonly IFragmentRenderer _renderer;
    private readonly ILogger<FragmentRenderDispatcher> _logger;

    public FragmentRenderDispatcher(
        IFragmentRenderer renderer,
        ILogger<FragmentRenderDispatcher>? logger = null)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? NullLogger<FragmentRenderDispatcher>.Instance;
    }

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
