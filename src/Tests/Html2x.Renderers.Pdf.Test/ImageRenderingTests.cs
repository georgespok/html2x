using Html2x.Diagnostics.Contracts;
using Html2x.Renderers.Pdf.Pipeline;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;
using Shouldly;

namespace Html2x.Renderers.Pdf.Test;

[Trait("Category", "Integration")]
public class ImageRenderingTests
{
    [Fact]
    public async Task Render_Images_ReportStatusesAndRenderedSizes()
    {
        // arrange: construct layout with success, missing, and oversize cases
        var layout = new HtmlLayout();
        layout.AddPage(new LayoutPage(
            new SizePt(612, 792),
            new Spacing(24, 24, 24, 24),
            new List<Fragment>
            {
                CreateImageFragment(24, 60, 120, 120, status: ImageLoadStatus.Ok),
                CreateImageFragment(24, 120, 80, 80, status: ImageLoadStatus.Missing),
                CreateImageFragment(24, 180, 140, 70, status: ImageLoadStatus.Oversize)
            }));

        // act
        var (bytes, diagnostics) = await RenderLayoutAsync(layout);

        // assert
        bytes.ShouldNotBeNull();

        var images = GetImageRenderRecords(diagnostics);

        images.Count.ShouldBe(3);
        GetStatus(images[0]).ShouldBe("Ok");
        GetStatus(images[1]).ShouldBe("Missing");
        GetStatus(images[2]).ShouldBe("Oversize");

        GetNumber(images[0], "renderedWidth").ShouldBe(120d, 1d);
        GetNumber(images[0], "renderedHeight").ShouldBe(120d, 1d);

        GetNumber(images[1], "renderedWidth").ShouldBe(80d, 1d);
        GetNumber(images[1], "renderedHeight").ShouldBe(80d, 1d);

        GetNumber(images[2], "renderedWidth").ShouldBe(140d, 1d);
        GetNumber(images[2], "renderedHeight").ShouldBe(70d, 1d);
    }

    [Fact]
    public async Task Render_ImageDiagnostics_UseCanonicalEventAndContext()
    {
        var layout = new HtmlLayout();
        layout.AddPage(new LayoutPage(
            new SizePt(612, 792),
            new Spacing(24, 24, 24, 24),
            new List<Fragment>
            {
                CreateImageFragment(24, 60, 120, 80, ImageLoadStatus.Missing, src: "missing.png")
            }));

        var (bytes, diagnostics) = await RenderLayoutAsync(layout);

        bytes.ShouldNotBeNull();

        var evt = diagnostics.ShouldHaveSingleItem();
        evt.Name.ShouldBe("image/render");
        evt.Severity.ShouldBe(DiagnosticSeverity.Warning);
        evt.Context.ShouldNotBeNull();
        evt.Context!.ElementIdentity.ShouldBe("img");
        evt.Context.StructuralPath.ShouldBe("image:missing.png");
        evt.Context.RawUserInput.ShouldBe("missing.png");
        evt.Fields["src"].ShouldBe(new DiagnosticStringValue("missing.png"));
        evt.Fields["status"].ShouldBe(new DiagnosticStringValue("Missing"));
        GetNumber(evt, "renderedWidth").ShouldBe(120d, 1d);
        GetNumber(evt, "renderedHeight").ShouldBe(80d, 1d);
    }

    [Theory]
    [MemberData(nameof(ImageBorderCases))]
    public async Task Render_ImageWithBorder_ReportsBorderMetadata(
        ImageLoadStatus status,
        float borderWidth,
        ColorRgba borderColor,
        BorderLineStyle lineStyle)
    {
        var borders = BorderEdges.Uniform(new BorderSide(borderWidth, borderColor, lineStyle));

        var layout = new HtmlLayout();
        layout.AddPage(new LayoutPage(
            new SizePt(612, 792),
            new Spacing(0, 0, 0, 0),
            new List<Fragment>
            {
                CreateImageFragment(24, 40, 64, 64, status, borders)
            }));

        var (bytes, diagnostics) = await RenderLayoutAsync(layout);

        bytes.ShouldNotBeNull();

        var payload = GetSingleImageRenderRecord(diagnostics);

        payload.ShouldNotBeNull();
        GetStatus(payload).ShouldBe(status.ToString());
        var bordersObject = GetBorders(payload);
        var top = GetBorderSide(bordersObject, "top");
        top.ShouldNotBeNull();
        GetNumber(top, "width").ShouldBe(borderWidth, 0.01d);
        top["color"].ShouldBe(new DiagnosticStringValue(borderColor.ToHex()));
        top["lineStyle"].ShouldBe(new DiagnosticStringValue(lineStyle.ToString()));
    }

