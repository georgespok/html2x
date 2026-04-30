using Html2x.Abstractions.Layout.Fragments;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Published;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Diagnostics;

internal static class GeometryLayoutStructureValidator
{
    private static readonly HashSet<BoxRole> UnsupportedInlineBlockRoles =
    [
        BoxRole.Table,
        BoxRole.TableRow,
        BoxRole.TableCell
    ];

    public static void ValidateInlineBlockStructures(
        BoxTree boxTree,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(boxTree);

        foreach (var root in boxTree.Blocks)
        {
            ValidateInlineBlockStructure(root, diagnosticsSink);
        }
    }

    public static void ValidateInlineBlockStructures(
        BoxNode root,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(root);

        ValidateInlineBlockStructure(root, diagnosticsSink);
    }

    public static void ValidateInlineBlockStructures(
        PublishedLayoutTree layout,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(layout);

        foreach (var root in layout.Blocks)
        {
            if (TryFindUnsupportedInlineBlockStructure(root, out var unsupportedBlock))
            {
                var payload = new UnsupportedStructureDiagnostic(
                    unsupportedBlock.Identity.NodePath,
                    unsupportedBlock.Display.Role.ToString(),
                    "Unsupported structure encountered inside inline-block formatting context.",
                    FormattingContextKind.InlineBlock);

                EmitUnsupportedStructure(diagnosticsSink, payload);

                throw new InvalidOperationException(
                    $"Unsupported inline-block internal structure: {payload.StructureKind} at {payload.NodePath}.");
            }
        }
    }

    private static void ValidateInlineBlockStructure(
        BoxNode root,
        IDiagnosticsSink? diagnosticsSink)
    {
        if (!TryFindUnsupportedInlineBlockStructure(root, out var unsupportedNode))
        {
            return;
        }

        var payload = new UnsupportedStructureDiagnostic(
            BoxNodePathBuilder.Build(unsupportedNode),
            unsupportedNode.Role.ToString(),
            "Unsupported structure encountered inside inline-block formatting context.",
            FormattingContextKind.InlineBlock);

        EmitUnsupportedStructure(diagnosticsSink, payload);

        throw new InvalidOperationException(
            $"Unsupported inline-block internal structure: {payload.StructureKind} at {payload.NodePath}.");
    }

    private static void EmitUnsupportedStructure(
        IDiagnosticsSink? diagnosticsSink,
        UnsupportedStructureDiagnostic payload)
    {
        diagnosticsSink?.Emit(new DiagnosticRecord(
            Stage: "stage/box-tree",
            Name: "layout/inline-block/unsupported-structure",
            Severity: DiagnosticSeverity.Error,
            Message: payload.Reason,
            Context: null,
            Fields: DiagnosticFields.Create(
                DiagnosticFields.Field("nodePath", payload.NodePath),
                DiagnosticFields.Field("structureKind", payload.StructureKind),
                DiagnosticFields.Field("reason", payload.Reason),
                DiagnosticFields.Field("formattingContext", DiagnosticValue.FromEnum(payload.FormattingContext))),
            Timestamp: DateTimeOffset.UtcNow));
    }

    private sealed record UnsupportedStructureDiagnostic(
        string NodePath,
        string StructureKind,
        string Reason,
        FormattingContextKind FormattingContext);

    private static bool TryFindUnsupportedInlineBlockStructure(BoxNode root, out BoxNode unsupportedNode)
    {
        var rootIsInlineBlockContext = root is BlockBox rootBlock && rootBlock.IsInlineBlockContext;
        var stack = new Stack<(BoxNode Node, bool InInlineBlockContext)>();
        stack.Push((root, rootIsInlineBlockContext));

        while (stack.Count > 0)
        {
            var (current, inInlineBlockContext) = stack.Pop();
            if (inInlineBlockContext && UnsupportedInlineBlockRoles.Contains(current.Role))
            {
                unsupportedNode = current;
                return true;
            }

            var childInlineBlockContext = inInlineBlockContext ||
                                          (current is BlockBox block && block.IsInlineBlockContext);

            for (var i = current.Children.Count - 1; i >= 0; i--)
            {
                stack.Push((current.Children[i], childInlineBlockContext));
            }
        }

        unsupportedNode = null!;
        return false;
    }

    private static bool TryFindUnsupportedInlineBlockStructure(
        PublishedBlock root,
        out PublishedBlock unsupportedBlock)
    {
        var stack = new Stack<(PublishedBlock Block, bool InInlineBlockContext)>();
        stack.Push((root, root.Display.FormattingContext == FormattingContextKind.InlineBlock));

        while (stack.Count > 0)
        {
            var (current, inInlineBlockContext) = stack.Pop();
            if (inInlineBlockContext && UnsupportedInlineBlockRoles.Contains(MapRole(current.Display.Role)))
            {
                unsupportedBlock = current;
                return true;
            }

            var childInlineBlockContext = inInlineBlockContext ||
                                          current.Display.FormattingContext == FormattingContextKind.InlineBlock;

            foreach (var child in EnumeratePublishedChildBlocks(current).Reverse())
            {
                stack.Push((child, childInlineBlockContext));
            }
        }

        unsupportedBlock = null!;
        return false;
    }

    private static IEnumerable<PublishedBlock> EnumeratePublishedChildBlocks(PublishedBlock block)
    {
        foreach (var child in block.Children)
        {
            yield return child;
        }

        if (block.InlineLayout is null)
        {
            yield break;
        }

        foreach (var inlineObject in block.InlineLayout.Segments
                     .SelectMany(static segment => segment.Lines)
                     .SelectMany(static line => line.Items)
                     .OfType<PublishedInlineObjectItem>())
        {
            yield return inlineObject.Content;
        }
    }

    private static BoxRole MapRole(FragmentDisplayRole role)
    {
        return role switch
        {
            FragmentDisplayRole.Table => BoxRole.Table,
            FragmentDisplayRole.TableRow => BoxRole.TableRow,
            FragmentDisplayRole.TableCell => BoxRole.TableCell,
            _ => BoxRole.Block
        };
    }
}
