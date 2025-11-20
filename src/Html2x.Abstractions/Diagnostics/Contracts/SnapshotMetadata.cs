namespace Html2x.Abstractions.Diagnostics.Contracts;

public sealed record SnapshotMetadata
{
    public SnapshotMetadata(
        string format,
        string summary,
        int nodeCount,
        string body)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            throw new ArgumentException("Metadata format is required.", nameof(format));
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Metadata summary is required.", nameof(summary));
        }

        if (nodeCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nodeCount), "Node count must be non-negative.");
        }

        if (body is null)
        {
            throw new ArgumentNullException(nameof(body));
        }

        Format = format;
        Summary = summary;
        NodeCount = nodeCount;
        Body = body;
    }

    public string Format { get; }

    public string Summary { get; }

    public int NodeCount { get; }

    public string Body { get; }
}