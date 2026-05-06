namespace Html2x.RenderModel.Styles;

public readonly record struct ColorRgba(byte R, byte G, byte B, byte A)
{
    public static readonly ColorRgba Black = new(0, 0, 0, 255);
    public static readonly ColorRgba Transparent = new(0, 0, 0, 0);

    public string ToHex(bool includeAlpha = true) =>
        includeAlpha && A != 255
            ? $"#{R:X2}{G:X2}{B:X2}{A:X2}"
            : $"#{R:X2}{G:X2}{B:X2}";
}