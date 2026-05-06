namespace Html2x.LayoutEngine.Geometry.Diagnostics;

internal static class BoxNodePath
{
    public static string Build(BoxNode node)
    {
        var segments = new Stack<string>();
        var current = node;

        while (current is not null)
        {
            var tag = current.Element?.TagName?.ToLowerInvariant() ?? current.Role.ToString().ToLowerInvariant();
            segments.Push(tag);
            current = current.Parent;
        }

        return string.Join("/", segments);
    }
}