using Html2x.Diagnostics.Contracts;
using Html2x.RenderModel;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Formatting;

/// <summary>
/// Reports layout modes that are parsed today but do not have implemented formatting contexts.
/// </summary>
internal sealed class UnsupportedLayoutModePolicy
{
    private const string UnsupportedModeEvent = "layout/unsupported-mode";

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
            string.Equals(node.Style.FloatDirection, HtmlCssConstants.CssValues.Left, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(node.Style.FloatDirection, HtmlCssConstants.CssValues.Right, StringComparison.OrdinalIgnoreCase))
        {
            yield return new UnsupportedLayoutMode(
                StructureKind: "float",
                Reason: "CSS floats are not implemented. The current fallback omits floated content from normal layout.");
        }

        if (string.Equals(node.Style.Display, HtmlCssConstants.CssValues.Flex, StringComparison.OrdinalIgnoreCase))
        {
            yield return new UnsupportedLayoutMode(
                StructureKind: "display:flex",
                Reason: "CSS flex layout is not implemented. The current fallback lays the container out as block flow.");
        }

        if (string.Equals(node.Style.Position, HtmlCssConstants.CssValues.Absolute, StringComparison.OrdinalIgnoreCase))
        {
            yield return new UnsupportedLayoutMode(
                StructureKind: "position:absolute",
                Reason: "Absolute positioning is not implemented. The current fallback keeps the element in normal flow.");
        }
    }

    private static void EmitDiagnostic(
        IDiagnosticsSink? diagnosticsSink,
        BoxNode node,
        UnsupportedLayoutMode unsupported)
    {
        var nodePath = BoxNodePathBuilder.Build(node);
        var formattingContext = ResolveFormattingContext(node);

        diagnosticsSink?.Emit(new DiagnosticRecord(
            Stage: "stage/box-tree",
            Name: UnsupportedModeEvent,
            Severity: DiagnosticSeverity.Warning,
            Message: unsupported.Reason,
            Context: null,
            Fields: DiagnosticFields.Create(
                DiagnosticFields.Field("nodePath", nodePath),
                DiagnosticFields.Field("structureKind", unsupported.StructureKind),
                DiagnosticFields.Field("reason", unsupported.Reason),
                DiagnosticFields.Field("formattingContext", DiagnosticValue.FromEnum(formattingContext))),
            Timestamp: DateTimeOffset.UtcNow));
    }

    private static FormattingContextKind ResolveFormattingContext(BoxNode node)
    {
        return node is BlockBox { IsInlineBlockContext: true }
            ? FormattingContextKind.InlineBlock
            : FormattingContextKind.Block;
    }

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
