using System.ComponentModel;
using Spectre.Console.Cli;

namespace Html2x.TestConsole;

internal sealed class RenderSettings : CommandSettings
{
    [CommandArgument(0, "[input]")]
    [Description("Path to the HTML file to convert.")]
    public string? Input { get; init; }

    [CommandArgument(1, "[output]")]
    [Description("Path for the generated PDF (defaults to output.pdf).")]
    public string? Output { get; init; }

    [CommandOption("--diagnostics")]
    [Description("Enable diagnostic logging and overlays.")]
    public bool Diagnostics { get; init; }

    [CommandOption("--diagnostics-json <PATH>")]
    [Description("Optional path for structured diagnostics output.")]
    public string? DiagnosticsJson { get; init; }
}
