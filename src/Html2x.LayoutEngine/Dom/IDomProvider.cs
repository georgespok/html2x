using AngleSharp.Dom;
using Html2x.Abstractions.Options;

namespace Html2x.LayoutEngine.Dom;

public interface IDomProvider
{
    Task<IDocument> LoadAsync(string html, LayoutOptions options);
}
