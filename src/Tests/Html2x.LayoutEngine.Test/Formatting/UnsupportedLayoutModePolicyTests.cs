using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Diagnostics;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Formatting;

public sealed class UnsupportedLayoutModePolicyTests
{
    [Fact]
    public void Report_UnsupportedModes_EmitsExplicitDiagnostics()
    {
        var diagnostics = new RecordingDiagnosticsSink();
        var root = new BlockBox(BoxRole.Block);
        root.AddChild(new FloatBox(BoxRole.Float)
        {
            Parent = root,
            Style = new()
            {
                FloatDirection = HtmlCssVocabulary.CssValues.Left
            }
        });
        root.AddChild(new BlockBox(BoxRole.Block)
        {
            Parent = root,
            Style = new()
            {
                Display = HtmlCssVocabulary.CssValues.Flex
            }
        });
        root.AddChild(new BlockBox(BoxRole.Block)
        {
            Parent = root,
            Style = new()
            {
                Position = HtmlCssVocabulary.CssValues.Absolute
            }
        });

        new UnsupportedLayoutModePolicy().Report(root, diagnostics);

        diagnostics.Records
            .Where(static e => e.Name == "layout/unsupported-mode")
            .Select(static e => e.Fields["structureKind"].ShouldBeOfType<DiagnosticStringValue>().Value)
            .ShouldBe(["float", "display:flex", "position:absolute"]);
    }
}
