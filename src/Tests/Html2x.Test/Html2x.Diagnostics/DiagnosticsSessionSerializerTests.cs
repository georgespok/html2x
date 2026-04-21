using System.Text.Json;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Options;
using Html2x.Diagnostics;
using Shouldly;

namespace Html2x.Test.Html2x.Diagnostics;

public sealed class DiagnosticsSessionSerializerTests
{
    [Fact]
    public void ToJson_EventWithCanonicalFields_SerializesSeverityLifecycleContextAndRawInput()
    {
        var session = new DiagnosticsSession
        {
            StartTime = DateTimeOffset.Parse("2026-04-14T10:00:00Z"),
            EndTime = DateTimeOffset.Parse("2026-04-14T10:00:01Z"),
            Options = new HtmlConverterOptions()
        };

        session.Events.Add(new DiagnosticsEvent
        {
            Type = DiagnosticsEventType.StartStage,
            Name = "stage/style",
            Description = "Style computation started.",
            Timestamp = DateTimeOffset.Parse("2026-04-14T10:00:00Z"),
            Severity = DiagnosticSeverity.Info,
            StageState = DiagnosticStageState.Started,
            Context = new DiagnosticContext(
                Selector: ".invoice-total",
                ElementIdentity: "p#total",
                StyleDeclaration: "width: 120qu",
                StructuralPath: "html/body/p[3]",
                RawUserInput: "<p id=\"total\">$42</p>"),
            RawUserInput = "<p id=\"total\">$42</p>"
        });

        var json = DiagnosticsSessionSerializer.ToJson(session);

        using var document = JsonDocument.Parse(json);
        var evt = document.RootElement.GetProperty("events")[0];

        evt.GetProperty("severity").GetString().ShouldBe("Info");
        evt.GetProperty("stageState").GetString().ShouldBe("Started");
        evt.GetProperty("description").GetString().ShouldBe("Style computation started.");
        evt.GetProperty("rawUserInput").GetString().ShouldBe("<p id=\"total\">$42</p>");

        var context = evt.GetProperty("context");
        context.GetProperty("selector").GetString().ShouldBe(".invoice-total");
        context.GetProperty("elementIdentity").GetString().ShouldBe("p#total");
        context.GetProperty("styleDeclaration").GetString().ShouldBe("width: 120qu");
        context.GetProperty("structuralPath").GetString().ShouldBe("html/body/p[3]");
        context.GetProperty("rawUserInput").GetString().ShouldBe("<p id=\"total\">$42</p>");
    }

    [Fact]
    public void ToJson_StageLifecycleEvents_SerializesLifecycleFieldsAndPayloadKind()
    {
        var session = new DiagnosticsSession
        {
            StartTime = DateTimeOffset.Parse("2026-04-14T10:00:00Z"),
            EndTime = DateTimeOffset.Parse("2026-04-14T10:00:01Z"),
            Options = new HtmlConverterOptions()
        };

        session.Events.Add(DiagnosticsEventFactory.StageStarted(
            "LayoutBuild",
            new HtmlPayload { Html = "<html><body>Report</body></html>" }));
        session.Events.Add(DiagnosticsEventFactory.StageSucceeded("stage/style"));
        session.Events.Add(DiagnosticsEventFactory.StageFailed(
            "PdfRender",
            "Renderer failed."));

        var json = DiagnosticsSessionSerializer.ToJson(session);

        using var document = JsonDocument.Parse(json);
        var events = document.RootElement.GetProperty("events");

        var layoutBuild = events[0];
        layoutBuild.GetProperty("type").GetString().ShouldBe("StartStage");
        layoutBuild.GetProperty("name").GetString().ShouldBe("LayoutBuild");
        layoutBuild.GetProperty("severity").GetString().ShouldBe("Info");
        layoutBuild.GetProperty("stageState").GetString().ShouldBe("Started");
        layoutBuild.GetProperty("payload").GetProperty("kind").GetString().ShouldBe("html");

        var styleStage = events[1];
        styleStage.GetProperty("type").GetString().ShouldBe("EndStage");
        styleStage.GetProperty("name").GetString().ShouldBe("stage/style");
        styleStage.GetProperty("severity").GetString().ShouldBe("Info");
        styleStage.GetProperty("stageState").GetString().ShouldBe("Succeeded");

        var pdfRender = events[2];
        pdfRender.GetProperty("type").GetString().ShouldBe("Error");
        pdfRender.GetProperty("name").GetString().ShouldBe("PdfRender");
        pdfRender.GetProperty("severity").GetString().ShouldBe("Error");
        pdfRender.GetProperty("stageState").GetString().ShouldBe("Failed");
        pdfRender.GetProperty("description").GetString().ShouldBe("Renderer failed.");
    }

