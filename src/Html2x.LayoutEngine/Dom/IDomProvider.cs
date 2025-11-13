using AngleSharp.Dom;

namespace Html2x.LayoutEngine.Dom;

public interface IDomProvider
{
    Task<IDocument> LoadAsync(string html);
}