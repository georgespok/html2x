namespace Html2x.RenderModel.Resources;

/// <summary>
///     Provides derived facts for the canonical image load outcome.
/// </summary>
internal static class ImageLoadStatusFacts
{
    /// <summary>
    ///     Returns true when the status means no image bytes should be rendered.
    ///     Oversized images are a separate placeholder case, not missing resources.
    /// </summary>
    public static bool IsMissing(ImageLoadStatus status) =>
        status is not ImageLoadStatus.Ok and not ImageLoadStatus.Oversized;

    /// <summary>
    ///     Returns true when the image exceeded the configured byte limit.
    /// </summary>
    public static bool IsOversized(ImageLoadStatus status) =>
        status == ImageLoadStatus.Oversized;
}
