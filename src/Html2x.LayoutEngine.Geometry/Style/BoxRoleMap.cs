namespace Html2x.LayoutEngine.Geometry.Style;

internal static class BoxRoleMap
{
    private static readonly IReadOnlyDictionary<string, BoxRole> DisplayTokens =
        new Dictionary<string, BoxRole>(StringComparer.OrdinalIgnoreCase)
        {
            [HtmlCssVocabulary.CssValues.Block] = BoxRole.Block,
            [HtmlCssVocabulary.CssValues.Inline] = BoxRole.Inline,
            [HtmlCssVocabulary.CssValues.InlineBlock] = BoxRole.InlineBlock,
            [HtmlCssVocabulary.CssValues.ListItem] = BoxRole.ListItem,
            [HtmlCssVocabulary.CssValues.Table] = BoxRole.Table,
            [HtmlCssVocabulary.CssValues.TableRowGroup] = BoxRole.TableSection,
            [HtmlCssVocabulary.CssValues.TableHeaderGroup] = BoxRole.TableSection,
            [HtmlCssVocabulary.CssValues.TableFooterGroup] = BoxRole.TableSection,
            [HtmlCssVocabulary.CssValues.TableRow] = BoxRole.TableRow,
            [HtmlCssVocabulary.CssValues.TableCell] = BoxRole.TableCell
        };

    private static readonly IReadOnlyDictionary<string, BoxRole> DefaultRoles =
        new Dictionary<string, BoxRole>(StringComparer.OrdinalIgnoreCase)
        {
            [HtmlCssVocabulary.HtmlTags.Div] = BoxRole.Block,
            [HtmlCssVocabulary.HtmlTags.Section] = BoxRole.Block,
            [HtmlCssVocabulary.HtmlTags.Main] = BoxRole.Block,
            [HtmlCssVocabulary.HtmlTags.Header] = BoxRole.Block,
            [HtmlCssVocabulary.HtmlTags.Footer] = BoxRole.Block,
            [HtmlCssVocabulary.HtmlTags.P] = BoxRole.Block,
            [HtmlCssVocabulary.HtmlTags.Body] = BoxRole.Block,
            [HtmlCssVocabulary.HtmlTags.Ul] = BoxRole.Block,
            [HtmlCssVocabulary.HtmlTags.Ol] = BoxRole.Block,
            [HtmlCssVocabulary.HtmlTags.Li] = BoxRole.ListItem,
            [HtmlCssVocabulary.HtmlTags.Table] = BoxRole.Table,
            [HtmlCssVocabulary.HtmlTags.Tbody] = BoxRole.TableSection,
            [HtmlCssVocabulary.HtmlTags.Thead] = BoxRole.TableSection,
            [HtmlCssVocabulary.HtmlTags.Tfoot] = BoxRole.TableSection,
            [HtmlCssVocabulary.HtmlTags.Tr] = BoxRole.TableRow,
            [HtmlCssVocabulary.HtmlTags.Td] = BoxRole.TableCell,
            [HtmlCssVocabulary.HtmlTags.Th] = BoxRole.TableCell,
            [HtmlCssVocabulary.HtmlTags.Img] = BoxRole.InlineBlock,
            [HtmlCssVocabulary.HtmlTags.Span] = BoxRole.Inline,
            [HtmlCssVocabulary.HtmlTags.B] = BoxRole.Inline,
            [HtmlCssVocabulary.HtmlTags.I] = BoxRole.Inline,
            [HtmlCssVocabulary.HtmlTags.Strong] = BoxRole.Inline,
            [HtmlCssVocabulary.HtmlTags.U] = BoxRole.Inline,
            [HtmlCssVocabulary.HtmlTags.S] = BoxRole.Inline,
            [HtmlCssVocabulary.HtmlTags.H1] = BoxRole.Block,
            [HtmlCssVocabulary.HtmlTags.H2] = BoxRole.Block,
            [HtmlCssVocabulary.HtmlTags.H3] = BoxRole.Block,
            [HtmlCssVocabulary.HtmlTags.H4] = BoxRole.Block,
            [HtmlCssVocabulary.HtmlTags.H5] = BoxRole.Block,
            [HtmlCssVocabulary.HtmlTags.H6] = BoxRole.Block,
            [HtmlCssVocabulary.HtmlTags.Hr] = BoxRole.Block,
            [HtmlCssVocabulary.HtmlTags.Br] = BoxRole.Inline
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