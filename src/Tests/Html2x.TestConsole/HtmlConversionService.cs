using Html2x.Abstractions.Options;
using Html2x.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Html2x.TestConsole;

internal sealed class HtmlConversionService(ConsoleOptions options)
{
    public async Task<int> ExecuteAsync()
    {
        var inputPath = Path.GetFullPath(options.InputPath);
        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"Error: Input file '{inputPath}' not found.");
            return 1;
        }

        var outputPath = ResolveOutputPath(options.OutputPath);
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

            if (!string.IsNullOrWhiteSpace(options.DiagnosticsJson) && result.Diagnostics is { } session)
            {
                var diagnosticsPath = Path.GetFullPath(options.DiagnosticsJson);
                Directory.CreateDirectory(Path.GetDirectoryName(diagnosticsPath)!);

                var json = DiagnosticsSessionSerializer.ToJson(session);
                await File.WriteAllTextAsync(diagnosticsPath, json);

                logger.LogInformation("Diagnostics written to {DiagnosticsPath}", diagnosticsPath);
            }

            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rendering failed.");
            return 1;
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

    private static string ResolveOutputPath(string requestedPath)
    {
        if (Path.IsPathRooted(requestedPath))
        {
            return requestedPath;
        }

        var tempDir = Path.GetTempPath();
        return Path.Combine(tempDir, requestedPath);
    }

    private static async Task<Html2PdfResult> RenderPdfAsync(
        HtmlConverter converter,
        string htmlContent,
        ConsoleOptions consoleOptions)
    {
        var options = new HtmlConverterOptions
        {
            Pdf = new PdfOptions
            {
                FontPath = "\\fonts\\Inter-Regular.ttf",
                EnableDebugging = true
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
