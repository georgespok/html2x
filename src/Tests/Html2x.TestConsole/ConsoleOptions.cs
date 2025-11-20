namespace Html2x.TestConsole;

internal readonly record struct ConsoleOptions(
    string InputPath,
    string OutputPath,
    bool DiagnosticsEnabled,
    string? DiagnosticsJson);
