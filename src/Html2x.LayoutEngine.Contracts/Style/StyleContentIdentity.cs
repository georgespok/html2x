namespace Html2x.LayoutEngine.Contracts.Style;

internal sealed record StyleContentIdentity
{
    public StyleContentIdentity(
        StyleContentId contentId,
        StyleNodeId parentId,
        int sourceOrder,
        int siblingIndex,
        string sourcePath)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sourceOrder);
        ArgumentOutOfRangeException.ThrowIfNegative(siblingIndex);
        ArgumentNullException.ThrowIfNull(sourcePath);

        if (contentId.IsSpecified && string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException(
                "Source path is required for a specified style content identity.",
                nameof(sourcePath));
        }

        ContentId = contentId;
        ParentId = parentId;
        SourceOrder = sourceOrder;
        SiblingIndex = siblingIndex;
        SourcePath = sourcePath;
    }

    public static StyleContentIdentity Unspecified { get; } = new(
        StyleContentId.Unspecified,
        StyleNodeId.Unspecified,
        0,
        0,
        string.Empty);

    public StyleContentId ContentId { get; }

    public StyleNodeId ParentId { get; }

    public int SourceOrder { get; }

    public int SiblingIndex { get; }

    public string SourcePath { get; }
}