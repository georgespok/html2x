namespace Html2x.RenderModel;

/// <summary>
/// Describes the shared image resource load outcome carried by image fragments.
/// </summary>
public enum ImageLoadStatus
{
    Ok,
    Missing,
    Oversize,
    InvalidDataUri,
    DecodeFailed,
    OutOfScope
}
