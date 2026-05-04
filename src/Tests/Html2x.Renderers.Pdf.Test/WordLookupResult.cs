using Html2x.RenderModel;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace Html2x.Renderers.Pdf.Test;


/// <summary>
///     Result of word lookup operations.
/// </summary>
public class WordLookupResult(Dictionary<string, Word?> words)
{
    private readonly Dictionary<string, Word?> _words = words;

    /// <summary>
    ///     Gets a word by its search text, throwing if not found.
    /// </summary>
    public Word GetWord(string searchText)
    {
        var word = _words.GetValueOrDefault(searchText) ??
                   throw new InvalidOperationException($"Word '{searchText}' should be found in the PDF");
        return word;
    }
}
