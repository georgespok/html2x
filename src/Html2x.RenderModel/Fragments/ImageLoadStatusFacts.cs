namespace Html2x.RenderModel.Fragments;

/// <summary>
///     Provides derived facts for the canonical image load outcome.
/// </summary>
public static class ImageLoadStatusFacts
{
    /// <summary>
    ///     Returns true when the status means no image bytes should be rendered.
    ///     Oversize images are a separate placeholder case, not missing resources.
    /// </summary>
    public static bool IsMissing(ImageLoadStatus status) =>
        status is not ImageLoadStatus.Ok and not ImageLoadStatus.Oversize;

    /// <summary>
    ///     Returns true when the image exceeded the configured byte limit.
    /// </summary>
    public static bool IsOversize(ImageLoadStatus status) =>
        status == ImageLoadStatus.Oversize;
}