namespace Html2x.LayoutEngine.Models;

public sealed record StyleSourceIdentity
{
    public StyleSourceIdentity(
        StyleNodeId nodeId,
        StyleNodeId? parentId,
        int sourceOrder,
        int siblingIndex,
        string sourcePath,
        string? elementIdentity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sourceOrder);
        ArgumentOutOfRangeException.ThrowIfNegative(siblingIndex);
        ArgumentNullException.ThrowIfNull(sourcePath);

        if (nodeId.IsSpecified && string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException(
                "Source path is required for a specified style source identity.",
                nameof(sourcePath));
        }

        NodeId = nodeId;
        ParentId = parentId;
        SourceOrder = sourceOrder;
        SiblingIndex = siblingIndex;
        SourcePath = sourcePath;
        ElementIdentity = string.IsNullOrWhiteSpace(elementIdentity) ? null : elementIdentity;
    }

    public static StyleSourceIdentity Unspecified { get; } = new(
        StyleNodeId.Unspecified,
        null,
        0,
        0,
        string.Empty,
        null);

    public StyleNodeId NodeId { get; }

    public StyleNodeId? ParentId { get; }

    public int SourceOrder { get; }

    public int SiblingIndex { get; }

    public string SourcePath { get; }

    public string? ElementIdentity { get; }
}
