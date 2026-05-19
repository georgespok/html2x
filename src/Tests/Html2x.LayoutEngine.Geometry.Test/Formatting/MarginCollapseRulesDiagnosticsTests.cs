using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.BlockFlow;
using Html2x.RenderModel.Fragments;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test.Formatting;

public sealed class MarginCollapseRulesDiagnosticsTests
{
    [Theory]
    [InlineData(12f, 4f, 12f)]
    [InlineData(-12f, -4f, -12f)]
    [InlineData(12f, -4f, 8f)]
    [InlineData(-12f, 4f, -8f)]
    public void CollapseMargins_WithSignedPairs_UsesCollapseRule(
        float previousBottomMargin,
        float nextTopMargin,
        float expectedCollapsedTopMargin)
    {
        var rules = new MarginCollapseRules();

        var collapsed = rules.Collapse(
            previousBottomMargin,
            nextTopMargin,
            FormattingContextKind.Block,
            "test-consumer");

        collapsed.ShouldBe(expectedCollapsedTopMargin);
    }

    [Fact]
    public void CollapseMargins_DiagnosticsSink_EmitsConstrainedFieldRecord()
    {
        var sink = new RecordingDiagnosticsSink();
        var rules = new MarginCollapseRules();

        var collapsed = rules.Collapse(
            12f,
            -4f,
            FormattingContextKind.Block,
            "test-consumer",
            sink);

        collapsed.ShouldBe(8f);
        var record = sink.Records.ShouldHaveSingleItem();
        record.Stage.ShouldBe("stage/box-tree");
        record.Name.ShouldBe("layout/margin-collapse");
        record.Severity.ShouldBe(DiagnosticSeverity.Info);
        record.Fields["previousBottomMargin"].ShouldBe(new DiagnosticNumberValue(12f));
        record.Fields["nextTopMargin"].ShouldBe(new DiagnosticNumberValue(-4f));
        record.Fields["collapsedTopMargin"].ShouldBe(new DiagnosticNumberValue(8f));
        record.Fields["owner"].ShouldBe(new DiagnosticStringValue("BlockFormattingContext"));
        record.Fields["consumer"].ShouldBe(new DiagnosticStringValue("test-consumer"));
        record.Fields["formattingContext"].ShouldBe(new DiagnosticStringValue("Block"));
    }
}
