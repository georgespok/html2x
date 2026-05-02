using System.Text.Json;
using Html2x.Diagnostics;
using Html2x.Diagnostics.Contracts;
using Shouldly;

namespace Html2x.TestConsole.Test;

public sealed class TestConsoleDiagnosticsSerializerTests
{
    [Fact]
    public void ToJson_RunContextAndSession_WritesWrappedReproductionEnvelope()
    {
        var startTime = DateTimeOffset.Parse("2026-04-14T10:00:00Z");
        var endTime = DateTimeOffset.Parse("2026-04-14T10:00:01Z");
        var report = new DiagnosticsReport(
            startTime,
            endTime,
            [
                new DiagnosticRecord(
                    "LayoutBuild",
                    "stage/started",
                    DiagnosticSeverity.Info,
                    null,
                    null,
                    DiagnosticFields.Empty,
                    startTime)
            ]);
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

        var json = TestConsoleDiagnosticsSerializer.ToJson(report, options);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.TryGetProperty("testConsole", out var testConsole).ShouldBeTrue();
        root.TryGetProperty("environment", out var environment).ShouldBeTrue();
        root.TryGetProperty("diagnosticsReport", out var diagnosticsReport).ShouldBeTrue();
        root.TryGetProperty("startTime", out _).ShouldBeFalse();

        testConsole.GetProperty("inputPath").GetString().ShouldBe(options.InputPath);
        testConsole.GetProperty("outputPath").GetString().ShouldBe(options.OutputPath);
        testConsole.GetProperty("diagnosticsEnabled").GetBoolean().ShouldBeFalse();
        testConsole.GetProperty("diagnosticsActive").GetBoolean().ShouldBeTrue();
        testConsole.GetProperty("diagnosticsJsonPath").GetString().ShouldBe(options.DiagnosticsJson);
        testConsole.GetProperty("enableDebugging").GetBoolean().ShouldBeTrue();
        testConsole.GetProperty("interactive").GetBoolean().ShouldBeTrue();
        testConsole.GetProperty("selectedSamplePath").GetString().ShouldBe(options.SelectedSamplePath);

        var rawArguments = testConsole.GetProperty("rawArguments").EnumerateArray()
            .Select(static argument => argument.GetString()!)
            .ToArray();
        rawArguments.ShouldBe(options.RawArguments.ToArray());

        environment.GetProperty("workingDirectory").GetString().ShouldNotBeNullOrWhiteSpace();
        environment.GetProperty("applicationBaseDirectory").GetString().ShouldNotBeNullOrWhiteSpace();
        environment.GetProperty("osDescription").GetString().ShouldNotBeNullOrWhiteSpace();
        environment.GetProperty("frameworkDescription").GetString().ShouldNotBeNullOrWhiteSpace();
        environment.GetProperty("processArchitecture").GetString().ShouldNotBeNullOrWhiteSpace();

        diagnosticsReport.GetProperty("startTime").GetDateTimeOffset().ShouldBe(report.StartTime);
        diagnosticsReport.GetProperty("endTime").GetDateTimeOffset().ShouldBe(report.EndTime);
        diagnosticsReport.GetProperty("records")[0].GetProperty("name").GetString().ShouldBe("stage/started");
    }

