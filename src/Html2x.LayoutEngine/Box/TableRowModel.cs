using AngleSharp.Dom;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

internal static class TableRowModel
{
    // Current supported row model:
    // table -> tr -> td|th
    // table -> thead|tbody|tfoot -> tr -> td|th
    // Arbitrary descendants do not participate in the table grid.
    public static TableRowModelResult Build(TableBox table)
    {
        ArgumentNullException.ThrowIfNull(table);

        var rowCount = CountRowsForDiagnostics(table);
        var rows = new List<TableRowBox>();
        var hasDirectRows = false;
        var hasDirectSections = false;

        foreach (var child in table.Children)
        {
            switch (child)
            {
                case TableRowBox row:
                    hasDirectRows = true;
                    var rowValidation = ValidateRow(row);
                    if (!rowValidation.IsSupported)
                    {
                        return TableRowModelResult.Unsupported(
                            rowValidation.UnsupportedStructureKind,
                            rowValidation.UnsupportedReason,
                            rowCount);
                    }

                    rows.Add(row);
                    break;
                case TableSectionBox section:
                    hasDirectSections = true;
                    var sectionValidation = AddSectionRows(section, rows);
                    if (!sectionValidation.IsSupported)
                    {
                        return TableRowModelResult.Unsupported(
                            sectionValidation.UnsupportedStructureKind,
                            sectionValidation.UnsupportedReason,
                            rowCount);
                    }

                    break;
                default:
                    return TableRowModelResult.Unsupported(
                        "unsupported-table-child",
                        $"Tables currently support only direct row and section children. Found '{child.Role}'.",
                        rowCount);
            }
        }

        if (hasDirectRows && hasDirectSections)
        {
            return TableRowModelResult.Unsupported(
                "malformed-section-nesting",
                "Tables cannot mix direct rows with explicit table sections.",
                rowCount);
        }

        return TableRowModelResult.Supported(rows, rowCount);
    }

    private static int CountRowsForDiagnostics(TableBox table)
    {
        return table.Children.Sum(static child => child switch
        {
            TableRowBox => 1,
            TableSectionBox section => section.Children.OfType<TableRowBox>().Count(),
            _ => 0
        });
    }

    private static TableStructureValidation AddSectionRows(TableSectionBox section, List<TableRowBox> rows)
    {
        foreach (var child in section.Children)
        {
            if (child is TableSectionBox)
            {
                return TableStructureValidation.Unsupported(
                    "malformed-section-nesting",
                    "Table sections cannot contain nested table sections.");
            }

            if (child is not TableRowBox row)
            {
                return TableStructureValidation.Unsupported(
                    "malformed-section-nesting",
                    $"Table sections currently support only direct row children. Found '{child.Role}'.");
            }

            var rowValidation = ValidateRow(row);
            if (!rowValidation.IsSupported)
            {
                return rowValidation;
            }

            rows.Add(row);
        }

        return TableStructureValidation.Supported();
    }

    private static TableStructureValidation ValidateRow(TableRowBox row)
    {
        foreach (var child in row.Children)
        {
            if (child is not TableCellBox cell)
            {
                return TableStructureValidation.Unsupported(
                    "unsupported-row-child",
                    $"Table rows currently support only direct cell children. Found '{child.Role}'.");
            }

            var cellValidation = ValidateCell(cell);
            if (!cellValidation.IsSupported)
            {
                return cellValidation;
            }
        }

        return TableStructureValidation.Supported();
    }

    private static TableStructureValidation ValidateCell(TableCellBox cell)
    {
        if (TryGetSpanValue(cell.Element, HtmlCssConstants.HtmlAttributes.Colspan, out var colspan) && colspan != 1)
        {
            return TableStructureValidation.Unsupported(
                HtmlCssConstants.HtmlAttributes.Colspan,
                "Table cell colspan is not supported.");
        }

        if (TryGetSpanValue(cell.Element, HtmlCssConstants.HtmlAttributes.Rowspan, out var rowspan) && rowspan != 1)
        {
            return TableStructureValidation.Unsupported(
                HtmlCssConstants.HtmlAttributes.Rowspan,
                "Table cell rowspan is not supported.");
        }

        return TableStructureValidation.Supported();
    }

    private static bool TryGetSpanValue(IElement? element, string attributeName, out int value)
    {
        value = 0;
        if (element is null || !element.HasAttribute(attributeName))
        {
            return false;
        }

        var rawValue = element.GetAttribute(attributeName);
        if (!int.TryParse(rawValue, out value) || value < 1)
        {
            value = 0;
            return true;
        }

        return true;
    }

    private readonly record struct TableStructureValidation(
        bool IsSupported,
        string UnsupportedStructureKind,
        string UnsupportedReason)
    {
        public static TableStructureValidation Supported()
        {
            return new TableStructureValidation(true, string.Empty, string.Empty);
        }

        public static TableStructureValidation Unsupported(string structureKind, string reason)
        {
            return new TableStructureValidation(false, structureKind, reason);
        }
    }
}

internal readonly record struct TableRowModelResult(
    bool IsSupported,
    string UnsupportedStructureKind,
    string UnsupportedReason,
    IReadOnlyList<TableRowBox> Rows,
    int RowCount)
{
    public static TableRowModelResult Supported(IReadOnlyList<TableRowBox> rows, int rowCount)
    {
        return new TableRowModelResult(
            true,
            string.Empty,
            string.Empty,
            rows,
            rowCount);
    }

    public static TableRowModelResult Unsupported(string structureKind, string reason, int rowCount)
    {
        return new TableRowModelResult(
            false,
            structureKind,
            reason,
            [],
            rowCount);
    }
}
