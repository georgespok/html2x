using Html2x.Abstractions.Layout;

using Html2x.Pdf.Options;
namespace Html2x.Pdf.Rendering;

public interface IFragmentRenderer
{
    void RenderBlock(BlockFragment fragment, Action<Fragment, IFragmentRenderer> renderChild);

    void RenderLine(LineBoxFragment fragment);

    void RenderImage(ImageFragment fragment);

    void RenderRule(RuleFragment fragment);
}


