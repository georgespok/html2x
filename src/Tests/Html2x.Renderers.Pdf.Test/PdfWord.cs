using Html2x.RenderModel;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

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
