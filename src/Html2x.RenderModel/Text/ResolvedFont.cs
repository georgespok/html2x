namespace Html2x.RenderModel.Text;

/// <summary>
/// Describes a resolved font without renderer-specific types.
/// </summary>
public sealed record ResolvedFont(
    string Family,
    FontWeight Weight,
    FontStyle Style,
    string SourceId,
    string? FilePath = null,
    int FaceIndex = 0,
    string? ConfiguredPath = null);
