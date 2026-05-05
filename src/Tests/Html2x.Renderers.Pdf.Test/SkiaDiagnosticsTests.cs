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
public class SkiaDiagnosticsTests
{
    [Fact]
    public async Task MissingImage_DiagnosticsSink_EmitsImageRenderFields()
    {
        var sink = new RecordingDiagnosticsSink();
        var layout = CreateLayoutWithMissingImage();
        var renderer = new PdfRenderer();
        var options = new PdfRenderSettings { ResourceBaseDirectory = "." };

        var pdf = await renderer.RenderAsync(layout, options, diagnosticsSink: sink);

        pdf.ShouldNotBeNull();
        var record = sink.Records.Single(static x => x.Name == "image/render");
        record.Stage.ShouldBe("stage/render");
        record.Severity.ShouldBe(DiagnosticSeverity.Warning);
        record.Context.ShouldNotBeNull().RawUserInput.ShouldBe("missing.png");
        record.Fields["src"].ShouldBe(new DiagnosticStringValue("missing.png"));
        record.Fields["status"].ShouldBe(new DiagnosticStringValue("Missing"));
        record.Fields["renderedWidth"].ShouldBe(new DiagnosticNumberValue(50f));
        record.Fields["renderedHeight"].ShouldBe(new DiagnosticNumberValue(40f));
        record.Fields["borders"].ShouldBe(DiagnosticObject.Empty);
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
                Status = ImageLoadStatus.Missing,
                Rect = new RectPt(20, 30, 50, 40),
                ContentRect = new RectPt(20, 30, 50, 40),
                Style = new VisualStyle()
            }
        };

        var page = new LayoutPage(
            new SizePt(PaperSizes.A4.Width, PaperSizes.A4.Height),
            new Spacing(0, 0, 0, 0),
            fragments,
            1,
            new ColorRgba(255, 255, 255, 255));

        layout.AddPage(page);
        return layout;
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
}
