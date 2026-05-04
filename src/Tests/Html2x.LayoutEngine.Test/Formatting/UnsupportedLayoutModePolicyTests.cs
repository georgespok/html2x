using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Contracts.Style;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Formatting;

public sealed class UnsupportedLayoutModePolicyTests
{
    [Fact]
    public void Report_UnsupportedModes_EmitsExplicitDiagnostics()
    {
        var diagnostics = new RecordingDiagnosticsSink();
        var root = new BlockBox(BoxRole.Block);
        root.Children.Add(new FloatBox(BoxRole.Float)
        {
            Parent = root,
            Style = new ComputedStyle
            {
                FloatDirection = HtmlCssConstants.CssValues.Left
            }
        });
        root.Children.Add(new BlockBox(BoxRole.Block)
        {
            Parent = root,
            Style = new ComputedStyle
            {
                Display = HtmlCssConstants.CssValues.Flex
            }
        });
        root.Children.Add(new BlockBox(BoxRole.Block)
        {
            Parent = root,
            Style = new ComputedStyle
            {
                Position = HtmlCssConstants.CssValues.Absolute
            }
        });

        new UnsupportedLayoutModePolicy().Report(root, diagnostics);

        diagnostics.Records
            .Where(static e => e.Name == "layout/unsupported-mode")
            .Select(static e => e.Fields["structureKind"].ShouldBeOfType<DiagnosticStringValue>().Value)
            .ShouldBe(["float", "display:flex", "position:absolute"]);
    }
}
