using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Html2x.TestConsole;

[Description("Convert HTML into a PDF using the Html2x pipeline.")]
internal sealed class RenderCommand : AsyncCommand<RenderSettings>
{
    private const string HtmlSamplesFolder = "html";

        public override async Task<int> ExecuteAsync(CommandContext context, RenderSettings settings, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(settings.Input))
            {
                return await RunConversionAsync(settings, settings.Input!);
            }
    
            return await RunInteractiveLoopAsync(settings, cancellationToken);
        }
    
        private static async Task<int> RunConversionAsync(RenderSettings settings, string inputPath)
        {
            var outputPath = ResolveOutputPath(settings, inputPath);
            var options = new ConsoleOptions(inputPath, outputPath, settings.Diagnostics, 
                settings.DiagnosticsJson, settings.EnableDebugging);
            var service = new HtmlConversionService(options);
    
            var (result, actualOutputPath) = await service.ExecuteAsync().ConfigureAwait(false);
            
            if (result == 0)
            {
                OpenInBrowser(inputPath); // inputPath is already absolute due to ConsoleOptions
                if (actualOutputPath != null) // Ensure actualOutputPath is not null
                {
                    OpenInBrowser(actualOutputPath);
                }
            }
            
            return result;
        }
    
        private static void OpenInBrowser(string path)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Could not open '{path}': {ex.Message}[/]");
            }
        }
    private static async Task<int> RunInteractiveLoopAsync(RenderSettings settings, CancellationToken cancellationToken)
    {
        if (Console.IsInputRedirected)
        {
            AnsiConsole.MarkupLine("[red]Input path is required when standard input is redirected.[/]");
            return 1;
        }

        var samplesDirectory = Path.Combine(AppContext.BaseDirectory, HtmlSamplesFolder);
        if (!Directory.Exists(samplesDirectory))
        {
            AnsiConsole.MarkupLine($"[red]Sample folder '{samplesDirectory}' not found.[/]");
            return 1;
        }

        var samples = EnumerateSamples(samplesDirectory);
        if (samples.Count == 0)
        {
            AnsiConsole.MarkupLine($"[red]No .html files were found under '{samplesDirectory}'.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine("[grey]No input file supplied. Choose one of the bundled samples.[/]");
        AnsiConsole.MarkupLine("Use ↑/↓ to navigate, Enter to convert, or choose Quit (Ctrl+C)");

        while (!cancellationToken.IsCancellationRequested)
        {
            var selection = PromptForSample(samples);
            if (selection.IsCancel)
            {
                AnsiConsole.MarkupLine("[yellow]No file selected. Exiting.[/]");
                return 0;
            }

            var result = await RunConversionAsync(settings, selection.FullPath!).ConfigureAwait(false);
            if (result != 0)
            {
                return result;
            }
        }

        return 0;
    }

    private static string ResolveOutputPath(RenderSettings settings, string? inputPath)
    {
        if (!string.IsNullOrWhiteSpace(settings.Output))
        {
            return settings.Output;
        }

        var fileName = Path.GetFileNameWithoutExtension(inputPath);
        var safeName = string.IsNullOrWhiteSpace(fileName) ? "output" : fileName;
        return $"{safeName}.pdf";
    }

    private static SampleChoice PromptForSample(IReadOnlyList<SampleChoice> samples)
    {
        var prompt = new SelectionPrompt<SampleChoice>
        {
            Title = "Select HTML sample",
            PageSize = Math.Clamp(samples.Count + 1, 1, 12),
            MoreChoicesText = "[grey](Scroll to discover more samples)[/]"
        };

        prompt.UseConverter(sample => sample.DisplayName);
        prompt.AddChoices(samples);
        prompt.AddChoice(SampleChoice.Cancel);

        try
        {
            return AnsiConsole.Prompt(prompt);
        }
        catch (OperationCanceledException)
        {
            return SampleChoice.Cancel;
        }
    }

    private static IReadOnlyList<SampleChoice> EnumerateSamples(string samplesDirectory)
    {
        var results = Directory.EnumerateFiles(samplesDirectory, "*.html", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select((path, index) =>
            {
                var relative = Path.GetRelativePath(samplesDirectory, path)
                    .Replace(Path.DirectorySeparatorChar, '/');
                return new SampleChoice($"{index + 1}. {relative}", path, false);
            })
            .ToList();

        return results;
    }

    private sealed record SampleChoice(string DisplayName, string? FullPath, bool IsCancel)
    {
        public static SampleChoice Cancel { get; } = new("Quit", null, true);
    }
}
