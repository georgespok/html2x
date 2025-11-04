using Html2x.Pdf;
using Microsoft.Extensions.Logging;

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

        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Error: Input file '{inputFile}' not found.");
            return;
        }

        string outputPath;
        if (Path.IsPathRooted(outputFile))
        {
            outputPath = outputFile;
        }
        else
        {
            var tempDir = Path.GetTempPath();
            outputPath = Path.Combine(tempDir, outputFile);
        }

        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var options = new PdfOptions { FontPath = "\\fonts\\Inter-Regular.ttf" };

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddSimpleConsole(opts =>
            {
                opts.SingleLine = true;
                opts.TimestampFormat = "HH:mm:ss ";
            });
            builder.SetMinimumLevel(LogLevel.Trace);
        });

        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogInformation("Converting HTML to PDF");
        logger.LogInformation("Input file: {Input}", Path.GetFullPath(inputFile));

        try
        {
            var htmlContent = await File.ReadAllTextAsync(inputFile);

            var converter = new HtmlConverter(loggerFactory: loggerFactory);
            var pdfBytes = await converter.ToPdfAsync(htmlContent, options);

            await File.WriteAllBytesAsync(outputPath, pdfBytes);

            logger.LogInformation("Output file: {Output}", Path.GetFullPath(outputPath));
            logger.LogInformation("PDF created successfully. Size {FileSize:N0} bytes", pdfBytes.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating PDF");
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
