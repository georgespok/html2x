using AngleSharp.Dom;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Style;

public interface IStyleComputer
{
    StyleTree Compute(IDocument doc);
}