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
            return await ConvertAsync(settings.Input, settings);
        }

        return await RunInteractiveLoopAsync(settings);
    }

    private static async Task<int> RunInteractiveLoopAsync(RenderSettings settings)
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
        AnsiConsole.MarkupLine("[grey]Use ↑/↓ to navigate, Enter to convert, or choose Quit.[/]");

        while (true)
        {
            var selection = PromptForSample(samples);
            if (selection.IsQuit)
            {
                AnsiConsole.MarkupLine("[yellow]Quit selected. Exiting.[/]");
                return 0;
            }

            var result = await ConvertAsync(selection.FullPath!, settings);
            if (result == 0)
            {
                AnsiConsole.MarkupLine("[green]Conversion complete.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Conversion failed. Review the logs before retrying.[/]");
            }

            AnsiConsole.MarkupLine("[grey]Select another file or choose Quit.[/]");
        }
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
        prompt.AddChoice(SampleChoice.Quit);

        try
        {
            return AnsiConsole.Prompt(prompt);
        }
        catch (OperationCanceledException)
        {
            return SampleChoice.Quit;
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

    private static async Task<int> ConvertAsync(string inputPath, RenderSettings settings)
    {
        var options = BuildOptions(inputPath, settings);
        var service = new HtmlConversionService(options);
        return await service.ExecuteAsync();
    }

    private static ConsoleOptions BuildOptions(string inputPath, RenderSettings settings)
    {
        var outputPath = settings.Output ?? "output.pdf";
        var diagnosticsEnabled = settings.Diagnostics || !string.IsNullOrWhiteSpace(settings.DiagnosticsJson);
        return new ConsoleOptions(inputPath, outputPath, diagnosticsEnabled, settings.DiagnosticsJson);
    }

    private sealed record SampleChoice(string DisplayName, string? FullPath, bool IsQuit)
    {
        public static SampleChoice Quit { get; } = new("Quit", null, true);
    }
}
