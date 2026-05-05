namespace Html2x.LayoutEngine.Geometry.Style;

internal static class BoxRoleMap
{
    private static readonly IReadOnlyDictionary<string, BoxRole> DisplayTokens =
        new Dictionary<string, BoxRole>(StringComparer.OrdinalIgnoreCase)
        {
            [HtmlCssConstants.CssValues.Block] = BoxRole.Block,
            [HtmlCssConstants.CssValues.Inline] = BoxRole.Inline,
            [HtmlCssConstants.CssValues.InlineBlock] = BoxRole.InlineBlock,
            [HtmlCssConstants.CssValues.ListItem] = BoxRole.ListItem,
            [HtmlCssConstants.CssValues.Table] = BoxRole.Table,
            [HtmlCssConstants.CssValues.TableRowGroup] = BoxRole.TableSection,
            [HtmlCssConstants.CssValues.TableHeaderGroup] = BoxRole.TableSection,
            [HtmlCssConstants.CssValues.TableFooterGroup] = BoxRole.TableSection,
            [HtmlCssConstants.CssValues.TableRow] = BoxRole.TableRow,
            [HtmlCssConstants.CssValues.TableCell] = BoxRole.TableCell
        };

    private static readonly IReadOnlyDictionary<string, BoxRole> DefaultRoles = new Dictionary<string, BoxRole>(StringComparer.OrdinalIgnoreCase)
    {
        [HtmlCssConstants.HtmlTags.Div] = BoxRole.Block,
        [HtmlCssConstants.HtmlTags.Section] = BoxRole.Block,
        [HtmlCssConstants.HtmlTags.Main] = BoxRole.Block,
        [HtmlCssConstants.HtmlTags.Header] = BoxRole.Block,
        [HtmlCssConstants.HtmlTags.Footer] = BoxRole.Block,
        [HtmlCssConstants.HtmlTags.P] = BoxRole.Block,
        [HtmlCssConstants.HtmlTags.Body] = BoxRole.Block,
        [HtmlCssConstants.HtmlTags.Ul] = BoxRole.Block,
        [HtmlCssConstants.HtmlTags.Ol] = BoxRole.Block,
        [HtmlCssConstants.HtmlTags.Li] = BoxRole.ListItem,
        [HtmlCssConstants.HtmlTags.Table] = BoxRole.Table,
        [HtmlCssConstants.HtmlTags.Tbody] = BoxRole.TableSection,
        [HtmlCssConstants.HtmlTags.Thead] = BoxRole.TableSection,
        [HtmlCssConstants.HtmlTags.Tfoot] = BoxRole.TableSection,
        [HtmlCssConstants.HtmlTags.Tr] = BoxRole.TableRow,
        [HtmlCssConstants.HtmlTags.Td] = BoxRole.TableCell,
        [HtmlCssConstants.HtmlTags.Th] = BoxRole.TableCell,
        [HtmlCssConstants.HtmlTags.Img] = BoxRole.InlineBlock,
        [HtmlCssConstants.HtmlTags.Span] = BoxRole.Inline,
        [HtmlCssConstants.HtmlTags.B] = BoxRole.Inline,
        [HtmlCssConstants.HtmlTags.I] = BoxRole.Inline,
        [HtmlCssConstants.HtmlTags.Strong] = BoxRole.Inline,
        [HtmlCssConstants.HtmlTags.U] = BoxRole.Inline,
        [HtmlCssConstants.HtmlTags.S] = BoxRole.Inline,
        [HtmlCssConstants.HtmlTags.H1] = BoxRole.Block,
        [HtmlCssConstants.HtmlTags.H2] = BoxRole.Block,
        [HtmlCssConstants.HtmlTags.H3] = BoxRole.Block,
        [HtmlCssConstants.HtmlTags.H4] = BoxRole.Block,
        [HtmlCssConstants.HtmlTags.H5] = BoxRole.Block,
        [HtmlCssConstants.HtmlTags.H6] = BoxRole.Block,
        [HtmlCssConstants.HtmlTags.Hr] = BoxRole.Block,
        [HtmlCssConstants.HtmlTags.Br] = BoxRole.Inline
    };

    public static BoxRole Resolve(string? display, string? tagName)
    {
        if (!string.IsNullOrWhiteSpace(display) &&
            DisplayTokens.TryGetValue(display.Trim(), out var displayRole))
        {
            return displayRole;
        }

        if (string.IsNullOrWhiteSpace(tagName))
        {
            return BoxRole.Inline;
        }

        return DefaultRoles.GetValueOrDefault(tagName, BoxRole.Inline);
    }
}
