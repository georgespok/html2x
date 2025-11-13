using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

using Html2x.Abstractions.Utilities.Color;
namespace Html2x.Renderers.Pdf.Test;

/// <summary>
///     Parses PDF documents to extract structured word information including text and styling attributes.
/// </summary>
public static class PdfWordParser
{
    private static readonly string DefaultBlackColor = HexColors.Black;

    /// <summary>
    ///     Extracts all words from a PDF with their styling attributes (all pages).
    /// </summary>
    public static IReadOnlyList<PdfWord> GetStyledWords(byte[] pdfBytes)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);

        using var stream = new MemoryStream(pdfBytes);
        using var pdf = PdfDocument.Open(stream);

        var words = new List<PdfWord>();
        foreach (var page in pdf.GetPages())
        foreach (var word in page.GetWords(NearestNeighbourWordExtractor.Instance))
        {
            var pdfWord = ExtractWordInfo(word);
            if (!string.IsNullOrWhiteSpace(pdfWord.Text))
            {
                words.Add(pdfWord);
            }
        }

        return words;
    }

    /// <summary>
    ///     Extracts all words from a PDF as text strings (all pages).
    /// </summary>
    public static string[] GetTextWords(byte[] pdfBytes)
    {
        return [.. GetStyledWords(pdfBytes).Select(w => w.Text)];
    }

    private static PdfWord ExtractWordInfo(Word word)
    {
        var cleanText = CleanWordText(word.Text);
        var hexColor = GetMostCommonTextColor(word);
        var isBold = IsBold(word);
        var isItalic = IsItalic(word);
        var fontSize = GetAverageFontSize(word);

        return new PdfWord(cleanText, hexColor, isBold, isItalic, fontSize);
    }

    private static string CleanWordText(string text)
    {
        return new string([.. text.Where(ch => ch != 0)]);
    }

    private static string GetMostCommonTextColor(Word word)
    {
        if (word.Letters is not { Count: > 0 } letters)
        {
            return DefaultBlackColor;
        }

        var colorCounts = letters
            .Select(GetLetterColorHex)
            .GroupBy(color => color)
            .ToDictionary(g => g.Key, g => g.Count());

        return colorCounts
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key)
            .Select(kvp => kvp.Key)
            .FirstOrDefault() ?? DefaultBlackColor;
    }

    private static string GetLetterColorHex(Letter? letter)
    {
        if (letter == null)
        {
            return DefaultBlackColor;
        }

        try
        {
            return TryGetColorHex(letter.FillColor)
                   ?? TryGetColorHex(letter.Color)
                   ?? TryGetColorHex(letter.StrokeColor)
                   ?? DefaultBlackColor;
        }
        catch
        {
            return DefaultBlackColor;
        }
    }

    private static string? TryGetColorHex(object? color)
    {
        if (color == null)
        {
            return null;
        }

        var hex = ConvertIColorToHex(color);
        return hex != DefaultBlackColor ? hex : null;
    }

    private static string ConvertIColorToHex(object color)
    {
        try
        {
            var colorString = color.ToString();
            if (colorString?.StartsWith("RGB:") != true)
            {
                return DefaultBlackColor;
            }

            return ParseRgbString(colorString);
        }
        catch
        {
            return DefaultBlackColor;
        }
    }

    private static string ParseRgbString(string rgbString)
    {
        var rgbPart = rgbString[4..].Trim('(', ')', ' ');
        var values = rgbPart.Split(',');

        if (values.Length < 3)
        {
            return DefaultBlackColor;
        }

        if (!double.TryParse(values[0].Trim(), out var r) ||
            !double.TryParse(values[1].Trim(), out var g) ||
            !double.TryParse(values[2].Trim(), out var b))
        {
            return DefaultBlackColor;
        }

        var red = (int)(r * 255);
        var green = (int)(g * 255);
        var blue = (int)(b * 255);

        return $"#{red:X2}{green:X2}{blue:X2}";
    }

    private static bool IsBold(Word word)
    {
        return word.Letters.Any(l => l.FontName?.Contains("Bold") == true ||
                                     l.FontName?.Contains("Black") == true ||
                                     l.FontName?.Contains("Heavy") == true);
    }

    private static bool IsItalic(Word word)
    {
        return word.Letters.Any(l => l.FontName?.Contains("Italic") == true ||
                                     l.FontName?.Contains("Oblique") == true);
    }

    private static double GetAverageFontSize(Word word)
    {
        if (word.Letters is not { Count: > 0 } letters)
        {
            return 0;
        }

        var fontSizes = letters
            .Select(l => l.PointSize)
            .Where(size => size > 0)
            .ToArray();

        return fontSizes.Length > 0 ? fontSizes.Average() : 0;
    }

    /// <summary>
    ///     Extracts raw PdfPig Word objects from a PDF document (first page only).
    ///     Used for positioning analysis and gap calculations.
    /// </summary>
    public static List<Word> GetRawWords(byte[] pdfBytes)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);

        using var stream = new MemoryStream(pdfBytes);
        using var pdf = PdfDocument.Open(stream);
        var page = pdf.GetPage(1);
        return [.. page.GetWords()];
    }

    /// <summary>
    ///     Finds a word by partial text match (case-insensitive).
    /// </summary>
    public static Word? FindWordByText(List<Word> words, string searchText)
    {
        return words.FirstOrDefault(w =>
            CleanWordText(w.Text).Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Finds multiple words by their partial text matches.
    /// </summary>
    public static WordLookupResult FindWords(List<Word> words, params string[] searchTexts)
    {
        var foundWords = new Dictionary<string, Word?>();

        foreach (var searchText in searchTexts)
        {
            var word = FindWordByText(words, searchText);
            foundWords[searchText] = word;
        }

        return new WordLookupResult(foundWords);
    }

    /// <summary>
    ///     Logs word positions for debugging purposes.
    /// </summary>
    public static void LogWordPositions(List<Word> words, Action<string> writeLine)
    {
        writeLine("All words found in PDF:");
        foreach (var word in words)
        {
            var cleanText = CleanWordText(word.Text);
            writeLine(
                $"  Original: '{word.Text}' -> Clean: '{cleanText}' at position ({word.BoundingBox.TopLeft.X:F1}, {word.BoundingBox.TopLeft.Y:F1})");
        }
    }
}

/// <summary>
///     Represents a word extracted from a PDF with its styling attributes.
/// </summary>
public record PdfWord(
    string Text,
    string HexColor,
    bool IsBold,
    bool IsItalic,
    double FontSize);

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
