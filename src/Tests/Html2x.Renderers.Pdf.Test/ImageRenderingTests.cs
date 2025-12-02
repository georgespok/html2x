using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Options;
using Html2x.Diagnostics;
using Html2x.Renderers.Pdf.Pipeline;
using Shouldly;
using Xunit;

namespace Html2x.Renderers.Pdf.Test;

public class ImageRenderingTests
{
    [Fact]
    public async Task Render_Images_ShouldReportStatusesAndRenderedSizes()
    {
        // arrange: construct layout with success, missing, and oversize cases
        var layout = new HtmlLayout();
        layout.Pages.Add(new LayoutPage(
            new SizeF(612, 792),
            new Spacing(24, 24, 24, 24),
            new List<Fragment>
            {
                CreateImageFragment(24, 60, 120, 120, status: ImageStatus.Ok),
                CreateImageFragment(24, 120, 80, 80, status: ImageStatus.Missing),
                CreateImageFragment(24, 180, 140, 70, status: ImageStatus.Oversize)
            }));

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

        var renderer = new PdfRenderer();

        // act
        var bytes = await renderer.RenderAsync(layout, pdfOptions, diagnostics);

        // assert
        bytes.ShouldNotBeNull();

        var images = diagnostics.Events
            .Where(e => e.Name == "ImageRender")
            .Select(e => e.Payload)
            .OfType<ImageRenderPayload>()
            .ToList();

        images.Count.ShouldBe(3);
        images[0].Status.ShouldBe(ImageStatus.Ok);
        images[1].Status.ShouldBe(ImageStatus.Missing);
        images[2].Status.ShouldBe(ImageStatus.Oversize);

        images[0].RenderedWidth.ShouldBe(120, 1);
        images[0].RenderedHeight.ShouldBe(120, 1);

        images[1].RenderedWidth.ShouldBe(80, 1);
        images[1].RenderedHeight.ShouldBe(80, 1);

        images[2].RenderedWidth.ShouldBe(140, 1);
        images[2].RenderedHeight.ShouldBe(70, 1);
    }

    private static ImageFragment CreateImageFragment(float x, float y, float width, float height, ImageStatus status)
    {
        const string dataUri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAuMB9p8S9F0AAAAASUVORK5CYII=";

        var isMissing = status == ImageStatus.Missing;
        var isOversize = status == ImageStatus.Oversize;

        return new ImageFragment
        {
            Src = dataUri,
            AuthoredWidthPx = width,
            AuthoredHeightPx = height,
            IntrinsicWidthPx = width,
            IntrinsicHeightPx = height,
            Rect = new RectangleF(x, y, width, height),
            Style = new VisualStyle(),
            ZOrder = 0,
            IsMissing = isMissing,
            IsOversize = isOversize
        };
    }
}
