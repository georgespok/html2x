namespace Html2x.Abstractions.Measurements.Units;

public static class PaperSizes
{
    public static readonly SizePt Letter = new(612, 792); // 8.5x11 in @ 72 DPI
    public static readonly SizePt A4 = new(595, 842); // 210x297 mm @ 72 DPI
    public static readonly SizePt Legal = new(612, 1008); // 8.5x14 in @ 72 DPI
    public static readonly SizePt A5 = new(420, 595); // 148x210 mm @ 72 DPI
    public static readonly SizePt A3 = new(842, 1191); // 297x420 mm @ 72 DPI
}
