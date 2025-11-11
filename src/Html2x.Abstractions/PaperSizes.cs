namespace Html2x.Core;

public static class PaperSizes
{
    public static readonly PageSize Letter = new(612, 792); // 8.5x11 in @ 72 DPI
    public static readonly PageSize A4 = new(595, 842); // 210x297 mm @ 72 DPI
    public static readonly PageSize Legal = new(612, 1008); // 8.5x14 in @ 72 DPI
    public static readonly PageSize A5 = new(420, 595); // 148x210 mm @ 72 DPI
    public static readonly PageSize A3 = new(842, 1191); // 297x420 mm @ 72 DPI
}