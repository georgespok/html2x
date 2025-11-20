namespace Html2x.Abstractions.Diagnostics.Contracts;

public sealed record SnapshotNode
{
    public SnapshotNode(
        string id,
        string type,
        string? name,
        IReadOnlyDictionary<string, object?> attributes,
        IReadOnlyList<SnapshotNode> children)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Node identifier is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Node type is required.", nameof(type));
        }

        Attributes = attributes ?? new Dictionary<string, object?>(StringComparer.Ordinal);
        Children = children ?? [];
        Id = id;
        Type = type;
        Name = name;
    }

    public string Id { get; }

    public string Type { get; }

    public string? Name { get; }

    public IReadOnlyDictionary<string, object?> Attributes { get; }

    public IReadOnlyList<SnapshotNode> Children { get; }
}