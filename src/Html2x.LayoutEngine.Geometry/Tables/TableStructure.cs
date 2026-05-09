using Html2x.LayoutEngine.Geometry.Models;

namespace Html2x.LayoutEngine.Geometry.Tables;

internal static class TableStructure
{
    // Current supported row model:
    // table -> tr -> td|th
    // table -> thead|tbody|tfoot -> tr -> td|th
    // Arbitrary descendants do not participate in the table grid.
    public static TableStructureResult Build(TableBox table)
    {
        ArgumentNullException.ThrowIfNull(table);

        var rowCount = CountRowsForDiagnostics(table);
        var rows = new List<TableRowFacts>();
        var hasDirectRows = false;
        var hasDirectSections = false;

        foreach (var child in table.Children)
        {
            switch (child)
            {
                case TableRowBox row:
                    hasDirectRows = true;
                    var rowValidation = BuildRowFacts(row);
                    if (!rowValidation.IsSupported)
                    {
                        return TableStructureResult.Unsupported(
                            rowValidation.UnsupportedStructureKind,
                            rowValidation.UnsupportedReason,
                            rowCount);
                    }

                    rows.Add(rowValidation.RowFacts ?? throw new InvalidOperationException(
                        "Supported table row validation must include row facts."));
                    break;
                case TableSectionBox section:
                    hasDirectSections = true;
                    var sectionValidation = AddSectionRows(section, rows);
                    if (!sectionValidation.IsSupported)
                    {
                        return TableStructureResult.Unsupported(
                            sectionValidation.UnsupportedStructureKind,
                            sectionValidation.UnsupportedReason,
                            rowCount);
                    }

                    break;
                default:
                    return TableStructureResult.Unsupported(
                        TableStructureDiagnosticNames.StructureKinds.UnsupportedTableChild,
                        $"Tables currently support only direct row and section children. Found '{child.Role}'.",
                        rowCount);
            }
        }

        if (hasDirectRows && hasDirectSections)
        {
            return TableStructureResult.Unsupported(
                TableStructureDiagnosticNames.StructureKinds.MalformedSectionNesting,
                TableStructureDiagnosticNames.Reasons.MixedDirectRowsAndSections,
                rowCount);
        }

        return TableStructureResult.Supported(rows, rowCount);
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

    private static TableStructureValidation AddSectionRows(TableSectionBox section, List<TableRowFacts> rows)
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

            var rowValidation = BuildRowFacts(row);
            if (!rowValidation.IsSupported)
            {
                return rowValidation;
            }

            rows.Add(rowValidation.RowFacts ?? throw new InvalidOperationException(
                "Supported table row validation must include row facts."));
        }

        return TableStructureValidation.Supported();
    }

    private static TableStructureValidation BuildRowFacts(TableRowBox row)
    {
        var cells = new List<TableCellFacts>();
        var effectiveColumnCount = 0;

        foreach (var child in row.Children)
        {
            if (child is not TableCellBox cell)
            {
                return TableStructureValidation.Unsupported(
                    TableStructureDiagnosticNames.StructureKinds.UnsupportedRowChild,
                    $"Table rows currently support only direct cell children. Found '{child.Role}'.");
            }

            var cellValidation = BuildCellFacts(cell);
            if (!cellValidation.IsSupported)
            {
                return cellValidation;
            }

            var cellFacts = cellValidation.CellFacts ?? throw new InvalidOperationException(
                "Supported table cell validation must include cell facts.");
            cells.Add(cellFacts);
            effectiveColumnCount += cellFacts.ColumnSpan;
        }

        return TableStructureValidation.Supported(new TableRowFacts(row, cells, effectiveColumnCount));
    }

    private static TableStructureValidation BuildCellFacts(TableCellBox cell)
    {
        var colspan = GetSpanValue(cell.Element, HtmlCssConstants.HtmlAttributes.Colspan);
        if (colspan.HasValue && colspan.Value < 1)
        {
            return TableStructureValidation.Unsupported(
                HtmlCssConstants.HtmlAttributes.Colspan,
                TableStructureDiagnosticNames.Reasons.InvalidColspan);
        }

        var rowspan = GetSpanValue(cell.Element, HtmlCssConstants.HtmlAttributes.Rowspan);
        if (rowspan.HasValue && rowspan.Value != 1)
        {
            return TableStructureValidation.Unsupported(
                HtmlCssConstants.HtmlAttributes.Rowspan,
                TableStructureDiagnosticNames.Reasons.UnsupportedRowspan);
        }

        return TableStructureValidation.Supported(new TableCellFacts(cell, colspan ?? 1));
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
        string UnsupportedReason,
        TableRowFacts? RowFacts,
        TableCellFacts? CellFacts)
    {
        public static TableStructureValidation Supported() => new(true, string.Empty, string.Empty, null, null);

        public static TableStructureValidation Supported(TableRowFacts rowFacts) =>
            new(true, string.Empty, string.Empty, rowFacts, null);

        public static TableStructureValidation Supported(TableCellFacts cellFacts) =>
            new(true, string.Empty, string.Empty, null, cellFacts);

        public static TableStructureValidation Unsupported(string structureKind, string reason) =>
            new(false, structureKind, reason, null, null);
    }
}
