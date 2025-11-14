using Html2x.Diagnostics.Runtime;
using Html2x.Renderers.Pdf.Options;
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

        var runtime = DiagnosticsFactory.Create(options, loggerFactory, logger);
        var converter = runtime?.Decorate(new HtmlConverter(loggerFactory: loggerFactory))
                         ?? new HtmlConverter(loggerFactory: loggerFactory);

        try
        {
            var htmlContent = await File.ReadAllTextAsync(inputPath);
            var pdfBytes = await RenderPdfAsync(converter, htmlContent, runtime, inputPath, outputPath);

            await File.WriteAllBytesAsync(outputPath, pdfBytes);
            logger.LogInformation("PDF created successfully. Size {FileSize:N0} bytes", pdfBytes.Length);
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

    private static async Task<byte[]> RenderPdfAsync(
        HtmlConverter converter,
        string htmlContent,
        DiagnosticsRuntime? runtime,
        string inputPath,
        string outputPath)
    {
        var pdfOptions = new PdfOptions
        {
            FontPath = "\\fonts\\Inter-Regular.ttf",
            EnableDebugging = true
        };

        var metadata = runtime is null
            ? null
            : new Dictionary<string, object?>
            {
                ["input"] = inputPath,
                ["output"] = outputPath
            };

        using var session = runtime?.StartSession(
            $"console-{Path.GetFileNameWithoutExtension(inputPath)}",
            metadata: metadata);

        return await converter.ToPdfAsync(htmlContent, pdfOptions);
    }
}
