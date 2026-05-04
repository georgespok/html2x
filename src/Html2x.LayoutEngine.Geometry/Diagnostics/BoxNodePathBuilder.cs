using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Diagnostics;

internal static class BoxNodePathBuilder
{
    public static string Build(BoxNode node)
    {
        var segments = new Stack<string>();
        BoxNode? current = node;

        while (current is not null)
        {
            var tag = current.Element?.TagName?.ToLowerInvariant() ?? current.Role.ToString().ToLowerInvariant();
            segments.Push(tag);
            current = current.Parent;
        }

        return string.Join("/", segments);
    }
}
