namespace Html2x.Core;

public readonly record struct Dimensions(int Width, int Height);

public static class PaperSizes
{
    public static readonly Dimensions Letter = new(612, 792); // 8.5x11 in @ 72 DPI
    public static readonly Dimensions A4 = new(595, 842); // 210x297 mm @ 72 DPI
    public static readonly Dimensions Legal = new(612, 1008); // 8.5x14 in @ 72 DPI
    public static readonly Dimensions A5 = new(420, 595); // 148x210 mm @ 72 DPI
    public static readonly Dimensions A3 = new(842, 1191); // 297x420 mm @ 72 DPI
}