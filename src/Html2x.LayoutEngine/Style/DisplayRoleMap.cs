using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Style;

internal static class DisplayRoleMap
{
    private static readonly IReadOnlyDictionary<string, DisplayRole> DisplayTokens =
        new Dictionary<string, DisplayRole>(StringComparer.OrdinalIgnoreCase)
        {
            [HtmlCssConstants.CssValues.Block] = DisplayRole.Block,
            [HtmlCssConstants.CssValues.Inline] = DisplayRole.Inline,
            [HtmlCssConstants.CssValues.InlineBlock] = DisplayRole.InlineBlock,
            [HtmlCssConstants.CssValues.ListItem] = DisplayRole.ListItem,
            [HtmlCssConstants.CssValues.Table] = DisplayRole.Table,
            [HtmlCssConstants.CssValues.TableRow] = DisplayRole.TableRow,
            [HtmlCssConstants.CssValues.TableCell] = DisplayRole.TableCell
        };

    private static readonly IReadOnlyDictionary<string, DisplayRole> DefaultRoles = new Dictionary<string, DisplayRole>(StringComparer.OrdinalIgnoreCase)
    {
        [HtmlCssConstants.HtmlTags.Div] = DisplayRole.Block,
        [HtmlCssConstants.HtmlTags.Section] = DisplayRole.Block,
        [HtmlCssConstants.HtmlTags.Main] = DisplayRole.Block,
        [HtmlCssConstants.HtmlTags.Header] = DisplayRole.Block,
        [HtmlCssConstants.HtmlTags.Footer] = DisplayRole.Block,
        [HtmlCssConstants.HtmlTags.P] = DisplayRole.Block,
        [HtmlCssConstants.HtmlTags.Body] = DisplayRole.Block,
        [HtmlCssConstants.HtmlTags.Ul] = DisplayRole.Block,
        [HtmlCssConstants.HtmlTags.Ol] = DisplayRole.Block,
        [HtmlCssConstants.HtmlTags.Li] = DisplayRole.ListItem,
        [HtmlCssConstants.HtmlTags.Table] = DisplayRole.Table,
        [HtmlCssConstants.HtmlTags.Tr] = DisplayRole.TableRow,
        [HtmlCssConstants.HtmlTags.Td] = DisplayRole.TableCell,
        [HtmlCssConstants.HtmlTags.Th] = DisplayRole.TableCell,
        [HtmlCssConstants.HtmlTags.Img] = DisplayRole.InlineBlock,
        [HtmlCssConstants.HtmlTags.Span] = DisplayRole.Inline,
        [HtmlCssConstants.HtmlTags.B] = DisplayRole.Inline,
        [HtmlCssConstants.HtmlTags.I] = DisplayRole.Inline,
        [HtmlCssConstants.HtmlTags.Strong] = DisplayRole.Inline,
        [HtmlCssConstants.HtmlTags.U] = DisplayRole.Inline,
        [HtmlCssConstants.HtmlTags.S] = DisplayRole.Inline,
        [HtmlCssConstants.HtmlTags.H1] = DisplayRole.Block,
        [HtmlCssConstants.HtmlTags.H2] = DisplayRole.Block,
        [HtmlCssConstants.HtmlTags.H3] = DisplayRole.Block,
        [HtmlCssConstants.HtmlTags.H4] = DisplayRole.Block,
        [HtmlCssConstants.HtmlTags.H5] = DisplayRole.Block,
        [HtmlCssConstants.HtmlTags.H6] = DisplayRole.Block,
        [HtmlCssConstants.HtmlTags.Hr] = DisplayRole.Block,
        [HtmlCssConstants.HtmlTags.Br] = DisplayRole.Inline
    };

    public static DisplayRole Resolve(string? display, string? tagName)
    {
        if (!string.IsNullOrWhiteSpace(display) &&
            DisplayTokens.TryGetValue(display.Trim(), out var displayRole))
        {
            return displayRole;
        }

        if (string.IsNullOrWhiteSpace(tagName))
        {
            return DisplayRole.Inline;
        }

        return DefaultRoles.GetValueOrDefault(tagName, DisplayRole.Inline);
    }
}
