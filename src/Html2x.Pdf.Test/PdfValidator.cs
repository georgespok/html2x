namespace Html2x.Pdf.Test;

public class PdfValidator
{
    public static bool Validate(byte[] pdfBytes)
    {
        if (pdfBytes.Length < PdfHeader.MinimumLength)
        {
            return false;
        }

        return pdfBytes[0] == PdfHeader.Percent &&
               pdfBytes[1] == PdfHeader.P &&
               pdfBytes[2] == PdfHeader.D &&
               pdfBytes[3] == PdfHeader.F;
    }

    private static class PdfHeader
    {
        public const byte Percent = 0x25; // %
        public const byte P = 0x50; // P
        public const byte D = 0x44; // D
        public const byte F = 0x46; // F
        public const int MinimumLength = 4;
    }
}