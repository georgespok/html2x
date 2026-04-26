using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Models;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Formatting;

public sealed class FormattingContextBoundaryTests
{
    [Fact]
    public void Resolve_TableBox_ReturnsTableRoleWithBlockPublishedContext()
    {
        var table = new TableBox(DisplayRole.Table);

        var boundary = FormattingContextBoundaryResolver.Resolve(table, "test");

        boundary.Role.ShouldBe(FormattingContextRole.Table);
        boundary.ContextKind.ShouldBe(FormattingContextKind.Block);
        boundary.Support.ShouldBe(FormattingContextSupport.Supported);
        boundary.Owner.ShouldBe("test");
    }

    [Fact]
    public void Resolve_InlineBlock_ReturnsInlineBlockRoleAndPublishedContext()
    {
        var inlineBlock = new InlineBox(DisplayRole.InlineBlock);

        var boundary = FormattingContextBoundaryResolver.Resolve(inlineBlock, "test");

        boundary.Role.ShouldBe(FormattingContextRole.InlineBlock);
        boundary.ContextKind.ShouldBe(FormattingContextKind.InlineBlock);
        boundary.Support.ShouldBe(FormattingContextSupport.Supported);
    }

    [Fact]
    public void Report_UnsupportedModes_EmitsExplicitDiagnosticsAndBoundaries()
    {
        var diagnostics = new DiagnosticsSession();
        var root = new BlockBox(DisplayRole.Block);
        root.Children.Add(new FloatBox(DisplayRole.Float)
        {
            Parent = root,
            Style = new ComputedStyle
            {
                FloatDirection = HtmlCssConstants.CssValues.Left
            }
        });
        root.Children.Add(new BlockBox(DisplayRole.Block)
        {
            Parent = root,
            Style = new ComputedStyle
            {
                Display = HtmlCssConstants.CssValues.Flex
            }
        });
        root.Children.Add(new BlockBox(DisplayRole.Block)
        {
            Parent = root,
            Style = new ComputedStyle
            {
                Position = HtmlCssConstants.CssValues.Absolute
            }
        });

        var boundaries = new UnsupportedLayoutModePolicy().Report(root, diagnostics);

        boundaries.Count.ShouldBe(3);
        boundaries.ShouldAllBe(boundary => boundary.Support == FormattingContextSupport.UnsupportedDiagnostic);
        diagnostics.Events
            .Where(static e => e.Name == "layout/unsupported-mode")
            .Select(static e => ((UnsupportedStructurePayload)e.Payload!).StructureKind)
            .ShouldBe(["float", "display:flex", "position:absolute"]);
    }
}
