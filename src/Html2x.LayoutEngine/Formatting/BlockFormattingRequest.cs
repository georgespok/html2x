using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Formatting;

internal sealed record BlockFormattingRequest
{
    public BlockFormattingRequest(
        FormattingContextKind contextKind,
        BlockBox rootBlock,
        float availableWidth,
        bool isWidthUnbounded = false,
        object? pageContext = null)
    {
        ContextKind = contextKind;
        RootBlock = rootBlock ?? throw new ArgumentNullException(nameof(rootBlock));
        AvailableWidth = availableWidth;
        IsWidthUnbounded = isWidthUnbounded;
        PageContext = pageContext;

        ValidateWidth(availableWidth, isWidthUnbounded);
    }

    public FormattingContextKind ContextKind { get; }

    public BlockBox RootBlock { get; }

    public float AvailableWidth { get; }

    public bool IsWidthUnbounded { get; }

    public object? PageContext { get; }

    public static BlockFormattingRequest ForInlineBlock(
        BlockBox rootBlock,
        float availableWidth)
    {
        return new BlockFormattingRequest(
            FormattingContextKind.InlineBlock,
            rootBlock,
            availableWidth,
            isWidthUnbounded: false,
            pageContext: null);
    }

    public static BlockFormattingRequest ForTopLevel(
        BlockBox rootBlock,
        float availableWidth,
        object? pageContext = null)
    {
        return new BlockFormattingRequest(
            FormattingContextKind.Block,
            rootBlock,
            availableWidth,
            isWidthUnbounded: false,
            pageContext);
    }

    public static BlockFormattingRequest ForUnboundedWidth(
        FormattingContextKind contextKind,
        BlockBox rootBlock,
        object? pageContext = null)
    {
        return new BlockFormattingRequest(
            contextKind,
            rootBlock,
            float.PositiveInfinity,
            isWidthUnbounded: true,
            pageContext);
    }

    private static void ValidateWidth(float availableWidth, bool isWidthUnbounded)
    {
        if (isWidthUnbounded)
        {
            if (!float.IsPositiveInfinity(availableWidth))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(availableWidth),
                    "Unbounded width requests must use float.PositiveInfinity.");
            }

            return;
        }

        if (!float.IsFinite(availableWidth))
        {
            throw new ArgumentOutOfRangeException(
                nameof(availableWidth),
                "Available width must be finite unless explicitly marked as unbounded.");
        }

        if (availableWidth < 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(availableWidth),
                "Available width cannot be negative.");
        }
    }
}
