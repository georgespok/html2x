namespace Html2x.LayoutEngine;

/// <summary>
///     Centralized constants for HTML element tag names, CSS property names,
///     CSS value strings, and CSS class names to avoid hardcoded strings.
/// </summary>
public static class HtmlCssConstants
{
    /// <summary>
    ///     HTML element tag names (case-insensitive in practice).
    /// </summary>
    public static class HtmlTags
    {
        public const string Body = "body";
        public const string H1 = "h1";
        public const string H2 = "h2";
        public const string H3 = "h3";
        public const string H4 = "h4";
        public const string H5 = "h5";
        public const string H6 = "h6";
        public const string P = "p";
        public const string Span = "span";
        public const string Div = "div";
        public const string Table = "table";
        public const string Tr = "tr";
        public const string Td = "td";
        public const string Th = "th";
        public const string Img = "img";
        public const string Hr = "hr";
        public const string Br = "br";
        public const string Ul = "ul";
        public const string Ol = "ol";
        public const string Li = "li";
        public const string Section = "section";
        public const string Main = "main";
        public const string Header = "header";
        public const string Footer = "footer";
        public const string B = "b";
        public const string I = "i";
        public const string Strong = "strong";
    }

    /// <summary>
    ///     HTML attribute names.
    /// </summary>
    public static class HtmlAttributes
    {
        public const string Style = "style";
        public const string Class = "class";
        public const string Src = "src";
    }

    /// <summary>
    ///     CSS property names.
    /// </summary>
    public static class CssProperties
    {
        public const string Margin = "margin";
        public const string MarginTop = "margin-top";
        public const string MarginRight = "margin-right";
        public const string MarginBottom = "margin-bottom";
        public const string MarginLeft = "margin-left";
        public const string Padding = "padding";
        public const string PaddingTop = "padding-top";
        public const string PaddingRight = "padding-right";
        public const string PaddingBottom = "padding-bottom";
        public const string PaddingLeft = "padding-left";
        public const string BorderWidth = "border-width";
        public const string BorderStyle = "border-style";
        public const string BorderColor = "border-color";
        public const string FontFamily = "font-family";
        public const string FontSize = "font-size";
        public const string FontWeight = "font-weight";
        public const string FontStyle = "font-style";
        public const string TextAlign = "text-align";
        public const string LineHeight = "line-height";
        public const string Color = "color";
        public const string BackgroundColor = "background-color";
        public const string Width = "width";
        public const string Height = "height";
        public const string MaxWidth = "max-width";
        public const string MinWidth = "min-width";
        public const string MaxHeight = "max-height";
        public const string MinHeight = "min-height";
    }

    /// <summary>
    ///     CSS shorthand property names.
    /// </summary>
    public static class CssShorthand
    {
        public const string Font = "font";
    }

    /// <summary>
    ///     CSS value strings.
    /// </summary>
    public static class CssValues
    {
        public const string Bold = "bold";
        public const string Italic = "italic";
        public const string Oblique = "oblique";
        public const string Left = "left";
        public const string Right = "right";
        public const string None = "none";
        public const string Zero = "0";
        public const string Solid = "solid";
        public const string Dashed = "dashed";
        public const string Dotted = "dotted";
    }

    /// <summary>
    ///     CSS unit strings.
    /// </summary>
    public static class CssUnits
    {
        public const string Pt = "pt";
        public const string Px = "px";
    }

    /// <summary>
    ///     CSS class names.
    /// </summary>
    public static class CssClasses
    {
        public const string Hero = "hero";
    }

    /// <summary>
    ///     Default style values.
    /// </summary>
    public static class Defaults
    {
        public const string FontFamily = "Arial";
        public const string Color = "#000000";
        public const string TextAlign = "left";
        public const string FloatDirection = "none";
        public const int DefaultFontSizePt = 12;
    }
}
