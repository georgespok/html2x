using Html2x.Pdf;

namespace Html2x.TestConsole;

internal class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length < 1)
        {
            ShowUsage();
            return;
        }

        var inputFile = args[0];
        var outputFile = args.Length > 1 ? args[1] : "output.pdf";

        // Validate input file
        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Error: Input file '{inputFile}' not found.");
            return;
        }

        // Determine output path
        string outputPath;
        if (Path.IsPathRooted(outputFile))
        {
            // Absolute path provided
            outputPath = outputFile;
        }
        else
        {
            // Relative path - create in temp folder
            var tempDir = Path.GetTempPath();
            outputPath = Path.Combine(tempDir, outputFile);
        }

        // Ensure output directory exists
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var options = new PdfOptions { FontPath = "\\fonts\\Inter-Regular.ttf" };

        Console.WriteLine("Converting HTML to PDF...");
        Console.WriteLine($"Input file: {Path.GetFullPath(inputFile)}");


        try
        {
            var htmlContent = await File.ReadAllTextAsync(inputFile);

            var pdfBytes = await new HtmlConverter().ToPdfAsync(htmlContent, options);

            await File.WriteAllBytesAsync(outputPath, pdfBytes);

            Console.WriteLine($"Output file: {Path.GetFullPath(outputPath)}");
            Console.WriteLine();

            Console.WriteLine("✅ PDF created successfully!");
            Console.WriteLine($"📄 File size: {pdfBytes.Length:N0} bytes");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating PDF: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private static void ShowUsage()
    {
        Console.WriteLine("Usage: Html2x.Pdf.TestConsole <input.html> [output.pdf]");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  Html2x.Pdf.TestConsole example.html");
        Console.WriteLine("  Html2x.Pdf.TestConsole example.html output.pdf");
        Console.WriteLine("  Html2x.Pdf.TestConsole example.html C:\\temp\\report.pdf");
        Console.WriteLine("  Html2x.Pdf.TestConsole example.html ./reports/monthly-report.pdf");
        Console.WriteLine();
    }
}