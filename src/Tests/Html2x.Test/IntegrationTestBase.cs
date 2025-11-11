using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace html2x.IntegrationTest;

[CollectionDefinition("PdfRendering", DisableParallelization = true)]
public abstract class IntegrationTestBase(ITestOutputHelper output)
{
    protected async Task SavePdfForInspectionAsync(
        byte[] pdfBytes,
        string? fileName = null,
        [CallerMemberName] string? callerName = null)
    {
        var baseName = string.IsNullOrWhiteSpace(fileName)
            ? callerName ?? "output"
            : fileName;

        var safeFileName = MakeSafeFileName(baseName);
        var finalFileName = EnsurePdfExtension(safeFileName);
        var tempPath = Path.Combine(Path.GetTempPath(), finalFileName);

        await File.WriteAllBytesAsync(tempPath, pdfBytes);
        output.WriteLine($"PDF saved to: {tempPath}");
    }

    private static string MakeSafeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(ch => invalidChars.Contains(ch) ? '_' : ch)).Trim();
    }

    private static string EnsurePdfExtension(string fileName)
    {
        return fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{fileName}.pdf";
    }
}