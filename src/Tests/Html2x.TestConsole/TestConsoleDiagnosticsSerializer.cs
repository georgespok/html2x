using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using Html2x.Diagnostics;

namespace Html2x.TestConsole;

internal static class TestConsoleDiagnosticsSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string ToJson(DiagnosticsReport report, ConsoleOptions options)
    {
        if (report is null)
        {
            throw new ArgumentNullException(nameof(report));
        }

        var envelope = new TestConsoleDiagnosticsEnvelope(
            TestConsoleRunDiagnostics.From(options),
            TestConsoleEnvironmentDiagnostics.Capture(),
            PolicyOwnershipDiagnostics.CreateDefault(),
            DiagnosticsReportSerializer.ToSerializableObject(report));

        return JsonSerializer.Serialize(envelope, JsonOptions);
    }
}

internal sealed record TestConsoleDiagnosticsEnvelope(
    TestConsoleRunDiagnostics TestConsole,
    TestConsoleEnvironmentDiagnostics Environment,
    PolicyOwnershipDiagnostics PolicyOwnership,
    object DiagnosticsReport);

internal sealed record TestConsoleRunDiagnostics(
    string InputPath,
    string OutputPath,
    bool DiagnosticsEnabled,
    bool DiagnosticsActive,
    string? DiagnosticsJsonPath,
    bool EnableDebugging,
    bool Interactive,
    string? SelectedSamplePath,
    IReadOnlyList<string> RawArguments)
{
    public static TestConsoleRunDiagnostics From(ConsoleOptions options)
    {
        return new TestConsoleRunDiagnostics(
            options.InputPath,
            options.OutputPath,
            options.DiagnosticsEnabled,
            options.DiagnosticsEnabled || !string.IsNullOrWhiteSpace(options.DiagnosticsJson),
            options.DiagnosticsJson,
            options.EnableDebugging,
            options.Interactive,
            options.SelectedSamplePath,
            options.RawArguments.ToArray());
    }
}

internal sealed record PolicyOwnershipDiagnostics(
    IReadOnlyList<string> RequiredEvidence,
    IReadOnlyList<string> ApprovedExceptionPaths)
{
    public static PolicyOwnershipDiagnostics CreateDefault()
    {
        return new PolicyOwnershipDiagnostics(
            ["owner", "consumer", "approved-exception"],
            [
                "block-formatting:inline-block-descendant-implicit-width"
            ]);
    }
}

internal sealed record TestConsoleEnvironmentDiagnostics(
    string WorkingDirectory,
    string ApplicationBaseDirectory,
    string OsDescription,
    string FrameworkDescription,
    string ProcessArchitecture,
    string CurrentCulture,
    string CurrentUiCulture)
{
    public static TestConsoleEnvironmentDiagnostics Capture()
    {
        return new TestConsoleEnvironmentDiagnostics(
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory,
            RuntimeInformation.OSDescription,
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            CultureInfo.CurrentCulture.Name,
            CultureInfo.CurrentUICulture.Name);
    }
}
