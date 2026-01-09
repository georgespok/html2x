using System.Drawing;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.File;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Options;
using Html2x.Diagnostics;
using Html2x.Renderers.Pdf.Pipeline;
using Moq;
using Shouldly;

namespace Html2x.Renderers.Pdf.Test;

public class ImageRenderingTests
{
    [Fact]
    public async Task Render_Images_ShouldReportStatusesAndRenderedSizes()
    {
        // arrange: construct layout with success, missing, and oversize cases
        var layout = new HtmlLayout();
        layout.Pages.Add(new LayoutPage(
            new SizePt(612, 792),
            new Spacing(24, 24, 24, 24),
            new List<Fragment>
            {
                CreateImageFragment(24, 60, 120, 120, status: ImageStatus.Ok),
                CreateImageFragment(24, 120, 80, 80, status: ImageStatus.Missing),
                CreateImageFragment(24, 180, 140, 70, status: ImageStatus.Oversize)
            }));

        // act
        var (bytes, diagnostics) = await RenderLayoutAsync(layout);

        // assert
        bytes.ShouldNotBeNull();

        var images = GetImageRenderPayloads(diagnostics);

        images.Count.ShouldBe(3);
        images[0].Status.ShouldBe(ImageStatus.Ok);
        images[1].Status.ShouldBe(ImageStatus.Missing);
        images[2].Status.ShouldBe(ImageStatus.Oversize);

        images[0].RenderedSize.Width.ShouldBe(120, 1);
        images[0].RenderedSize.Height.ShouldBe(120, 1);

        images[1].RenderedSize.Width.ShouldBe(80, 1);
        images[1].RenderedSize.Height.ShouldBe(80, 1);

        images[2].RenderedSize.Width.ShouldBe(140, 1);
        images[2].RenderedSize.Height.ShouldBe(70, 1);
    }

    [Fact]
    public async Task Render_ImageWithBorder_ShouldReportBorderMetadata()
    {
        var borderColor = new ColorRgba(0x12, 0x34, 0x56, 0xFF);
        var borders = BorderEdges.Uniform(new BorderSide(2f, borderColor, BorderLineStyle.Solid));

        var layout = new HtmlLayout();
        layout.Pages.Add(new LayoutPage(
            new SizePt(612, 792),
            new Spacing(0, 0, 0, 0),
            new List<Fragment>
            {
                CreateImageFragment(24, 40, 64, 64, ImageStatus.Ok, borders)
            }));

        var (bytes, diagnostics) = await RenderLayoutAsync(layout);

        bytes.ShouldNotBeNull();

        var payload = GetSingleImageRenderPayload(diagnostics);

        payload.ShouldNotBeNull();
        payload!.Borders.ShouldNotBeNull();
        payload.Borders!.Top.ShouldNotBeNull();
        payload.Borders.Top!.Width.ShouldBe(2f);
        payload.Borders.Top!.Color.ShouldBe(borderColor);
        payload.Borders.Top!.LineStyle.ShouldBe(BorderLineStyle.Solid);
    }

    [Fact]
    public async Task Render_MissingImageWithBorder_ShouldReportBorderMetadata()
    {
        var borderColor = new ColorRgba(0x9A, 0x3D, 0xC0, 0xFF);
        var borders = BorderEdges.Uniform(new BorderSide(3f, borderColor, BorderLineStyle.Solid));

        var layout = new HtmlLayout();
        layout.Pages.Add(new LayoutPage(
            new SizePt(612, 792),
            new Spacing(0, 0, 0, 0),
            new List<Fragment>
            {
                CreateImageFragment(30, 50, 72, 72, ImageStatus.Missing, borders)
            }));

        var (bytes, diagnostics) = await RenderLayoutAsync(layout);

        bytes.ShouldNotBeNull();

        var payload = GetSingleImageRenderPayload(diagnostics);

        payload.ShouldNotBeNull();
        payload!.Status.ShouldBe(ImageStatus.Missing);
        payload.Borders.ShouldNotBeNull();
        payload.Borders!.Top.ShouldNotBeNull();
        payload.Borders.Top!.Width.ShouldBe(3f);
        payload.Borders.Top!.Color.ShouldBe(borderColor);
        payload.Borders.Top!.LineStyle.ShouldBe(BorderLineStyle.Solid);
    }

