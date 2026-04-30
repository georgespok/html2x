using Html2x.Diagnostics;

namespace Html2x
{
    public class Html2PdfResult(byte[] pdfBytes)
    {
        public byte[] PdfBytes { get; init; } = pdfBytes;
        public DiagnosticsReport? DiagnosticsReport { get; init; }
    }
}
