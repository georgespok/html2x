namespace Html2x.TestConsole;

internal static class OptionsParser
{
    public static bool TryParse(string[] args, out ConsoleOptions options, out string? error)
    {
        var positional = new List<string>();
        var diagnosticsEnabled = false;
        string? diagnosticsJson = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--diagnostics", StringComparison.OrdinalIgnoreCase))
            {
                diagnosticsEnabled = true;
                continue;
            }

            if (string.Equals(arg, "--diagnostics-json", StringComparison.OrdinalIgnoreCase))
            {
                diagnosticsEnabled = true;
                if (i + 1 >= args.Length)
                {
                    options = default!;
                    error = "Missing path after --diagnostics-json.";
                    return false;
                }

                diagnosticsJson = args[++i];
                continue;
            }

            positional.Add(arg);
        }

        if (positional.Count < 1)
        {
            options = default!;
            error = "Input HTML path is required.";
            return false;
        }

        var input = positional[0];
        var output = positional.Count > 1 ? positional[1] : "output.pdf";

        options = new ConsoleOptions(input, output, diagnosticsEnabled, diagnosticsJson);
        error = null;
        return true;
    }

    public static void ShowUsage(string? errorMessage)
    {
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(errorMessage);
            Console.ResetColor();
            Console.WriteLine();
        }

        Console.WriteLine("Usage: Html2x.TestConsole <input.html> [output.pdf] [--diagnostics] [--diagnostics-json <path>]");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  Html2x.TestConsole example.html");
        Console.WriteLine("  Html2x.TestConsole example.html output.pdf");
        Console.WriteLine("  Html2x.TestConsole example.html output.pdf --diagnostics");
        Console.WriteLine("  Html2x.TestConsole example.html output.pdf --diagnostics --diagnostics-json diagnostics/session.json");
        Console.WriteLine();
    }
}

internal readonly record struct ConsoleOptions(
    string InputPath,
    string OutputPath,
    bool DiagnosticsEnabled,
    string? DiagnosticsJson);
