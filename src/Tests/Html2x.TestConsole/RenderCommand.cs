using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Html2x.TestConsole;

[Description("Convert HTML into a PDF using the Html2x pipeline.")]
internal sealed class RenderCommand : AsyncCommand<RenderSettings>
{
    private const string HtmlSamplesFolder = "html";

    public override Task<int> ExecuteAsync(CommandContext context, RenderSettings settings, CancellationToken cancellationToken)
    {
        var inputPath = settings.Input;

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            var resolution = ResolveInputInteractively();
            if (resolution.Status == InputResolutionStatus.Cancelled)
            {
                return Task.FromResult(0);
            }

            if (resolution.Status == InputResolutionStatus.Failed)
            {
                return Task.FromResult(1);
            }

            inputPath = resolution.Path;
        }

        var outputPath = settings.Output ?? "output.pdf";
        var options = new ConsoleOptions(inputPath!, outputPath, settings.Diagnostics, settings.DiagnosticsJson);
        var service = new HtmlConversionService(options);
        return service.ExecuteAsync();
    }

    private static InputResolution ResolveInputInteractively()
    {
        if (Console.IsInputRedirected)
        {
            AnsiConsole.MarkupLine("[red]Input path is required when standard input is redirected.[/]");
            return InputResolution.Failed();
        }

        var samplesDirectory = Path.Combine(AppContext.BaseDirectory, HtmlSamplesFolder);
        if (!Directory.Exists(samplesDirectory))
        {
            AnsiConsole.MarkupLine($"[red]Sample folder '{samplesDirectory}' not found.[/]");
            return InputResolution.Failed();
        }

        var samples = EnumerateSamples(samplesDirectory);
        if (samples.Count == 0)
        {
            AnsiConsole.MarkupLine($"[red]No .html files were found under '{samplesDirectory}'.[/]");
            return InputResolution.Failed();
        }

        AnsiConsole.MarkupLine("[grey]No input file supplied. Choose one of the bundled samples.[/]");
        AnsiConsole.MarkupLine("Use ↑/↓ to navigate, Enter to convert, or choose Quit (Ctrl+C)");

        var selection = PromptForSample(samples);
        if (selection.IsCancel)
        {
            AnsiConsole.MarkupLine("[yellow]No file selected. Exiting.[/]");
            return InputResolution.Cancelled();
        }

        return InputResolution.Success(selection.FullPath!);
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

    private sealed record InputResolution(string? Path, InputResolutionStatus Status)
    {
        public static InputResolution Success(string path) => new(path, InputResolutionStatus.Success);
        public static InputResolution Cancelled() => new(null, InputResolutionStatus.Cancelled);
        public static InputResolution Failed() => new(null, InputResolutionStatus.Failed);
    }

    private enum InputResolutionStatus
    {
        Success,
        Cancelled,
        Failed
    }
}
