namespace Html2x.LayoutEngine.Geometry.Box;


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
                        TableStructureDiagnosticNames.StructureKinds.UnsupportedTableChild,
                        $"Tables currently support only direct row and section children. Found '{child.Role}'.",
                        rowCount);
            }
        }

        if (hasDirectRows && hasDirectSections)
        {
            return TableRowModelResult.Unsupported(
                TableStructureDiagnosticNames.StructureKinds.MalformedSectionNesting,
                TableStructureDiagnosticNames.Reasons.MixedDirectRowsAndSections,
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
                    TableStructureDiagnosticNames.StructureKinds.MalformedSectionNesting,
                    TableStructureDiagnosticNames.Reasons.NestedTableSections);
            }

            if (child is not TableRowBox row)
            {
                return TableStructureValidation.Unsupported(
                    TableStructureDiagnosticNames.StructureKinds.MalformedSectionNesting,
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
                    TableStructureDiagnosticNames.StructureKinds.UnsupportedRowChild,
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
        var colspan = GetSpanValue(cell.Element, HtmlCssConstants.HtmlAttributes.Colspan);
        if (colspan.HasValue && colspan.Value != 1)
        {
            return TableStructureValidation.Unsupported(
                HtmlCssConstants.HtmlAttributes.Colspan,
                TableStructureDiagnosticNames.Reasons.UnsupportedColspan);
        }

        var rowspan = GetSpanValue(cell.Element, HtmlCssConstants.HtmlAttributes.Rowspan);
        if (rowspan.HasValue && rowspan.Value != 1)
        {
            return TableStructureValidation.Unsupported(
                HtmlCssConstants.HtmlAttributes.Rowspan,
                TableStructureDiagnosticNames.Reasons.UnsupportedRowspan);
        }

        return TableStructureValidation.Supported();
    }

    private static int? GetSpanValue(StyledElementFacts? element, string attributeName)
    {
        if (element is null || !element.HasAttribute(attributeName))
        {
            return null;
        }

        var rawValue = element.GetAttribute(attributeName);
        if (!int.TryParse(rawValue, out var value) || value < 1)
        {
            return 0;
        }

        return value;
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
