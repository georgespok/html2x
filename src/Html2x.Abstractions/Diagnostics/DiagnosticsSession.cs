using Html2x.Abstractions.Options;

namespace Html2x.Abstractions.Diagnostics
{
    public class DiagnosticsSession
    {
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }

        public HtmlConverterOptions Options { get; set; } = null!;
        public List<DiagnosticsEvent> Events { get; } = [];
    }
}
