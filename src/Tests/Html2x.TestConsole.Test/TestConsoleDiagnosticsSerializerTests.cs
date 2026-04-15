using System.Text.Json;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Options;
using Html2x.TestConsole;
using Shouldly;

namespace Html2x.TestConsole.Test;

public sealed class TestConsoleDiagnosticsSerializerTests
{
    [Fact]
    public void ToJson_RunContextAndSession_WritesWrappedReproductionEnvelope()
    {
        var session = new DiagnosticsSession
        {
            StartTime = DateTimeOffset.Parse("2026-04-14T10:00:00Z"),
            EndTime = DateTimeOffset.Parse("2026-04-14T10:00:01Z"),
            Options = new HtmlConverterOptions()
        };
        session.Events.Add(DiagnosticsEventFactory.StageStarted("LayoutBuild"));
        var options = new ConsoleOptions(
            Path.GetFullPath("input.html"),
            Path.Combine(Path.GetTempPath(), "output.pdf"),
            DiagnosticsEnabled: false,
            DiagnosticsJson: "build/diagnostics/session.json",
            EnableDebugging: true,
            RawArguments:
            [
                "input.html",
                "output.pdf",
                "--diagnostics-json",
                "build/diagnostics/session.json",
                "--debug"
            ],
            Interactive: true,
            SelectedSamplePath: Path.GetFullPath("sample.html"));

        var json = TestConsoleDiagnosticsSerializer.ToJson(session, options);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.TryGetProperty("testConsole", out var testConsole).ShouldBeTrue();
        root.TryGetProperty("environment", out var environment).ShouldBeTrue();
        root.TryGetProperty("diagnosticsSession", out var diagnosticsSession).ShouldBeTrue();
        root.TryGetProperty("startTime", out _).ShouldBeFalse();

        testConsole.GetProperty("inputPath").GetString().ShouldBe(options.InputPath);
        testConsole.GetProperty("outputPath").GetString().ShouldBe(options.OutputPath);
        testConsole.GetProperty("diagnosticsEnabled").GetBoolean().ShouldBeFalse();
        testConsole.GetProperty("diagnosticsActive").GetBoolean().ShouldBeTrue();
        testConsole.GetProperty("enableDebugging").GetBoolean().ShouldBeTrue();
        testConsole.GetProperty("interactive").GetBoolean().ShouldBeTrue();
        testConsole.GetProperty("selectedSamplePath").GetString().ShouldBe(options.SelectedSamplePath);
        testConsole.TryGetProperty("diagnosticsJson", out _).ShouldBeFalse();

        var rawArguments = testConsole.GetProperty("rawArguments").EnumerateArray()
            .Select(static argument => argument.GetString()!)
            .ToArray();
        rawArguments.ShouldBe(options.RawArguments.ToArray());

        environment.GetProperty("workingDirectory").GetString().ShouldNotBeNullOrWhiteSpace();
        environment.GetProperty("applicationBaseDirectory").GetString().ShouldNotBeNullOrWhiteSpace();
        environment.GetProperty("osDescription").GetString().ShouldNotBeNullOrWhiteSpace();
        environment.GetProperty("frameworkDescription").GetString().ShouldNotBeNullOrWhiteSpace();
        environment.GetProperty("processArchitecture").GetString().ShouldNotBeNullOrWhiteSpace();

        diagnosticsSession.GetProperty("startTime").GetDateTimeOffset().ShouldBe(session.StartTime);
        diagnosticsSession.GetProperty("endTime").GetDateTimeOffset().ShouldBe(session.EndTime);
        diagnosticsSession.GetProperty("events")[0].GetProperty("name").GetString().ShouldBe("LayoutBuild");
    }
}
