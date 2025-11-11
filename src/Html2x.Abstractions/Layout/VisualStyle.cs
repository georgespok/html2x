namespace Html2x.Core.Layout;

public sealed record VisualStyle(
    ColorRgba? BackgroundColor = null, // resolved final color; null = transparent
    BorderEdges? Borders = null,
    CornerRadii? CornerRadius = null,
    float Opacity = 1f,
    bool OverflowHidden = false // layout clip intent
);
