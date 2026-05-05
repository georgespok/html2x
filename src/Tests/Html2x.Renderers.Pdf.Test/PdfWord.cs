namespace Html2x.Renderers.Pdf.Test;


/// <summary>
///     Represents a word extracted from a PDF with its styling attributes.
/// </summary>
public record PdfWord(
    string Text,
    string HexColor,
    bool IsBold,
    bool IsItalic,
    double FontSize);
