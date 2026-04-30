using System.Drawing;
using Html2x.Diagnostics.Contracts;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.File;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Options;
using Html2x.Renderers.Pdf.Pipeline;
using Moq;
using Shouldly;

namespace Html2x.Renderers.Pdf.Test;

public class SkiaDiagnosticsTests
{
    [Fact]
    public async Task MissingImage_DiagnosticsSink_EmitsImageRenderFields()
    {
        var sink = new RecordingDiagnosticsSink();
        var layout = CreateLayoutWithMissingImage();
        var fileDirectory = new Mock<IFileDirectory>(MockBehavior.Strict);
        var renderer = new PdfRenderer(fileDirectory.Object);
        var options = new PdfOptions { FontPath = string.Empty, HtmlDirectory = "." };

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
                IsMissing = true,
                Rect = new RectangleF(20, 30, 50, 40),
                ContentRect = new RectangleF(20, 30, 50, 40),
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