    [Fact]
    public void ToJson_FeatureDiagnosticsSample_WritesOwnerConsumerEvidence()
    {
        var startTime = DateTimeOffset.Parse("2026-04-23T10:00:00Z");
        var endTime = DateTimeOffset.Parse("2026-04-23T10:00:02Z");
        var report = new DiagnosticsReport(
            startTime,
            endTime,
            [
                Record(
                    "stage/box-tree",
                    "layout/margin-collapse",
                    DiagnosticFields.Create(
                        DiagnosticFields.Field("owner", "BlockFormattingContext"),
                        DiagnosticFields.Field("consumer", "InlineLayoutEngine"),
                        DiagnosticFields.Field("formattingContext", "InlineBlock"),
                        DiagnosticFields.Field("previousBottomMargin", 12f),
                        DiagnosticFields.Field("nextTopMargin", 4f),
                        DiagnosticFields.Field("collapsedTopMargin", 12f))),
                Record(
                    "stage/render",
                    "font/resolve",
                    DiagnosticFields.Create(
                        DiagnosticFields.Field("owner", "FontPathSource"),
                        DiagnosticFields.Field("consumer", "SkiaTextMeasurer"),
                        DiagnosticFields.Field("requestedFamily", "Inter"),
                        DiagnosticFields.Field("requestedWeight", "W400"),
                        DiagnosticFields.Field("requestedStyle", "Normal"),
                        DiagnosticFields.Field("resolvedFamily", "Inter"),
                        DiagnosticFields.Field("resolvedWeight", "W400"),
                        DiagnosticFields.Field("resolvedStyle", "Normal"),
                        DiagnosticFields.Field("sourceId", "file:Inter-Regular.ttf"),
                        DiagnosticFields.Field("outcome", "Resolved"))),
                Record(
                    "LayoutBuild",
                    "stage/succeeded",
                    DiagnosticFields.Create(
                        DiagnosticFields.Field("snapshot", DiagnosticObject.Create(
                            DiagnosticObject.Field("pages", DiagnosticArray.Create(
                                DiagnosticObject.Create(
                                    DiagnosticObject.Field("fragments", DiagnosticArray.Create(
                                        DiagnosticObject.Create(
                                            DiagnosticObject.Field("metadataOwner", "FragmentBuilder"),
                                            DiagnosticObject.Field("metadataConsumer", "LayoutSnapshotMapper")))))))))))
            ]);

        var diagnosticsJson = Path.Combine("build", "diagnostics", "centralize-layout-font-policy.json");
        var options = new ConsoleOptions(
            Path.GetFullPath(Path.Combine("html", "centralize-layout-font-policy.html")),
            Path.GetFullPath(Path.Combine("build", "centralize-layout-font-policy.pdf")),
            DiagnosticsEnabled: true,
            DiagnosticsJson: diagnosticsJson,
            EnableDebugging: false,
            RawArguments:
            [
                "--diagnostics",
                "--diagnostics-json",
                diagnosticsJson
            ],
            Interactive: false,
            SelectedSamplePath: null);

        var json = TestConsoleDiagnosticsSerializer.ToJson(report, options);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var testConsole = root.GetProperty("testConsole");
        testConsole.GetProperty("diagnosticsEnabled").GetBoolean().ShouldBeTrue();
        testConsole.GetProperty("diagnosticsActive").GetBoolean().ShouldBeTrue();
        testConsole.GetProperty("diagnosticsJsonPath").GetString().ShouldBe(diagnosticsJson);

        var policyOwnership = root.GetProperty("policyOwnership");
        policyOwnership.GetProperty("requiredEvidence").EnumerateArray()
            .Select(static item => item.GetString())
            .ShouldContain("owner");
        policyOwnership.GetProperty("requiredEvidence").EnumerateArray()
            .Select(static item => item.GetString())
            .ShouldContain("consumer");
        policyOwnership.GetProperty("approvedExceptionPaths").EnumerateArray()
            .Select(static item => item.GetString())
            .ShouldBe(["block-formatting:inline-block-descendant-implicit-width"]);

        var records = root.GetProperty("diagnosticsReport").GetProperty("records").EnumerateArray().ToList();
        var margin = records.Single(static item => item.GetProperty("name").GetString() == "layout/margin-collapse");
        margin.GetProperty("fields").GetProperty("owner").GetString().ShouldBe("BlockFormattingContext");
        margin.GetProperty("fields").GetProperty("consumer").GetString().ShouldBe("InlineLayoutEngine");

        var font = records.Single(static item => item.GetProperty("name").GetString() == "font/resolve");
        font.GetProperty("fields").GetProperty("owner").GetString().ShouldBe("FontPathSource");
        font.GetProperty("fields").GetProperty("consumer").GetString().ShouldBe("SkiaTextMeasurer");

        var layout = records.Single(static item => item.GetProperty("stage").GetString() == "LayoutBuild");
        var fragment = layout
            .GetProperty("fields")
            .GetProperty("snapshot")
            .GetProperty("pages")[0]
            .GetProperty("fragments")[0];
        fragment.GetProperty("metadataOwner").GetString().ShouldBe("FragmentBuilder");
        fragment.GetProperty("metadataConsumer").GetString().ShouldBe("LayoutSnapshotMapper");
    }

    private static DiagnosticRecord Record(
        string stage,
        string name,
        DiagnosticFields fields) =>
        new(
            stage,
            name,
            DiagnosticSeverity.Info,
            null,
            null,
            fields,
            DateTimeOffset.Parse("2026-04-23T10:00:01Z"));
}
