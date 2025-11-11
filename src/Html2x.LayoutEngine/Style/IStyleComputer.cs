using AngleSharp.Dom;

namespace Html2x.LayoutEngine.Style;

public interface IStyleComputer
{
    StyleTree Compute(IDocument doc);
}