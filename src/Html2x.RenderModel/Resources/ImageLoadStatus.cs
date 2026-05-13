namespace Html2x.RenderModel.Resources;

/// <summary>
///     Describes the shared image resource load outcome carried across layout and rendering.
/// </summary>
public enum ImageLoadStatus
{
    Ok,
    Missing,
    Oversized,
    InvalidDataUri,
    DecodeFailed,
    OutOfScope
}
