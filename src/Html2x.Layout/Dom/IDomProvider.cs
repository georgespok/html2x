using AngleSharp.Dom;

namespace Html2x.Layout.Dom;

public interface IDomProvider
{
    Task<IDocument> LoadAsync(string html);
}