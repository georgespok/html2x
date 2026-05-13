namespace Html2x;

internal static class HtmlConverterDiagnosticNames
{
    public static class Stages
    {
        public const string LayoutBuild = "LayoutBuild";
        public const string PdfRender = "PdfRender";
        public const string Configuration = "Configuration";
    }

    public static class Events
    {
        public const string FontPathError = "font-path/error";
    }

    public static class Fields
    {
        public const string Snapshot = "snapshot";
        public const string PdfSize = "pdfSize";
        public const string PageCount = "pageCount";
        public const string HtmlLength = "htmlLength";
        public const string Html = "html";
        public const string HtmlTruncated = "htmlTruncated";
    }

    public static class Messages
    {
        public const string SkippedBecauseConfigurationFailed = "Skipped because Configuration failed.";
        public const string SkippedBecauseLayoutBuildFailed = "Skipped because LayoutBuild failed.";
    }
}
