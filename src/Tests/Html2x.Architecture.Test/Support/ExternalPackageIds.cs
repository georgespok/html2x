namespace Html2x.Architecture.Test.Support;

internal static class ExternalPackageIds
{
    public const string AngleSharp = "AngleSharp";

    public const string AngleSharpCss = AngleSharp + CssPackageSuffix;

    public const string SkiaSharp = "SkiaSharp";

    public const string SkiaSharpHarfBuzz = SkiaSharp + HarfBuzzPackageSuffix;

    private const string CssPackageSuffix = ".Css";

    private const string HarfBuzzPackageSuffix = ".HarfBuzz";
}
