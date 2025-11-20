namespace Html2x.Abstractions.Diagnostics.Contracts;

public sealed record SnapshotDocument
{
    public SnapshotDocument(
        string category,
        string summary,
        IReadOnlyList<SnapshotNode> nodes,
        int nodeCount)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Metadata category is required.", nameof(category));
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Metadata summary is required.", nameof(summary));
        }

        if (nodeCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nodeCount), "Node count must be non-negative.");
        }

        Category = category;
        Summary = summary;
        Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        NodeCount = nodeCount;
    }

    public string Category { get; }

    public string Summary { get; }

    public IReadOnlyList<SnapshotNode> Nodes { get; }

    public int NodeCount { get; }
}