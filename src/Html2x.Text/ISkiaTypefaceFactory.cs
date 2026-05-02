using SkiaSharp;

namespace Html2x.Text;

internal interface ISkiaTypefaceFactory
{
    SKTypeface? FromFile(string path);

    SKTypeface? FromFile(string path, int faceIndex);

    SKTypeface? FromFamilyName(string family, SKFontStyle style);
}
