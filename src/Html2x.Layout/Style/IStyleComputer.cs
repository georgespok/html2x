using AngleSharp.Dom;

namespace Html2x.Layout.Style;

public interface IStyleComputer
{
    StyleTree Compute(IDocument doc);
}