using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;

namespace Html2x.LayoutEngine.Models;

public sealed class ComputedStyleBuilder
{
    public string FontFamily { get; set; } = HtmlCssConstants.Defaults.FontFamily;
    public float FontSizePt { get; set; } = 12;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public TextDecorations Decorations { get; set; }
    public string TextAlign { get; set; } = HtmlCssConstants.Defaults.TextAlign;
    public float? LineHeightMultiplier { get; set; }
    public ColorRgba Color { get; set; } = ColorRgba.Black;
    public ColorRgba? BackgroundColor { get; set; }
    public string? Display { get; set; }
    public Spacing Margin { get; set; }
    public Spacing Padding { get; set; }
    public float? WidthPt { get; set; }
    public float? MinWidthPt { get; set; }
    public float? MaxWidthPt { get; set; }
    public float? HeightPt { get; set; }
    public float? MinHeightPt { get; set; }
    public float? MaxHeightPt { get; set; }

    public BorderBuilder Borders { get; } = new();

    public ComputedStyle Build()
    {
        return new ComputedStyle
        {
            FontFamily = FontFamily,
            FontSizePt = FontSizePt,
            Bold = Bold,
            Italic = Italic,
            Decorations = Decorations,
            TextAlign = TextAlign,
            LineHeightMultiplier = LineHeightMultiplier ?? 1.2f,
            Color = Color,
            BackgroundColor = BackgroundColor,
            Display = Display,
            Margin = Margin,
            Padding = NormalizePadding(Padding),
            WidthPt = WidthPt,
            MinWidthPt = MinWidthPt,
            MaxWidthPt = MaxWidthPt,
            HeightPt = HeightPt,
            MinHeightPt = MinHeightPt,
            MaxHeightPt = MaxHeightPt,
            Borders = Borders.Build(Color)
        };
    }

    private static Spacing NormalizePadding(Spacing padding)
        => new(
            Math.Max(0, padding.Top),
            Math.Max(0, padding.Right),
            Math.Max(0, padding.Bottom),
            Math.Max(0, padding.Left));

    public sealed class BorderBuilder
    {
        public float? TopWidth { get; set; }
        public BorderLineStyle TopStyle { get; set; }
        public ColorRgba? TopColor { get; set; }

        public float? RightWidth { get; set; }
        public BorderLineStyle RightStyle { get; set; }
        public ColorRgba? RightColor { get; set; }

        public float? BottomWidth { get; set; }
        public BorderLineStyle BottomStyle { get; set; }
        public ColorRgba? BottomColor { get; set; }

        public float? LeftWidth { get; set; }
        public BorderLineStyle LeftStyle { get; set; }
        public ColorRgba? LeftColor { get; set; }

        public BorderEdges Build(ColorRgba currentColor)
        {
            return new BorderEdges
            {
                Top = CreateSide(TopWidth, TopStyle, TopColor, currentColor),
                Right = CreateSide(RightWidth, RightStyle, RightColor, currentColor),
                Bottom = CreateSide(BottomWidth, BottomStyle, BottomColor, currentColor),
                Left = CreateSide(LeftWidth, LeftStyle, LeftColor, currentColor)
            };
        }

        private static BorderSide? CreateSide(float? width, BorderLineStyle style, ColorRgba? color, ColorRgba currentColor)
        {
            if (!width.HasValue || width.Value <= 0 || style == BorderLineStyle.None)
            {
                return null;
            }

            return new BorderSide(width.Value, color ?? currentColor, style);
        }
    }
}