    [Fact]
    public async Task Render_ImageWithDashedBorder_ShouldReportBorderStyle()
    {
        var borderColor = new ColorRgba(0x33, 0x66, 0x99, 0xFF);
        var borders = BorderEdges.Uniform(new BorderSide(1.5f, borderColor, BorderLineStyle.Dashed));

        var layout = new HtmlLayout();
        layout.Pages.Add(new LayoutPage(
            new SizePt(612, 792),
            new Spacing(0, 0, 0, 0),
            new List<Fragment>
            {
                CreateImageFragment(40, 60, 80, 80, ImageStatus.Ok, borders)
            }));

        var (bytes, diagnostics) = await RenderLayoutAsync(layout);

        bytes.ShouldNotBeNull();

        var payload = GetSingleImageRenderPayload(diagnostics);

        payload.ShouldNotBeNull();
        payload!.Borders.ShouldNotBeNull();
        payload.Borders!.Top.ShouldNotBeNull();
        payload.Borders.Top!.Width.ShouldBe(1.5f);
        payload.Borders.Top!.Color.ShouldBe(borderColor);
        payload.Borders.Top!.LineStyle.ShouldBe(BorderLineStyle.Dashed);
    }

    [Fact]
    public async Task Render_ImageWithNoBorder_ShouldReportNoBorders()
    {
        var borders = BorderEdges.Uniform(new BorderSide(0f, ColorRgba.Black, BorderLineStyle.None));

        var layout = new HtmlLayout();
        layout.Pages.Add(new LayoutPage(
            new SizePt(612, 792),
            new Spacing(0, 0, 0, 0),
            new List<Fragment>
            {
                CreateImageFragment(48, 72, 64, 64, ImageStatus.Ok, borders)
            }));

        var (bytes, diagnostics) = await RenderLayoutAsync(layout);

        bytes.ShouldNotBeNull();

        var payload = GetSingleImageRenderPayload(diagnostics);

        payload.ShouldNotBeNull();
        payload!.Borders.ShouldNotBeNull();
        payload.Borders!.Top.ShouldNotBeNull();
        payload.Borders.Top!.Width.ShouldBe(0f);
        payload.Borders.Top!.LineStyle.ShouldBe(BorderLineStyle.None);
    }

    private static ImageFragment CreateImageFragment(
        float x,
        float y,
        float width,
        float height,
        ImageStatus status,
        BorderEdges? borders = null)
    {
        const string dataUri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAuMB9p8S9F0AAAAASUVORK5CYII=";

        var isMissing = status == ImageStatus.Missing;
        var isOversize = status == ImageStatus.Oversize;

        return new ImageFragment
        {
            Src = dataUri,
            AuthoredSizePx = new SizePx(width, height),
            IntrinsicSizePx = new SizePx(width, height),
            Rect = new RectangleF(x, y, width, height),
            Style = new VisualStyle(Borders: borders),
            ZOrder = 0,
            IsMissing = isMissing,
            IsOversize = isOversize
        };
    }

    private static List<ImageRenderPayload> GetImageRenderPayloads(DiagnosticsSession diagnostics)
    {
        return diagnostics.Events
            .Where(e => e.Name == "ImageRender")
            .Select(e => e.Payload)
            .OfType<ImageRenderPayload>()
            .ToList();
    }

    private static ImageRenderPayload? GetSingleImageRenderPayload(DiagnosticsSession diagnostics)
        => GetImageRenderPayloads(diagnostics).SingleOrDefault();

    private static async Task<(byte[]? Bytes, DiagnosticsSession Diagnostics)> RenderLayoutAsync(HtmlLayout layout)
    {
        var pdfOptions = new PdfOptions
        {
            HtmlDirectory = Directory.GetCurrentDirectory()
        };

        var diagnostics = new DiagnosticsSession
        {
            StartTime = DateTimeOffset.UtcNow,
            Options = new HtmlConverterOptions
            {
                Pdf = pdfOptions,
                Diagnostics = new DiagnosticsOptions { EnableDiagnostics = true }
            }
        };

        var fileDirectory = new Mock<IFileDirectory>(MockBehavior.Strict);
        var renderer = new PdfRenderer(fileDirectory.Object);

        var bytes = await renderer.RenderAsync(layout, pdfOptions, diagnostics);
        return (bytes, diagnostics);
    }
}
