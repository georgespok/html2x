using Html2x.RenderModel;

namespace Html2x;


/// <summary>
/// Resource loading options shared by layout and rendering.
/// </summary>
public sealed class ResourceOptions
{
    /// <summary>Base directory used to resolve relative resource paths.</summary>
    public string? BaseDirectory { get; init; }

    /// <summary>Maximum allowed image size in bytes; images over this are marked oversize.</summary>
    public long MaxImageSizeBytes { get; init; } = 10 * 1024 * 1024;
}
