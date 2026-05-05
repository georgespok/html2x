using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Measurements.Units;

namespace Html2x.RenderModel.Fragments;

/// <summary>
/// Represents an HTML &lt;img&gt; element after style resolution and basic validation.
/// Values are immutable and passed to layout/rendering stages.
/// </summary>
public sealed class ImageFragment : Fragment
{
    private readonly RectPt _contentRect;

    /// <summary>Original src attribute value (data URI or file path relative to input HTML).</summary>
    public required string Src { get; init; }

    /// <summary>
    /// Content box rect after applying padding and borders to the outer rect.
    /// Used for image drawing and placeholders.
    /// </summary>
    public required RectPt ContentRect
    {
        get => _contentRect;
        init
        {
            FragmentGeometryGuard.GuardRect(nameof(ContentRect), value);
            _contentRect = value;
        }
    }

    public SizePt ContentSize => new(ContentRect.Width, ContentRect.Height);

    /// <summary>Author-specified size in CSS pixels, if present.</summary>
    public SizePx AuthoredSizePx { get; init; }

    /// <summary>Intrinsic image size in CSS pixels (0 if unknown at fragment creation).</summary>
    public SizePx IntrinsicSizePx { get; init; }

    /// <summary>Shared image resource load status carried from layout into rendering diagnostics.</summary>
    public ImageLoadStatus Status { get; init; }

    /// <summary>True when the image failed validation or loading and should render as a missing placeholder.</summary>
    public bool IsMissing => ImageLoadStatusFacts.IsMissing(Status);

    /// <summary>True when the image exceeded size caps and will be rendered as a placeholder.</summary>
    public bool IsOversize => ImageLoadStatusFacts.IsOversize(Status);
}
