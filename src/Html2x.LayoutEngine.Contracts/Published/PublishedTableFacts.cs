namespace Html2x.LayoutEngine.Contracts.Published;

internal sealed record PublishedTableFacts
{
    public PublishedTableFacts(
        int? derivedColumnCount,
        int? rowIndex,
        int? columnIndex,
        bool? isHeader,
        int columnSpan = 1)
    {
        PublishedLayoutGuard.ThrowIfNegative(derivedColumnCount, nameof(derivedColumnCount));
        PublishedLayoutGuard.ThrowIfNegative(rowIndex, nameof(rowIndex));
        PublishedLayoutGuard.ThrowIfNegative(columnIndex, nameof(columnIndex));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columnSpan);

        DerivedColumnCount = derivedColumnCount;
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        IsHeader = isHeader;
        ColumnSpan = columnSpan;
    }

    public int? DerivedColumnCount { get; }

    public int? RowIndex { get; }

    public int? ColumnIndex { get; }

    public bool? IsHeader { get; }

    public int ColumnSpan { get; }
}