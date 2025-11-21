using Html2x.Abstractions.Diagnostics;

namespace Html2x
{
    public class Html2PdfResult(byte[] pdfBytes)
    {
        public byte[] PdfBytes { get; init; } = pdfBytes;
        public DiagnosticsSession? Diagnostics { get; init; }
    }
}
