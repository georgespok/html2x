using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Formatting;

/// <summary>
/// Resolves the current formatting context owner for a display node.
/// </summary>
internal static class FormattingContextBoundaryResolver
{
    public static FormattingContextBoundary Resolve(DisplayNode source, string owner)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source switch
        {
            TableBox => Create(source, FormattingContextRole.Table, owner, "current-table-layout"),
            InlineBox { Role: DisplayRole.InlineBlock } => Create(
                source,
                FormattingContextRole.InlineBlock,
                owner,
                "current-inline-block-layout"),
            InlineBox => Create(source, FormattingContextRole.Inline, owner, "current-inline-layout"),
            BlockBox { IsInlineBlockContext: true } => Create(
                source,
                FormattingContextRole.InlineBlock,
                owner,
                "current-inline-block-content-flow"),
            _ => Create(source, FormattingContextRole.Block, owner, "current-block-flow")
        };
    }

    public static FormattingContextBoundary UnsupportedDiagnostic(
        DisplayNode source,
        FormattingContextRole fallbackRole,
        string owner,
        string behavior,
        string reason)
    {
        return new FormattingContextBoundary(
            source,
            fallbackRole,
            ResolvePublishedContext(source, fallbackRole),
            owner,
            FormattingContextSupport.UnsupportedDiagnostic,
            behavior,
            reason);
    }

    public static FormattingContextBoundary UnsupportedFailFast(
        DisplayNode source,
        FormattingContextRole fallbackRole,
        string owner,
        string behavior,
        string reason)
    {
        return new FormattingContextBoundary(
            source,
            fallbackRole,
            ResolvePublishedContext(source, fallbackRole),
            owner,
            FormattingContextSupport.UnsupportedFailFast,
            behavior,
            reason);
    }

    private static FormattingContextBoundary Create(
        DisplayNode source,
        FormattingContextRole role,
        string owner,
        string behavior)
    {
        return new FormattingContextBoundary(
            source,
            role,
            ResolvePublishedContext(source, role),
            owner,
            FormattingContextSupport.Supported,
            behavior);
    }

    private static FormattingContextKind ResolvePublishedContext(
        DisplayNode source,
        FormattingContextRole role)
    {
        if (role == FormattingContextRole.InlineBlock)
        {
            return FormattingContextKind.InlineBlock;
        }

        return source is BlockBox { IsInlineBlockContext: true }
            ? FormattingContextKind.InlineBlock
            : FormattingContextKind.Block;
    }
}
