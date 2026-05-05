using Html2x.Diagnostics;

namespace Html2x
{
    public sealed class Html2PdfResult(byte[] pdfBytes)
    {
        private readonly byte[] _pdfBytes = pdfBytes?.ToArray() ?? throw new ArgumentNullException(nameof(pdfBytes));

        public byte[] PdfBytes => _pdfBytes.ToArray();

        public DiagnosticsReport? DiagnosticsReport { get; init; }
    }
}
