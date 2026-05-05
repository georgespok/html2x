namespace Html2x.Text;

internal static class FontDiagnosticNames
{
    public static class Stages
    {
        public const string Font = "stage/font";
    }

    public static class Events
    {
        public const string Resolve = "font/resolve";
    }

    public static class Fields
    {
        public const string Owner = "owner";
        public const string Consumer = "consumer";
        public const string RequestedFamily = "requestedFamily";
        public const string RequestedWeight = "requestedWeight";
        public const string RequestedStyle = "requestedStyle";
        public const string ResolvedFamily = "resolvedFamily";
        public const string ResolvedWeight = "resolvedWeight";
        public const string ResolvedStyle = "resolvedStyle";
        public const string SourceId = "sourceId";
        public const string ConfiguredPath = "configuredPath";
        public const string FilePath = "filePath";
        public const string FaceIndex = "faceIndex";
        public const string Outcome = "outcome";
        public const string Reason = "reason";
    }

    public static class Outcomes
    {
        public const string Resolved = "Resolved";
        public const string Failed = "Failed";
    }
}
