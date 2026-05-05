using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Formatting;
using Html2x.RenderModel.Fragments;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test.Formatting;

public sealed class BlockFormattingContextDiagnosticsTests
{
    [Fact]
    public void CollapseMargins_DiagnosticsSink_EmitsConstrainedFieldRecord()
    {
        var sink = new RecordingDiagnosticsSink();
        var context = new BlockFormattingContext();

        var collapsed = context.CollapseMargins(
            previousBottomMargin: 12f,
            nextTopMargin: -4f,
            FormattingContextKind.Block,
            "test-consumer",
            diagnosticsSink: sink);

        collapsed.ShouldBe(8f);
        var record = sink.Records.ShouldHaveSingleItem();
        record.Stage.ShouldBe("stage/box-tree");
        record.Name.ShouldBe("layout/margin-collapse");
        record.Severity.ShouldBe(DiagnosticSeverity.Info);
        record.Fields["previousBottomMargin"].ShouldBe(new DiagnosticNumberValue(12f));
        record.Fields["nextTopMargin"].ShouldBe(new DiagnosticNumberValue(-4f));
        record.Fields["collapsedTopMargin"].ShouldBe(new DiagnosticNumberValue(8f));
        record.Fields["consumer"].ShouldBe(new DiagnosticStringValue("test-consumer"));
        record.Fields["formattingContext"].ShouldBe(new DiagnosticStringValue("Block"));
    }
}
