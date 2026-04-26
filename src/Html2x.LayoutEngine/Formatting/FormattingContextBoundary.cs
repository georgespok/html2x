using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Formatting;

/// <summary>
/// Describes the internal owner and fallback policy for one formatting context boundary.
/// </summary>
internal sealed record FormattingContextBoundary
{
    public FormattingContextBoundary(
        DisplayNode source,
        FormattingContextRole role,
        FormattingContextKind contextKind,
        string owner,
        FormattingContextSupport support,
        string behavior,
        string? reason = null)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Role = role;
        ContextKind = contextKind;
        Owner = string.IsNullOrWhiteSpace(owner) ? "unknown" : owner;
        Support = support;
        Behavior = string.IsNullOrWhiteSpace(behavior) ? "unspecified" : behavior;
        Reason = reason;
    }

    public DisplayNode Source { get; }

    public FormattingContextRole Role { get; }

    public FormattingContextKind ContextKind { get; }

    public string Owner { get; }

    public FormattingContextSupport Support { get; }

    public string Behavior { get; }

    public string? Reason { get; }
}
