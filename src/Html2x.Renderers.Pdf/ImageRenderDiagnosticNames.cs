namespace Html2x.Renderers.Pdf;

internal static class ImageRenderDiagnosticNames
{
    public static class Stages
    {
        public const string Render = "stage/render";
    }

    public static class Events
    {
        public const string Render = "image/render";
    }

    public static class Fields
    {
        public const string Src = "src";
        public const string Status = "status";
        public const string RenderedWidth = "renderedWidth";
        public const string RenderedHeight = "renderedHeight";
        public const string Borders = "borders";
        public const string Top = "top";
        public const string Right = "right";
        public const string Bottom = "bottom";
        public const string Left = "left";
        public const string Width = "width";
        public const string Color = "color";
        public const string LineStyle = "lineStyle";
    }

    public static class Context
    {
        public const string ImageElement = "img";
    }
}
