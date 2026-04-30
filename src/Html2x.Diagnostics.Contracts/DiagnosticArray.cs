using System.Collections;

namespace Html2x.Diagnostics.Contracts;

public sealed record DiagnosticArray : DiagnosticValue, IReadOnlyList<DiagnosticValue?>
{
    private readonly IReadOnlyList<DiagnosticValue?> _values;

    public DiagnosticArray(IEnumerable<DiagnosticValue?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        _values = values.ToArray();
    }

    public int Count => _values.Count;

    public DiagnosticValue? this[int index] => _values[index];

    public static DiagnosticArray Empty { get; } = new(Array.Empty<DiagnosticValue?>());

    public static DiagnosticArray Create(params DiagnosticValue?[] values) => new(values);

    public IEnumerator<DiagnosticValue?> GetEnumerator() => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