    [Fact]
    public async Task Render_ImageWithNoBorder_ReportNoBorders()
    {
        var borders = BorderEdges.Uniform(new BorderSide(0f, ColorRgba.Black, BorderLineStyle.None));

        var layout = new HtmlLayout();
        layout.AddPage(new LayoutPage(
            new SizePt(612, 792),
            new Spacing(0, 0, 0, 0),
            new List<Fragment>
            {
                CreateImageFragment(48, 72, 64, 64, ImageLoadStatus.Ok, borders)
            }));

        var (bytes, diagnostics) = await RenderLayoutAsync(layout);

        bytes.ShouldNotBeNull();

        var payload = GetSingleImageRenderRecord(diagnostics);

        payload.ShouldNotBeNull();
        var top = GetBorderSide(GetBorders(payload), "top");
        top.ShouldNotBeNull();
        GetNumber(top, "width").ShouldBe(0d);
        top["lineStyle"].ShouldBe(new DiagnosticStringValue(BorderLineStyle.None.ToString()));
    }

    [Theory]
    [InlineData(ImageLoadStatus.Missing, "Missing")]
    [InlineData(ImageLoadStatus.Oversize, "Oversize")]
    [InlineData(ImageLoadStatus.InvalidDataUri, "InvalidDataUri")]
    [InlineData(ImageLoadStatus.DecodeFailed, "DecodeFailed")]
    [InlineData(ImageLoadStatus.OutOfScope, "OutOfScope")]
    public async Task Render_ImageLoadStatus_MapsRenderModelStatusToDiagnostics(
        ImageLoadStatus loadStatus,
        string expectedStatus)
    {
        var layout = new HtmlLayout();
        layout.AddPage(new LayoutPage(
            new SizePt(612, 792),
            new Spacing(0, 0, 0, 0),
            new List<Fragment>
            {
                CreateImageFragmentWithLoadStatus(loadStatus)
            }));

        var (bytes, diagnostics) = await RenderLayoutAsync(layout);

        bytes.ShouldNotBeNull();
        GetStatus(GetSingleImageRenderRecord(diagnostics).ShouldNotBeNull()).ShouldBe(expectedStatus);
    }

    [Fact]
    public async Task Render_ResourceLoadStatuses_MapToDiagnostics()
    {
        var rootDirectory = Directory.CreateTempSubdirectory();
        var baseDirectory = Directory.CreateDirectory(Path.Combine(rootDirectory.FullName, "base"));

        try
        {
            await File.WriteAllBytesAsync(Path.Combine(rootDirectory.FullName, "outside.png"), TwoByOnePngBytes());
            await File.WriteAllBytesAsync(Path.Combine(baseDirectory.FullName, "oversize.png"), TwoByOnePngBytes());
            await File.WriteAllBytesAsync(Path.Combine(baseDirectory.FullName, "decode.png"), [1]);

            var layout = new HtmlLayout();
            layout.AddPage(new LayoutPage(
                new SizePt(612, 792),
                new Spacing(0, 0, 0, 0),
                new List<Fragment>
                {
                    CreateImageFragment(24, 40, 16, 16, ImageLoadStatus.Ok, src: "missing.png"),
                    CreateImageFragment(24, 64, 16, 16, ImageLoadStatus.Ok, src: "../outside.png"),
                    CreateImageFragment(24, 88, 16, 16, ImageLoadStatus.Ok, src: "oversize.png"),
                    CreateImageFragment(24, 112, 16, 16, ImageLoadStatus.Ok, src: "data:image/png;base64,not-base64"),
                    CreateImageFragment(24, 136, 16, 16, ImageLoadStatus.Ok, src: "decode.png")
                }));
            var settings = new PdfRenderSettings
            {
                ResourceBaseDirectory = baseDirectory.FullName,
                MaxImageSizeBytes = 1
            };

            var (bytes, diagnostics) = await RenderLayoutAsync(layout, settings);

            bytes.ShouldNotBeNull();
            GetImageRenderRecords(diagnostics)
                .Select(GetStatus)
                .ShouldBe(["Missing", "OutOfScope", "Oversize", "InvalidDataUri", "DecodeFailed"]);
        }
        finally
        {
            rootDirectory.Delete(recursive: true);
        }
    }

