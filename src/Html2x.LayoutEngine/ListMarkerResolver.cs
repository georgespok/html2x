using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine;

internal static class ListMarkerResolver
{
    public static string ResolveMarkerText(DisplayNode listContainer, DisplayNode listItem)
    {
        var tag = listContainer.Element?.TagName;
        if (string.Equals(tag, HtmlCssConstants.HtmlTags.Ul, StringComparison.OrdinalIgnoreCase))
        {
            return "• ";
        }

        if (string.Equals(tag, HtmlCssConstants.HtmlTags.Ol, StringComparison.OrdinalIgnoreCase))
        {
            var ordinal = ResolveOrderedListOrdinal(listContainer, listItem);
            return $"{ordinal}. ";
        }

        return string.Empty;
    }

    private static int ResolveOrderedListOrdinal(DisplayNode listContainer, DisplayNode listItem)
    {
        var listItems = listContainer.Children
            .Where(static child => child.Role == DisplayRole.ListItem)
            .ToList();

        var existingIndex = listItems.FindIndex(child => ReferenceEquals(child, listItem));
        if (existingIndex >= 0)
        {
            return existingIndex + 1;
        }

        return listItems.Count + 1;
    }
}
