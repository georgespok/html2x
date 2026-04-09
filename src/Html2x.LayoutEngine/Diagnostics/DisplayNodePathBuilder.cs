using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Diagnostics;

internal static class DisplayNodePathBuilder
{
    public static string Build(DisplayNode node)
    {
        var segments = new Stack<string>();
        DisplayNode? current = node;

        while (current is not null)
        {
            var tag = current.Element?.TagName?.ToLowerInvariant() ?? current.Role.ToString().ToLowerInvariant();
            segments.Push(tag);
            current = current.Parent;
        }

        return string.Join("/", segments);
    }
}
