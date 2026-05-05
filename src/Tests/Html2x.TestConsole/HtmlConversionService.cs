using Html2x.Options;
using Microsoft.Extensions.Logging;

namespace Html2x.TestConsole;

internal sealed class HtmlConversionService(ConsoleOptions options)
{
    public async Task<(int result, string? outputPath)> ExecuteAsync()
    {
        var inputPath = Path.GetFullPath(options.InputPath);
        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"Error: Input file '{inputPath}' not found.");
            return (1, null);
        }

        var outputPath = options.OutputPath;
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        using var loggerFactory = CreateLoggerFactory();
        var logger = loggerFactory.CreateLogger("Html2x.TestConsole");

        logger.LogInformation("Converting HTML to PDF.");
        logger.LogInformation("Input: {Input}", inputPath);
        logger.LogInformation("Output: {Output}", outputPath);

        var converter = new HtmlConverter();

        try
        {
            var htmlContent = await File.ReadAllTextAsync(inputPath);
            var result = await RenderPdfAsync(converter, htmlContent, options);

            await File.WriteAllBytesAsync(outputPath, result.PdfBytes);
            logger.LogInformation("PDF created successfully. Size {FileSize:N0} bytes", result.PdfBytes.Length);

            if (!string.IsNullOrWhiteSpace(options.DiagnosticsJson) && result.DiagnosticsReport is { } report)
            {
                var diagnosticsPath = Path.GetFullPath(options.DiagnosticsJson);
                Directory.CreateDirectory(Path.GetDirectoryName(diagnosticsPath)!);

                var json = TestConsoleDiagnosticsSerializer.ToJson(report, options);
                await File.WriteAllTextAsync(diagnosticsPath, json);

                logger.LogInformation("Diagnostics written to {DiagnosticsPath}", diagnosticsPath);
            }

            return (0, outputPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rendering failed.");
            return (1, null);
        }
    }

    private static ILoggerFactory CreateLoggerFactory() =>
        LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddSimpleConsole(opts =>
            {
                opts.SingleLine = true;
                opts.TimestampFormat = "HH:mm:ss ";
            });
            builder.SetMinimumLevel(LogLevel.Information);
        });

    private static async Task<Html2PdfResult> RenderPdfAsync(
        HtmlConverter converter,
        string htmlContent,
        ConsoleOptions consoleOptions)
    {
        var resourceBaseDirectory = Path.GetDirectoryName(consoleOptions.InputPath) ??
                                    Directory.GetCurrentDirectory();
        var fontPath = Path.Combine(AppContext.BaseDirectory, "fonts");

        var options = new HtmlConverterOptions
        {
            Fonts = new FontOptions
            {
                FontPath = fontPath
            },
            Resources = new ResourceOptions
            {
                BaseDirectory = resourceBaseDirectory,
                MaxImageSizeBytes = 10 * 1024 * 1024
            },
            Diagnostics = new DiagnosticsOptions
            {
                EnableDiagnostics = consoleOptions.DiagnosticsEnabled ||
                                    !string.IsNullOrWhiteSpace(consoleOptions.DiagnosticsJson)
            }
        };

        var result = await converter.ToPdfAsync(htmlContent, options);
        return result;
    }
}
