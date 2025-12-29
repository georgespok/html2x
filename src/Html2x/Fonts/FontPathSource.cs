using Html2x.Abstractions.File;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Styles;

namespace Html2x.Fonts;

internal sealed class FontPathSource : IFontSource
{
    private readonly string _fontPath;
    private readonly IFileDirectory _fileDirectory;

    public FontPathSource(string fontPath, IFileDirectory fileDirectory)
    {
        if (string.IsNullOrWhiteSpace(fontPath))
        {
            throw new ArgumentException("Font path must be provided.", nameof(fontPath));
        }

        _fontPath = fontPath;
        _fileDirectory = fileDirectory ?? throw new ArgumentNullException(nameof(fileDirectory));
    }

    public ResolvedFont Resolve(FontKey requested)
    {
        var sourceId = BuildSourceId(requested);
        return new ResolvedFont(requested.Family, requested.Weight, requested.Style, sourceId, _fontPath);
    }

    private string BuildSourceId(FontKey requested)
    {
        if (_fileDirectory.FileExists(_fontPath))
        {
            return _fontPath;
        }

        return $"{_fontPath}|{requested.Family}|{(int)requested.Weight}|{requested.Style}";
    }
}
