namespace Html2x.RenderModel.Styles;

/// <summary>
/// Renderer-facing visual facts that the built-in renderers can consume.
/// </summary>
/// <param name="BackgroundColor">Resolved background color. A null value means transparent.</param>
/// <param name="Borders">Resolved border edges, or null when no edge is painted.</param>
/// <param name="Color">Resolved foreground text color.</param>
/// <param name="Margin">Resolved margin values in points.</param>
/// <param name="Padding">Resolved padding values in points.</param>
/// <param name="WidthPt">Resolved content width in points when explicitly known.</param>
/// <param name="HeightPt">Resolved content height in points when explicitly known.</param>
/// <param name="Display">Resolved display value retained for diagnostics and custom renderers.</param>
public sealed record VisualStyle(
    ColorRgba? BackgroundColor = null,
    BorderEdges? Borders = null,
    ColorRgba? Color = null,
    Spacing? Margin = null,
    Spacing? Padding = null,
    float? WidthPt = null,
    float? HeightPt = null,
    string? Display = null
);
