using SkiaSharp;

namespace Html2x.Renderers.Pdf.Drawing;

public interface ISkiaTypefaceFactory
{
    SKTypeface? FromFile(string path);

    SKTypeface? FromFile(string path, int faceIndex);

    SKTypeface? FromFamilyName(string family, SKFontStyle style);
}
