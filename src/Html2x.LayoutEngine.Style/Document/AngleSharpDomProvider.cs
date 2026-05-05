using System.Reflection;
using AngleSharp;
using AngleSharp.Css;
using AngleSharp.Dom;

namespace Html2x.LayoutEngine.Style.Document;

internal sealed class AngleSharpDomProvider(IConfiguration config)
{
    public async Task<IDocument> LoadAsync(string html, StyleBuildSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var context = BrowsingContext.New(config);
        ConfigureUserAgentStyleSheet(context, settings);
        return await context.OpenAsync(req => req.Content(html));
    }

    private static void ConfigureUserAgentStyleSheet(IBrowsingContext context, StyleBuildSettings settings)
    {
        var provider = context.GetService<ICssDefaultStyleSheetProvider>();
        if (provider is null)
        {
            throw new InvalidOperationException("ICssDefaultStyleSheetProvider is not available.");
        }

        if (!string.IsNullOrWhiteSpace(settings.UserAgentStyleSheet))
        {
            provider.SetDefault(settings.UserAgentStyleSheet!);
            return;
        }

        if (settings.UseDefaultUserAgentStyleSheet)
        {
            provider.SetDefault(DefaultUserAgentStyleSheet.Load());
            return;
        }

        provider.SetDefault(string.Empty);
    }

    private static class DefaultUserAgentStyleSheet
    {
        private const string ResourceName = "Html2x.LayoutEngine.Style.Resources.DefaultUserAgent.css";

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
