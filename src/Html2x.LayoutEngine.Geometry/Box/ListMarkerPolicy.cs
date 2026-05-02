using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

internal static class ListMarkerPolicy
{
    public static InlineBox? CreateMarker(BoxNode listContainer, BlockBox listItem)
    {
        ArgumentNullException.ThrowIfNull(listContainer);
        ArgumentNullException.ThrowIfNull(listItem);

        var markerText = ResolveMarkerText(listContainer, listItem);
        if (string.IsNullOrWhiteSpace(markerText))
        {
            return null;
        }

        return CreateMarkerRun(markerText, listItem);
    }

    public static InlineBox? CreateSyntheticMarker(BlockBox listItem)
    {
        ArgumentNullException.ThrowIfNull(listItem);

        if (listItem.Role != BoxRole.ListItem ||
            listItem.MarkerOffset > 0f ||
            HasExplicitMarker(listItem))
        {
            return null;
        }

        var listContainer = FindNearestListContainer(listItem.Parent);
        return listContainer is null
            ? null
            : CreateMarker(listContainer, listItem);
    }

    private static string ResolveMarkerText(BoxNode listContainer, BoxNode listItem)
    {
        var tag = listContainer.Element?.TagName;
        if (string.Equals(tag, HtmlCssConstants.HtmlTags.Ul, StringComparison.OrdinalIgnoreCase))
        {
            return "\u2022 ";
        }

        if (string.Equals(tag, HtmlCssConstants.HtmlTags.Ol, StringComparison.OrdinalIgnoreCase))
        {
            var ordinal = ResolveOrderedListOrdinal(listContainer, listItem);
            return $"{ordinal}. ";
        }

        return string.Empty;
    }

    private static int ResolveOrderedListOrdinal(BoxNode listContainer, BoxNode listItem)
    {
        var listItems = listContainer.Children
            .Where(static child => child.Role == BoxRole.ListItem)
            .ToList();

        var existingIndex = listItems.FindIndex(child => ReferenceEquals(child, listItem));
        if (existingIndex >= 0)
        {
            return existingIndex + 1;
        }

        return listItems.Count + 1;
    }

    private static InlineBox CreateMarkerRun(string markerText, BlockBox listItem)
    {
        return new InlineBox(BoxRole.Inline)
        {
            TextContent = markerText,
            Style = listItem.Style,
            Parent = listItem,
            SourceIdentity = GeometrySourceIdentity
                .FirstSpecified(listItem.SourceIdentity, listItem.Parent?.SourceIdentity ?? GeometrySourceIdentity.Unspecified)
                .AsGenerated(GeometryGeneratedSourceKind.ListMarker)
        };
    }

    private static bool HasExplicitMarker(BlockBox listItem)
    {
        foreach (var inline in listItem.Children.OfType<InlineBox>())
        {
            var text = inline.TextContent?.TrimStart();
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            if (text.StartsWith("\u2022", StringComparison.Ordinal) ||
                (char.IsDigit(text[0]) && text.Contains('.')))
            {
                return true;
            }
        }

        return false;
    }

    private static BoxNode? FindNearestListContainer(BoxNode? node)
    {
        var current = node;
        while (current is not null)
        {
            if (HtmlElementClassifier.IsListContainer(current.Element))
            {
                return current;
            }

            current = current.Parent;
        }

        return null;
    }
}
