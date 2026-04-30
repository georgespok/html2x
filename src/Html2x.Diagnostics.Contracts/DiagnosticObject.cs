using System.Collections;

namespace Html2x.Diagnostics.Contracts;

public sealed record DiagnosticObject : DiagnosticValue, IReadOnlyDictionary<string, DiagnosticValue?>
{
    private readonly IReadOnlyDictionary<string, DiagnosticValue?> _values;

    public DiagnosticObject(IEnumerable<KeyValuePair<string, DiagnosticValue?>> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        _values = values.ToDictionary(
            static pair => ValidateKey(pair.Key),
            static pair => pair.Value,
            StringComparer.Ordinal);
    }

    public IEnumerable<string> Keys => _values.Keys;

    public IEnumerable<DiagnosticValue?> Values => _values.Values;

    public int Count => _values.Count;

    public DiagnosticValue? this[string key] => _values[key];

    public static DiagnosticObject Empty { get; } =
        new DiagnosticObject(Array.Empty<KeyValuePair<string, DiagnosticValue?>>());

    public static DiagnosticObject Create(params KeyValuePair<string, DiagnosticValue?>[] values) => new(values);

    public static KeyValuePair<string, DiagnosticValue?> Field(string key, DiagnosticValue? value) =>
        new(ValidateKey(key), value);

    public bool ContainsKey(string key) => _values.ContainsKey(key);

    public bool TryGetValue(string key, out DiagnosticValue? value) => _values.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<string, DiagnosticValue?>> GetEnumerator() => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static string ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return key;
    }
}
