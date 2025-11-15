namespace Html2x.Abstractions.Diagnostics.Contracts;

public sealed record StructuredDumpDocument
{
    public StructuredDumpDocument(
        string category,
        string summary,
        IReadOnlyList<StructuredDumpNode> nodes,
        int nodeCount)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Dump category is required.", nameof(category));
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Dump summary is required.", nameof(summary));
        }

        if (nodes is null)
        {
            throw new ArgumentNullException(nameof(nodes));
        }

        if (nodeCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nodeCount), "Node count must be non-negative.");
        }

        Category = category;
        Summary = summary;
        Nodes = nodes;
        NodeCount = nodeCount;
    }

    public string Category { get; }

    public string Summary { get; }

    public IReadOnlyList<StructuredDumpNode> Nodes { get; }

    public int NodeCount { get; }
}

public sealed record StructuredDumpNode
{
    public StructuredDumpNode(
        string id,
        string type,
        string? name,
        IReadOnlyDictionary<string, object?> attributes,
        IReadOnlyList<StructuredDumpNode> children)
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
        Children = children ?? Array.Empty<StructuredDumpNode>();
        Id = id;
        Type = type;
        Name = name;
    }

    public string Id { get; }

    public string Type { get; }

    public string? Name { get; }

    public IReadOnlyDictionary<string, object?> Attributes { get; }

    public IReadOnlyList<StructuredDumpNode> Children { get; }
}