    private static ImageFragment CreateImageFragment(
        float x,
        float y,
        float width,
        float height,
        ImageLoadStatus status,
        BorderEdges? borders = null,
        string? src = null)
    {
        const string dataUri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAIAAAABCAYAAAD0In+KAAAADklEQVR4nGP4z8DwHwQBEPgD/U6VwW8AAAAASUVORK5CYII=";

        return new ImageFragment
        {
            Src = src ?? dataUri,
            AuthoredSizePx = new SizePx(width, height),
            IntrinsicSizePx = new SizePx(width, height),
            Rect = new RectPt(x, y, width, height),
            ContentRect = new RectPt(x, y, width, height),
            Style = new VisualStyle(Borders: borders),
            ZOrder = 0,
            Status = status
        };
    }

    private static ImageFragment CreateImageFragmentWithLoadStatus(ImageLoadStatus status)
    {
        return new ImageFragment
        {
            Src = TwoByOnePngDataUri,
            AuthoredSizePx = new SizePx(16d, 16d),
            IntrinsicSizePx = new SizePx(16d, 16d),
            Rect = new RectPt(24, 40, 16, 16),
            ContentRect = new RectPt(24, 40, 16, 16),
            Style = new VisualStyle(),
            ZOrder = 0,
            Status = status
        };
    }

    public static IEnumerable<object[]> ImageBorderCases()
    {
        yield return
        [
            ImageLoadStatus.Ok,
            2f,
            new ColorRgba(0x12, 0x34, 0x56, 0xFF),
            BorderLineStyle.Solid
        ];
        yield return
        [
            ImageLoadStatus.Missing,
            3f,
            new ColorRgba(0x9A, 0x3D, 0xC0, 0xFF),
            BorderLineStyle.Solid
        ];
        yield return
        [
            ImageLoadStatus.Ok,
            1.5f,
            new ColorRgba(0x33, 0x66, 0x99, 0xFF),
            BorderLineStyle.Dashed
        ];
    }

    private static List<DiagnosticRecord> GetImageRenderRecords(IReadOnlyList<DiagnosticRecord> diagnostics)
    {
        return diagnostics
            .Where(static e => e.Name == "image/render")
            .ToList();
    }

    private static DiagnosticRecord? GetSingleImageRenderRecord(IReadOnlyList<DiagnosticRecord> diagnostics)
        => GetImageRenderRecords(diagnostics).SingleOrDefault();

    private static string GetStatus(DiagnosticRecord record) =>
        record.Fields["status"].ShouldBeOfType<DiagnosticStringValue>().Value;

    private static double GetNumber(DiagnosticRecord record, string fieldName) =>
        record.Fields[fieldName].ShouldBeOfType<DiagnosticNumberValue>().Value;

    private static double GetNumber(DiagnosticObject diagnosticObject, string fieldName) =>
        diagnosticObject[fieldName].ShouldBeOfType<DiagnosticNumberValue>().Value;

    private static DiagnosticObject GetBorders(DiagnosticRecord record) =>
        record.Fields["borders"].ShouldBeOfType<DiagnosticObject>();

    private static DiagnosticObject? GetBorderSide(DiagnosticObject borders, string side) =>
        borders[side]?.ShouldBeOfType<DiagnosticObject>();

    private static async Task<(byte[]? Bytes, IReadOnlyList<DiagnosticRecord> Diagnostics)> RenderLayoutAsync(
        HtmlLayout layout,
        PdfRenderSettings? settings = null)
    {
        var pdfOptions = settings ?? new PdfRenderSettings
        {
            ResourceBaseDirectory = Directory.GetCurrentDirectory()
        };

        var diagnostics = new RecordingDiagnosticsSink();

        var renderer = new PdfRenderer();

        var bytes = await renderer.RenderAsync(layout, pdfOptions, diagnosticsSink: diagnostics);
        return (bytes, diagnostics.Records);
    }

    private sealed class RecordingDiagnosticsSink : IDiagnosticsSink
    {
        private readonly List<DiagnosticRecord> _records = [];

        public IReadOnlyList<DiagnosticRecord> Records => _records;

        public void Emit(DiagnosticRecord record)
        {
            _records.Add(record);
        }
    }

    private static byte[] TwoByOnePngBytes() =>
        Convert.FromBase64String(TwoByOnePngBase64);

    private const string TwoByOnePngDataUri = $"data:image/png;base64,{TwoByOnePngBase64}";

    private const string TwoByOnePngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAABCAYAAAD0In+KAAAADklEQVR4nGP4z8DwHwQBEPgD/U6VwW8AAAAASUVORK5CYII=";
}
