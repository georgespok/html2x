using AngleSharp;
using AngleSharp.Dom;

namespace Html2x.LayoutEngine.Dom;

public sealed class AngleSharpDomProvider(IConfiguration config) : IDomProvider
{
    public async Task<IDocument> LoadAsync(string html)
    {
        var context = BrowsingContext.New(config);
        return await context.OpenAsync(req => req.Content(html));
    }
}