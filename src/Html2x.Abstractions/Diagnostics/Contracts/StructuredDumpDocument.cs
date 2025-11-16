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

    public IReadOnlyList<StructuredDumpNode> Nodes { get; }

    public int NodeCount { get; }
}