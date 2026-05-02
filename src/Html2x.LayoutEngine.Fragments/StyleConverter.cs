using Html2x.RenderModel;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Fragments;

public static class StyleConverter
{
    public static VisualStyle FromComputed(ComputedStyle s)
    {
        var hasBorders = s.Borders?.HasAny == true;

        return new VisualStyle(
            BackgroundColor: s.BackgroundColor,
            Borders: hasBorders ? s.Borders : null,
            Color: s.Color,
            Margin: s.Margin,
            Padding: s.Padding,
            WidthPt: s.WidthPt,
            HeightPt: s.HeightPt,
            Display: s.Display
        );
    }

}
