using SkiaSharp;

namespace Html2x.Text;

internal sealed class SkiaTypefaceFactory : ISkiaTypefaceFactory
{
    public SKTypeface? FromFile(string path) => SKTypeface.FromFile(path);

    public SKTypeface? FromFile(string path, int faceIndex) => SKTypeface.FromFile(path, faceIndex);

    public SKTypeface? FromFamilyName(string family, SKFontStyle style) => SKTypeface.FromFamilyName(family, style);
}
