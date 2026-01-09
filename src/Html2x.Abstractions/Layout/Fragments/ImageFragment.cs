using System.Drawing;
using Html2x.Abstractions.Measurements.Units;

namespace Html2x.Abstractions.Layout.Fragments;

/// <summary>
/// Represents an HTML &lt;img&gt; element after style resolution and basic validation.
/// Values are immutable and passed to layout/rendering stages.
/// </summary>
public sealed class ImageFragment : Fragment
{
    /// <summary>Original src attribute value (data URI or file path relative to input HTML).</summary>
    public required string Src { get; init; }

    /// <summary>
    /// Content box rect after applying padding and borders to the outer rect.
    /// Used for image drawing and placeholders.
    /// </summary>
    public RectangleF ContentRect { get; init; }
    public SizePt ContentSize => new(ContentRect.Width, ContentRect.Height);

    /// <summary>Author-specified size in CSS pixels, if present.</summary>
    public SizePx AuthoredSizePx { get; init; }

    /// <summary>Intrinsic image size in CSS pixels (0 if unknown at fragment creation).</summary>
    public SizePx IntrinsicSizePx { get; init; }

    /// <summary>True when the image failed validation or loading (e.g., out-of-scope path).</summary>
    public bool IsMissing { get; init; }

    /// <summary>True when the image exceeded size caps and will be rendered as a placeholder.</summary>
    public bool IsOversize { get; init; }
}
