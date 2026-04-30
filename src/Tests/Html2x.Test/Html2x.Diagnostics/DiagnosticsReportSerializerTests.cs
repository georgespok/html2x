using System.Text.Json;
using Html2x.Diagnostics;
using Html2x.Diagnostics.Contracts;
using Shouldly;

namespace Html2x.Test.Html2x.Diagnostics;

public sealed class DiagnosticsReportSerializerTests
{
    [Fact]
    public void Collector_ToReport_ReturnsImmutableSnapshotOfEmittedRecords()
    {
        var collector = new DiagnosticsCollector(DateTimeOffset.Parse("2026-04-14T10:00:00Z"));
        var firstRecord = CreateRecord("stage/style", "style/ignored-declaration");
        var secondRecord = CreateRecord("stage/render", "render/image");

        collector.Emit(firstRecord);
        var firstReport = collector.ToReport(DateTimeOffset.Parse("2026-04-14T10:00:01Z"));
        collector.Emit(secondRecord);
        var secondReport = collector.ToReport(DateTimeOffset.Parse("2026-04-14T10:00:02Z"));

        firstReport.StartTime.ShouldBe(DateTimeOffset.Parse("2026-04-14T10:00:00Z"));
        firstReport.EndTime.ShouldBe(DateTimeOffset.Parse("2026-04-14T10:00:01Z"));
        firstReport.Records.ShouldBe([firstRecord]);
        secondReport.Records.ShouldBe([firstRecord, secondRecord]);
    }

    [Fact]
    public void ToJson_ReportWithNestedFields_MatchesStableJsonShape()
    {
        var report = new DiagnosticsReport(
            DateTimeOffset.Parse("2026-04-14T10:00:00Z"),
            DateTimeOffset.Parse("2026-04-14T10:00:01Z"),
            [CreateRecord("stage/style", "style/ignored-declaration")]);

        var json = DiagnosticsReportSerializer.ToJson(report);

        NormalizeNewLines(json).ShouldBe(NormalizeNewLines("""
            {
              "startTime": "2026-04-14T10:00:00+00:00",
              "endTime": "2026-04-14T10:00:01+00:00",
              "records": [
                {
                  "stage": "stage/style",
                  "name": "style/ignored-declaration",
                  "severity": "Warning",
                  "message": "Ignored declaration.",
                  "context": {
                    "selector": ".invoice-total",
                    "elementIdentity": "p#total",
                    "styleDeclaration": "width: 120qu",
                    "structuralPath": "html/body/p[3]",
                    "rawUserInput": "<p id=\"total\">$42</p>"
                  },
                  "fields": {
                    "property": "width",
                    "attempts": 2,
                    "ratio": 0.75,
                    "successful": false,
                    "decision": "Skipped",
                    "raw": null,
                    "values": [
                      "120qu",
                      12.5,
                      true,
                      null
                    ],
                    "source": {
                      "path": "html/body/p[3]",
                      "index": 3
                    }
                  },
                  "timestamp": "2026-04-14T10:00:00.5+00:00"
                }
              ]
            }
            """));
    }

    [Fact]
    public void ToJson_NullContextAndEmptyFields_AreSerialized()
    {
        var report = new DiagnosticsReport(
            DateTimeOffset.Parse("2026-04-14T10:00:00Z"),
            DateTimeOffset.Parse("2026-04-14T10:00:01Z"),
            [
                new DiagnosticRecord(
                    Stage: "stage/render",
                    Name: "render/skipped",
                    Severity: DiagnosticSeverity.Info,
                    Message: null,
                    Context: null,
                    Fields: DiagnosticFields.Empty,
                    Timestamp: DateTimeOffset.Parse("2026-04-14T10:00:00Z"))
            ]);

        var json = DiagnosticsReportSerializer.ToJson(report);

        using var document = JsonDocument.Parse(json);
        var record = document.RootElement.GetProperty("records")[0];
        record.GetProperty("context").ValueKind.ShouldBe(JsonValueKind.Null);
        record.GetProperty("message").ValueKind.ShouldBe(JsonValueKind.Null);
        record.GetProperty("fields").EnumerateObject().ShouldBeEmpty();
    }

    private static DiagnosticRecord CreateRecord(string stage, string name)
    {
        return new DiagnosticRecord(
            Stage: stage,
            Name: name,
            Severity: DiagnosticSeverity.Warning,
            Message: "Ignored declaration.",
            Context: new DiagnosticContext(
                Selector: ".invoice-total",
                ElementIdentity: "p#total",
                StyleDeclaration: "width: 120qu",
                StructuralPath: "html/body/p[3]",
                RawUserInput: "<p id=\"total\">$42</p>"),
            Fields: DiagnosticFields.Create(
                DiagnosticFields.Field("property", "width"),
                DiagnosticFields.Field("attempts", 2),
                DiagnosticFields.Field("ratio", 0.75),
                DiagnosticFields.Field("successful", false),
                DiagnosticFields.Field("decision", DiagnosticValue.FromEnum(DiagnosticDecision.Skipped)),
                DiagnosticFields.Field("raw", null),
                DiagnosticFields.Field("values", DiagnosticArray.Create("120qu", 12.5, true, null)),
                DiagnosticFields.Field(
                    "source",
                    DiagnosticObject.Create(
                        DiagnosticObject.Field("path", "html/body/p[3]"),
                        DiagnosticObject.Field("index", 3)))),
            Timestamp: DateTimeOffset.Parse("2026-04-14T10:00:00.5Z"));
    }

    private static string NormalizeNewLines(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal);

    private enum DiagnosticDecision
    {
        Skipped
    }
}
