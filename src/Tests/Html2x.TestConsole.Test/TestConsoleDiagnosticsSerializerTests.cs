using System.Text.Json;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Options;
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

        diagnosticsSession.GetProperty("startTime").GetDateTimeOffset().ShouldBe(session.StartTime);
        diagnosticsSession.GetProperty("endTime").GetDateTimeOffset().ShouldBe(session.EndTime);
        diagnosticsSession.GetProperty("events")[0].GetProperty("name").GetString().ShouldBe("LayoutBuild");
    }

    [Fact]
    public void ToJson_FeatureDiagnosticsSample_WritesOwnerConsumerEvidence()
    {
        var session = new DiagnosticsSession
        {
            StartTime = DateTimeOffset.Parse("2026-04-23T10:00:00Z"),
            EndTime = DateTimeOffset.Parse("2026-04-23T10:00:02Z"),
            Options = new HtmlConverterOptions()
        };
        session.Events.Add(new DiagnosticsEvent
        {
            Type = DiagnosticsEventType.Trace,
            Name = "layout/margin-collapse",
            Payload = new MarginCollapsePayload
            {
                Owner = "BlockFormattingContext",
                Consumer = "InlineLayoutEngine",
                FormattingContext = FormattingContextKind.InlineBlock,
                PreviousBottomMargin = 12f,
                NextTopMargin = 4f,
                CollapsedTopMargin = 12f
            }
        });
        session.Events.Add(new DiagnosticsEvent
        {
            Type = DiagnosticsEventType.Trace,
            Name = "font/resolve",
            Payload = new FontResolutionPayload
            {
                Owner = "FontPathSource",
                Consumer = "SkiaTextMeasurer",
                RequestedFamily = "Inter",
                RequestedWeight = FontWeight.W400,
                RequestedStyle = FontStyle.Normal,
                ResolvedFamily = "Inter",
                ResolvedWeight = FontWeight.W400,
                ResolvedStyle = FontStyle.Normal,
                SourceId = "file:Inter-Regular.ttf",
                Outcome = "Resolved"
            }
        });
        session.Events.Add(DiagnosticsEventFactory.StageSucceeded(
            "LayoutBuild",
            new LayoutSnapshotPayload
            {
                Snapshot = new LayoutSnapshot
                {
                    PageCount = 1,
                    Pages =
                    [
                        new LayoutPageSnapshot
                        {
                            PageNumber = 1,
                            PageSize = PaperSizes.A4,
                            Margin = new Spacing(),
                            Fragments =
                            [
                                new FragmentSnapshot
                                {
                                    SequenceId = 1,
                                    Kind = "table",
                                    DisplayRole = FragmentDisplayRole.Table,
                                    FormattingContext = FormattingContextKind.Block,
                                    DerivedColumnCount = 2,
                                    MetadataOwner = "FragmentBuilder",
                                    MetadataConsumer = "LayoutSnapshotMapper"
                                }
                            ]
                        }
                    ]
                }
            }));

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

        var json = TestConsoleDiagnosticsSerializer.ToJson(session, options);

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
            .ShouldContain("block-formatting:inline-block-descendant-implicit-width");

        var events = root.GetProperty("diagnosticsSession").GetProperty("events").EnumerateArray().ToList();
        var margin = events.Single(static item => item.GetProperty("name").GetString() == "layout/margin-collapse");
        margin.GetProperty("payload").GetProperty("owner").GetString().ShouldBe("BlockFormattingContext");
        margin.GetProperty("payload").GetProperty("consumer").GetString().ShouldBe("InlineLayoutEngine");

        var font = events.Single(static item => item.GetProperty("name").GetString() == "font/resolve");
        font.GetProperty("payload").GetProperty("owner").GetString().ShouldBe("FontPathSource");
        font.GetProperty("payload").GetProperty("consumer").GetString().ShouldBe("SkiaTextMeasurer");

        var layout = events.Single(static item => item.GetProperty("name").GetString() == "LayoutBuild");
        var fragment = layout
            .GetProperty("payload")
            .GetProperty("snapshot")
            .GetProperty("pages")[0]
            .GetProperty("fragments")[0];
        fragment.GetProperty("metadataOwner").GetString().ShouldBe("FragmentBuilder");
        fragment.GetProperty("metadataConsumer").GetString().ShouldBe("LayoutSnapshotMapper");
    }
}
