namespace Html2x.RenderModel.Fragments;

/// <summary>
///     Describes the shared image resource load outcome carried by image fragments.
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