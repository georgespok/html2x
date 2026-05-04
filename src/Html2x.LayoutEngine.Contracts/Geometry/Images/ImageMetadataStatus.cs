namespace Html2x.LayoutEngine.Contracts.Geometry.Images;

internal enum ImageMetadataStatus
{
    Ok,
    Missing,
    Oversize,
    InvalidDataUri,
    DecodeFailed,
    OutOfScope
}
