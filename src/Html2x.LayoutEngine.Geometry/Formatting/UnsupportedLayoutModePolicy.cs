using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Diagnostics;
using Html2x.RenderModel.Fragments;

namespace Html2x.LayoutEngine.Geometry.Formatting;

/// <summary>
///     Reports layout modes that are parsed today but do not have implemented formatting contexts.
/// </summary>
internal sealed class UnsupportedLayoutModePolicy
{
    public void Report(
        BoxNode root,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(root);

        foreach (var node in Enumerate(root))
        {
            foreach (var unsupported in ResolveUnsupportedModes(node))
            {
                EmitDiagnostic(diagnosticsSink, node, unsupported);
            }
        }
    }

    private static IEnumerable<UnsupportedLayoutMode> ResolveUnsupportedModes(BoxNode node)
    {
        if (node is FloatBox ||
            string.Equals(node.Style.FloatDirection, HtmlCssConstants.CssValues.Left,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(node.Style.FloatDirection, HtmlCssConstants.CssValues.Right,
                StringComparison.OrdinalIgnoreCase))
        {
            yield return new(
                UnsupportedDiagnosticNames.StructureKinds.Float,
                UnsupportedDiagnosticNames.Reasons.CssFloats);
        }

        if (string.Equals(node.Style.Display, HtmlCssConstants.CssValues.Flex, StringComparison.OrdinalIgnoreCase))
        {
            yield return new(
                UnsupportedDiagnosticNames.StructureKinds.DisplayFlex,
                UnsupportedDiagnosticNames.Reasons.CssFlex);
        }

        if (string.Equals(node.Style.Position, HtmlCssConstants.CssValues.Absolute, StringComparison.OrdinalIgnoreCase))
        {
            yield return new(
                UnsupportedDiagnosticNames.StructureKinds.PositionAbsolute,
                UnsupportedDiagnosticNames.Reasons.AbsolutePosition);
        }
    }

    private static void EmitDiagnostic(
        IDiagnosticsSink? diagnosticsSink,
        BoxNode node,
        UnsupportedLayoutMode unsupported)
    {
        var nodePath = BoxNodePath.Build(node);
        var formattingContext = ResolveFormattingContext(node);

        diagnosticsSink?.Emit(new(
            GeometryDiagnosticNames.Stages.BoxTree,
            UnsupportedDiagnosticNames.Events.UnsupportedMode,
            DiagnosticSeverity.Warning,
            unsupported.Reason,
            null,
            DiagnosticFields.Create(
                DiagnosticFields.Field(GeometryDiagnosticNames.Fields.NodePath, nodePath),
                DiagnosticFields.Field(GeometryDiagnosticNames.Fields.StructureKind, unsupported.StructureKind),
                DiagnosticFields.Field(GeometryDiagnosticNames.Fields.Reason, unsupported.Reason),
                DiagnosticFields.Field(
                    GeometryDiagnosticNames.Fields.FormattingContext,
                    DiagnosticValue.FromEnum(formattingContext))),
            DateTimeOffset.UtcNow));
    }

    private static FormattingContextKind ResolveFormattingContext(BoxNode node) =>
        node is BlockBox { IsInlineBlockContext: true }
            ? FormattingContextKind.InlineBlock
            : FormattingContextKind.Block;

    private static IEnumerable<BoxNode> Enumerate(BoxNode root)
    {
        yield return root;

        foreach (var child in root.Children)
        {
            foreach (var descendant in Enumerate(child))
            {
                yield return descendant;
            }
        }
    }

    private readonly record struct UnsupportedLayoutMode(
        string StructureKind,
        string Reason);
}