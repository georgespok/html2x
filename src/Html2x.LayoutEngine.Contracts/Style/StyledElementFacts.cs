namespace Html2x.LayoutEngine.Contracts.Style;

internal sealed class StyledElementFacts
{
    private readonly IReadOnlyDictionary<string, string> _attributes;

    public StyledElementFacts(
        string tagName,
        string? localName = null,
        string? id = null,
        string? classAttribute = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        TagName = string.IsNullOrWhiteSpace(tagName)
            ? string.Empty
            : tagName;
        LocalName = string.IsNullOrWhiteSpace(localName)
            ? TagName.ToLowerInvariant()
            : localName!;
        Id = string.IsNullOrWhiteSpace(id) ? null : id;
        ClassAttribute = string.IsNullOrWhiteSpace(classAttribute) ? null : classAttribute;
        _attributes = NormalizeAttributes(attributes, Id, ClassAttribute);
    }

    public static StyledElementFacts Empty { get; } = new(string.Empty);

    public string TagName { get; }

    public string LocalName { get; }

    public string? Id { get; }

    public string? ClassAttribute { get; }

    public IReadOnlyDictionary<string, string> Attributes => _attributes;

    public static StyledElementFacts Create(string tagName, params (string Name, string Value)[] attributes)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in attributes)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                values[name] = value;
            }
        }

        values.TryGetValue(HtmlCssConstants.HtmlAttributes.Id, out var id);
        values.TryGetValue(HtmlCssConstants.HtmlAttributes.Class, out var classAttribute);

        return new StyledElementFacts(tagName, tagName.ToLowerInvariant(), id, classAttribute, values);
    }

    public bool IsTag(string tagName)
    {
        return string.Equals(TagName, tagName, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(LocalName, tagName, StringComparison.OrdinalIgnoreCase);
    }

    public bool HasAttribute(string attributeName)
    {
        return _attributes.ContainsKey(attributeName);
    }

    public string? GetAttribute(string attributeName)
    {
        return _attributes.TryGetValue(attributeName, out var value)
            ? value
            : null;
    }

    private static IReadOnlyDictionary<string, string> NormalizeAttributes(
        IReadOnlyDictionary<string, string>? attributes,
        string? id,
        string? classAttribute)
    {
        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (attributes is not null)
        {
            foreach (var pair in attributes)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key))
                {
                    normalized[pair.Key] = pair.Value;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(id))
        {
            normalized[HtmlCssConstants.HtmlAttributes.Id] = id;
        }

        if (!string.IsNullOrWhiteSpace(classAttribute))
        {
            normalized[HtmlCssConstants.HtmlAttributes.Class] = classAttribute;
        }

        return normalized;
    }
}