    [Fact]
    public void ToJson_KnownAndUnknownPayloads_PreservesRequiredExportFields()
    {
        var context = new DiagnosticContext(
            Selector: "img",
            ElementIdentity: "img#logo",
            StyleDeclaration: "width: 120px",
            StructuralPath: "html/body/img[1]",
            RawUserInput: "<img id=\"logo\" src=\"missing.png\">");
        var session = new DiagnosticsSession
        {
            StartTime = DateTimeOffset.Parse("2026-04-14T10:00:00Z"),
            EndTime = DateTimeOffset.Parse("2026-04-14T10:00:01Z"),
            Options = new HtmlConverterOptions()
        };

        session.Events.Add(new DiagnosticsEvent
        {
            Name = "layout/table",
            Payload = new TableLayoutPayload
            {
                NodePath = "html/body/table",
                TablePath = "html/body/table",
                RowCount = 1,
                DerivedColumnCount = 2,
                RequestedWidth = 400,
                ResolvedWidth = 400,
                Outcome = "Supported",
                RowContexts = [new TableRowDiagnosticContext(0, 2, 24)],
                CellContexts = [new TableCellDiagnosticContext(0, 0, true, 200, 24)],
                ColumnContexts = [new TableColumnDiagnosticContext(0, 200)],
                GroupContexts = [new TableGroupDiagnosticContext("thead", 1)]
            }
        });
        session.Events.Add(new DiagnosticsEvent
        {
            Name = "layout/pagination/oversized-block",
            Payload = new PaginationTracePayload
            {
                EventName = "layout/pagination/oversized-block",
                Severity = DiagnosticSeverity.Warning,
                Context = context,
                PageNumber = 2,
                FragmentId = 7,
                BlockHeight = 900,
                PageContentHeight = 700
            }
        });
        session.Events.Add(new DiagnosticsEvent
        {
            Name = "image/render",
            Payload = new ImageRenderPayload
            {
                Src = "missing.png",
                Severity = DiagnosticSeverity.Warning,
                Context = context,
                RenderedSize = new SizePt(120, 80),
                Status = ImageStatus.Missing,
                Borders = BorderEdges.Uniform(new BorderSide(1, ColorRgba.Black, BorderLineStyle.Solid))
            }
        });
        session.Events.Add(new DiagnosticsEvent
        {
            Name = "layout/snapshot",
            Payload = new LayoutSnapshotPayload
            {
                Snapshot = new LayoutSnapshot
                {
                    PageCount = 1,
                    Pages =
                    [
                        new LayoutPageSnapshot
                        {
                            PageNumber = 1,
                            Fragments =
                            [
                                new FragmentSnapshot
                                {
                                    Kind = "block",
                                    Size = new SizePt(100, 20),
                                    Color = ColorRgba.Black,
                                    BackgroundColor = ColorRgba.Transparent,
                                    Margin = new Spacing(1, 2, 3, 4),
                                    Padding = new Spacing(5, 6, 7, 8),
                                    WidthPt = 100,
                                    HeightPt = 20,
                                    Display = "block",
                                    DisplayRole = FragmentDisplayRole.Block
                                }
                            ]
                        }
                    ]
                }
            }
        });
        session.Events.Add(new DiagnosticsEvent
        {
            Name = "layout/geometry-snapshot",
            Payload = new GeometrySnapshotPayload
            {
                Snapshot = new GeometrySnapshot
                {
                    Boxes =
                    [
                        new BoxGeometrySnapshot
                        {
                            SequenceId = 1,
                            Path = "html/body/div",
                            Kind = "block",
                            TagName = "div",
                            X = 10,
                            Y = 20,
                            Size = new SizePt(100, 40),
                            ContentX = 14,
                            ContentY = 24,
                            ContentSize = new SizePt(92, 32),
                            MarkerOffset = 0
                        }
                    ],
                    Fragments = new LayoutSnapshot
                    {
                        PageCount = 1,
                        Pages =
                        [
                            new LayoutPageSnapshot
                            {
                                PageNumber = 1,
                                Fragments =
                                [
                                    new FragmentSnapshot
                                    {
                                        Kind = "block",
                                        Size = new SizePt(100, 40)
                                    }
                                ]
                            }
                        ]
                    },
                    Pagination =
                    [
                        new PaginationPageSnapshot
                        {
                            PageNumber = 1,
                            Placements =
                            [
                                new PaginationPlacementSnapshot
                                {
                                    FragmentId = 7,
                                    Kind = "Block",
                                    PageNumber = 1,
                                    OrderIndex = 0,
                                    X = 10,
                                    Y = 20,
                                    Size = new SizePt(100, 40)
                                }
                            ]
                        }
                    ]
                }
            }
        });
        session.Events.Add(new DiagnosticsEvent
        {
            Name = "unknown",
            Payload = new UnknownPayload()
        });

        var json = DiagnosticsSessionSerializer.ToJson(session);

        using var document = JsonDocument.Parse(json);
        var events = document.RootElement.GetProperty("events");
        var table = events[0].GetProperty("payload");
        table.GetProperty("kind").GetString().ShouldBe("layout.table");
        table.GetProperty("tablePath").GetString().ShouldBe("html/body/table");
        table.GetProperty("rowContexts")[0].GetProperty("rowIndex").GetInt32().ShouldBe(0);
        table.GetProperty("rowContexts")[0].TryGetProperty("sourceRow", out _).ShouldBeFalse();
        table.GetProperty("cellContexts")[0].GetProperty("isHeader").GetBoolean().ShouldBeTrue();
        table.GetProperty("cellContexts")[0].TryGetProperty("sourceCell", out _).ShouldBeFalse();
        table.GetProperty("columnContexts")[0].GetProperty("width").GetSingle().ShouldBe(200);
        table.GetProperty("groupContexts")[0].GetProperty("groupKind").GetString().ShouldBe("thead");
        table.TryGetProperty("rows", out _).ShouldBeFalse();
        table.TryGetProperty("columns", out _).ShouldBeFalse();

        var pagination = events[1].GetProperty("payload");
        pagination.GetProperty("severity").GetString().ShouldBe("Warning");
        pagination.GetProperty("context").GetProperty("elementIdentity").GetString().ShouldBe("img#logo");

        var image = events[2].GetProperty("payload");
        image.GetProperty("severity").GetString().ShouldBe("Warning");
        image.GetProperty("renderedWidth").GetSingle().ShouldBe(120);
        image.GetProperty("renderedHeight").GetSingle().ShouldBe(80);
        image.GetProperty("context").GetProperty("rawUserInput").GetString()
            .ShouldBe("<img id=\"logo\" src=\"missing.png\">");

        var snapshot = events[3].GetProperty("payload").GetProperty("snapshot");
        var fragment = snapshot.GetProperty("pages")[0].GetProperty("fragments")[0];
        ((int)fragment.GetProperty("color").GetProperty("r").GetByte()).ShouldBe(0);
        ((int)fragment.GetProperty("backgroundColor").GetProperty("a").GetByte()).ShouldBe(0);
        fragment.GetProperty("margin").GetProperty("top").GetSingle().ShouldBe(1);
        fragment.GetProperty("padding").GetProperty("left").GetSingle().ShouldBe(8);
        fragment.GetProperty("display").GetString().ShouldBe("block");

        var geometry = events[4].GetProperty("payload");
        geometry.GetProperty("kind").GetString().ShouldBe("layout.geometry");
        geometry.GetProperty("snapshot").GetProperty("boxes")[0].GetProperty("path").GetString()
            .ShouldBe("html/body/div");
        geometry.GetProperty("snapshot").GetProperty("pagination")[0].GetProperty("placements")[0].GetProperty("fragmentId")
            .GetInt32().ShouldBe(7);

        events[5].GetProperty("payload").GetProperty("kind").GetString().ShouldBe("test.unknown");
    }

    private sealed class UnknownPayload : IDiagnosticsPayload
    {
        public string Kind => "test.unknown";
    }
}
