using Html2x.Abstractions.Layout.Styles;

namespace Html2x.Abstractions.Layout.Fonts;

/// <summary>
/// Describes a resolved font without renderer-specific types.
/// </summary>
public sealed record ResolvedFont(
    string Family,
    FontWeight Weight,
    FontStyle Style,
    string SourceId,
    string? FilePath = null);
