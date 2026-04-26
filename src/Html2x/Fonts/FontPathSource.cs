using Html2x.Abstractions.File;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Renderers.Pdf.Drawing;
using SkiaSharp;

namespace Html2x.Fonts;

internal sealed class FontPathSource : IFontSource
{
    private readonly string _fontPath;
    private readonly IFileDirectory _fileDirectory;
    private readonly ISkiaTypefaceFactory _typefaceFactory;
    private readonly Lazy<IReadOnlyList<FontFaceEntry>> _directoryFaces;
    private readonly Lazy<bool> _singleFileValidated;

    public FontPathSource(string fontPath, IFileDirectory fileDirectory)
        : this(fontPath, fileDirectory, new DefaultTypefaceFactory())
    {
    }

    internal FontPathSource(string fontPath, IFileDirectory fileDirectory, ISkiaTypefaceFactory typefaceFactory)
    {
        if (string.IsNullOrWhiteSpace(fontPath))
        {
            throw new ArgumentException("Font path must be provided.", nameof(fontPath));
        }

        _fontPath = fontPath;
        _fileDirectory = fileDirectory ?? throw new ArgumentNullException(nameof(fileDirectory));
        _typefaceFactory = typefaceFactory ?? throw new ArgumentNullException(nameof(typefaceFactory));
        _directoryFaces = new Lazy<IReadOnlyList<FontFaceEntry>>(
            () => FontDirectoryIndex.Build(_fileDirectory, _typefaceFactory, _fontPath),
            LazyThreadSafetyMode.ExecutionAndPublication);
        _singleFileValidated = new Lazy<bool>(ValidateSingleFileFont, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public ResolvedFont Resolve(FontKey requested, string consumer)
    {
        ArgumentNullException.ThrowIfNull(requested);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumer);

        if (_fileDirectory.FileExists(_fontPath))
        {
            _ = _singleFileValidated.Value;
            return new ResolvedFont(
                requested.Family,
                requested.Weight,
                requested.Style,
                BuildSourceId(_fontPath, faceIndex: 0),
                FilePath: _fontPath,
                FaceIndex: 0,
                ConfiguredPath: _fontPath);
        }

        if (_fileDirectory.DirectoryExists(_fontPath))
        {
            var best = FontDirectoryIndex.FindBestMatch(_directoryFaces.Value, requested);
            if (best is null)
            {
                throw CreateFontResolutionException(
                    $"Font '{requested.Family}' not found in directory '{_fontPath}'.",
                    requested,
                    configuredPath: _fontPath);
            }

            return new ResolvedFont(
                best.Family,
                (FontWeight)best.Weight,
                best.IsItalic ? FontStyle.Italic : FontStyle.Normal,
                BuildSourceId(best.Path, best.FaceIndex),
                FilePath: best.Path,
                FaceIndex: best.FaceIndex,
                ConfiguredPath: _fontPath);
        }

        throw CreateFontResolutionException(
            $"Configured font path '{_fontPath}' does not exist.",
            requested,
            configuredPath: _fontPath);
    }

    private bool ValidateSingleFileFont()
    {
        SKTypeface? typeface = null;
        try
        {
            typeface = _typefaceFactory.FromFile(_fontPath);
            if (typeface is null)
            {
                throw CreateFontResolutionException(
                    $"Failed to load font file '{_fontPath}'.",
                    requested: null,
                    configuredPath: _fontPath,
                    resolvedPath: _fontPath);
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw CreateFontResolutionException(
                $"Failed to load font file '{_fontPath}': {exception.Message}",
                requested: null,
                configuredPath: _fontPath,
                resolvedPath: _fontPath);
        }
        finally
        {
            if (typeface is not null && !IsDefaultTypeface(typeface))
            {
                typeface.Dispose();
            }
        }

        return true;
    }

    private static string BuildSourceId(string filePath, int faceIndex)
    {
        return faceIndex > 0 ? $"{filePath}#{faceIndex}" : filePath;
    }

    private static bool IsDefaultTypeface(SKTypeface typeface)
    {
        return ReferenceEquals(typeface, SKTypeface.Default) || typeface.Handle == SKTypeface.Default.Handle;
    }

    private static InvalidOperationException CreateFontResolutionException(
        string message,
        FontKey? requested,
        string configuredPath,
        string? resolvedPath = null)
    {
        var exception = new InvalidOperationException(message);
        exception.Data["DiagnosticsName"] = "FontPath";
        exception.Data["FontConfiguredPath"] = configuredPath;

        if (requested is not null)
        {
            exception.Data["RequestedFamily"] = requested.Family;
            exception.Data["RequestedWeight"] = requested.Weight;
            exception.Data["RequestedStyle"] = requested.Style;
        }

        if (!string.IsNullOrWhiteSpace(resolvedPath))
        {
            exception.Data["FontResolvedPath"] = resolvedPath;
        }

        return exception;
    }

    private sealed class DefaultTypefaceFactory : ISkiaTypefaceFactory
    {
        public SKTypeface? FromFile(string path) => SKTypeface.FromFile(path);

        public SKTypeface? FromFile(string path, int faceIndex) => SKTypeface.FromFile(path, faceIndex);

        public SKTypeface? FromFamilyName(string family, SKFontStyle style) => SKTypeface.FromFamilyName(family, style);
    }
}
