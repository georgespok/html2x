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

public class SkiaDiagnosticsTests
{
    [Fact]
    public async Task MissingImage_ShouldEmitDiagnosticsWithSource()
    {
        var session = new DiagnosticsSession
        {
            StartTime = DateTimeOffset.UtcNow
        };

        var layout = CreateLayoutWithMissingImage();
        var fileDirectory = new Mock<IFileDirectory>(MockBehavior.Strict);
        var renderer = new PdfRenderer(fileDirectory.Object);
        var options = new PdfOptions { FontPath = string.Empty, HtmlDirectory = "." };

        var pdf = await renderer.RenderAsync(layout, options, session);
        pdf.ShouldNotBeNull();

        var evt = session.Events.SingleOrDefault(e => e.Name == "ImageRender");
        evt.ShouldNotBeNull("Expected ImageRender diagnostics event");

        var payload = evt!.Payload as ImageRenderPayload;
        payload.ShouldNotBeNull();
        payload!.Src.ShouldBe("missing.png");
        payload.Status.ShouldBe(ImageStatus.Missing);
        payload.RenderedSize.Width.ShouldBeGreaterThan(0);
        payload.RenderedSize.Height.ShouldBeGreaterThan(0);
    }

    private static HtmlLayout CreateLayoutWithMissingImage()
    {
        var layout = new HtmlLayout();
        var fragments = new List<Fragment>
        {
            new ImageFragment
            {
                Src = "missing.png",
                IntrinsicSizePx = new SizePx(50, 40),
                IsMissing = true,
                Rect = new RectangleF(20, 30, 50, 40),
                Style = new VisualStyle()
            }
        };

        var page = new LayoutPage(
            new SizePt(PaperSizes.A4.Width, PaperSizes.A4.Height),
            new Spacing(0, 0, 0, 0),
            fragments,
            1,
            new ColorRgba(255, 255, 255, 255));

        layout.Pages.Add(page);
        return layout;
    }
}
