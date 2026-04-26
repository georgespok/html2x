using Html2x.Abstractions.Diagnostics;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Formatting;

/// <summary>
/// Reports layout modes that are parsed today but do not have implemented formatting contexts.
/// </summary>
internal sealed class UnsupportedLayoutModePolicy
{
    private const string UnsupportedModeEvent = "layout/unsupported-mode";

    public IReadOnlyList<FormattingContextBoundary> Report(
        DisplayNode root,
        DiagnosticsSession? diagnosticsSession)
    {
        ArgumentNullException.ThrowIfNull(root);

        var boundaries = new List<FormattingContextBoundary>();
        foreach (var node in Enumerate(root))
        {
            foreach (var unsupported in ResolveUnsupportedModes(node))
            {
                var boundary = FormattingContextBoundaryResolver.UnsupportedDiagnostic(
                    node,
                    unsupported.FallbackRole,
                    nameof(UnsupportedLayoutModePolicy),
                    unsupported.FallbackBehavior,
                    unsupported.Reason);
                boundaries.Add(boundary);
                EmitDiagnostic(diagnosticsSession, node, unsupported, boundary);
            }
        }

        return boundaries;
    }

    private static IEnumerable<UnsupportedLayoutMode> ResolveUnsupportedModes(DisplayNode node)
    {
        if (node is FloatBox ||
            string.Equals(node.Style.FloatDirection, HtmlCssConstants.CssValues.Left, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(node.Style.FloatDirection, HtmlCssConstants.CssValues.Right, StringComparison.OrdinalIgnoreCase))
        {
            yield return new UnsupportedLayoutMode(
                StructureKind: "float",
                FallbackRole: FormattingContextRole.Block,
                FallbackBehavior: "omit-floated-subtree",
                Reason: "CSS floats are not implemented. The current fallback omits floated content from normal layout.");
        }

        if (string.Equals(node.Style.Display, HtmlCssConstants.CssValues.Flex, StringComparison.OrdinalIgnoreCase))
        {
            yield return new UnsupportedLayoutMode(
                StructureKind: "display:flex",
                FallbackRole: FormattingContextRole.Block,
                FallbackBehavior: "block-flow-fallback",
                Reason: "CSS flex layout is not implemented. The current fallback lays the container out as block flow.");
        }

        if (string.Equals(node.Style.Position, HtmlCssConstants.CssValues.Absolute, StringComparison.OrdinalIgnoreCase))
        {
            yield return new UnsupportedLayoutMode(
                StructureKind: "position:absolute",
                FallbackRole: FormattingContextRole.Block,
                FallbackBehavior: "normal-flow-fallback",
                Reason: "Absolute positioning is not implemented. The current fallback keeps the element in normal flow.");
        }
    }

    private static void EmitDiagnostic(
        DiagnosticsSession? diagnosticsSession,
        DisplayNode node,
        UnsupportedLayoutMode unsupported,
        FormattingContextBoundary boundary)
    {
        if (diagnosticsSession is null)
        {
            return;
        }

        diagnosticsSession.Events.Add(new DiagnosticsEvent
        {
            Type = DiagnosticsEventType.Warning,
            Name = UnsupportedModeEvent,
            Description = unsupported.Reason,
            Severity = DiagnosticSeverity.Warning,
            Payload = new UnsupportedStructurePayload
            {
                NodePath = DisplayNodePathBuilder.Build(node),
                StructureKind = unsupported.StructureKind,
                Reason = unsupported.Reason,
                FormattingContext = boundary.ContextKind
            }
        });
    }

    private static IEnumerable<DisplayNode> Enumerate(DisplayNode root)
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

    /// <summary>
    /// Describes one unsupported layout mode and the fallback that preserves current behavior.
    /// </summary>
    private readonly record struct UnsupportedLayoutMode(
        string StructureKind,
        FormattingContextRole FallbackRole,
        string FallbackBehavior,
        string Reason);
}
