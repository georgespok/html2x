using Html2x.RenderModel.Fragments;

namespace Html2x.Resources;

internal sealed class ConversionImageResourceStore : IImageResourceReader
{
    private readonly string _baseDirectory;
    private readonly Dictionary<string, ImageResourceResult> _resources = new(StringComparer.Ordinal);
    private readonly long _maxBytes;

    public ConversionImageResourceStore(string? baseDirectory, long maxBytes)
    {
        if (maxBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxBytes),
                "Maximum image size must be greater than zero.");
        }

        _baseDirectory = ImageResourceLoader.ResolveBaseDirectory(baseDirectory);
        _maxBytes = maxBytes;
    }

    public ImageResourceMetadataResult LoadMetadata(string src)
    {
        var resource = Load(src);
        return new()
        {
            Src = resource.Src,
            Status = resource.Status,
            IntrinsicSizePx = resource.IntrinsicSizePx
        };
    }

    public ImageResourceResult Load(string src)
    {
        var key = src ?? string.Empty;
        if (_resources.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var resource = ImageResourceLoader.Load(key, _baseDirectory, _maxBytes);
        _resources.Add(key, resource);
        return resource;
    }
}
