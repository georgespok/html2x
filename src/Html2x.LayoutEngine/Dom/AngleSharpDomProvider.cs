using System.Reflection;
using AngleSharp;
using AngleSharp.Css;
using AngleSharp.Dom;
using Html2x.Abstractions.Options;

namespace Html2x.LayoutEngine.Dom;

public sealed class AngleSharpDomProvider(IConfiguration config) : IDomProvider
{
    public async Task<IDocument> LoadAsync(string html, LayoutOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var context = BrowsingContext.New(config);
        ConfigureUserAgentStyleSheet(context, options);
        return await context.OpenAsync(req => req.Content(html));
    }

    private static void ConfigureUserAgentStyleSheet(IBrowsingContext context, LayoutOptions options)
    {
        var provider = context.GetService<ICssDefaultStyleSheetProvider>();
        if (provider is null)
        {
            throw new InvalidOperationException("ICssDefaultStyleSheetProvider is not available.");
        }

        if (!string.IsNullOrWhiteSpace(options.UserAgentStyleSheet))
        {
            provider.SetDefault(options.UserAgentStyleSheet!);
            return;
        }

        if (options.UseDefaultUserAgentStyleSheet)
        {
            provider.SetDefault(DefaultUserAgentStyleSheet.Load());
            return;
        }

        provider.SetDefault(string.Empty);
    }

    private static class DefaultUserAgentStyleSheet
    {
        private const string ResourceName = "Html2x.LayoutEngine.Resources.DefaultUserAgent.css";

        public static string Load()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(ResourceName);
            if (stream is null)
            {
                throw new InvalidOperationException("Embedded user agent stylesheet resource not found.");
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
