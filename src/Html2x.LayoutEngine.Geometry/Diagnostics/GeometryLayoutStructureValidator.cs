using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.RenderModel.Fragments;

namespace Html2x.LayoutEngine.Geometry.Diagnostics;

internal static class GeometryLayoutStructureValidator
{
    private static readonly HashSet<BoxRole> UnsupportedInlineBlockRoles =
    [
        BoxRole.Table,
        BoxRole.TableRow,
        BoxRole.TableCell
    ];

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
            var unsupportedBlock = FindUnsupportedInlineBlockStructure(root);
            if (unsupportedBlock is not null)
            {
                var payload = new UnsupportedStructureDiagnostic(
                    unsupportedBlock.Identity.NodePath,
                    unsupportedBlock.Display.Role.ToString(),
                    UnsupportedDiagnosticNames.Reasons.InlineBlockUnsupportedStructure,
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
        var unsupportedNode = FindUnsupportedInlineBlockStructure(root);
        if (unsupportedNode is null)
        {
            return;
        }

        var payload = new UnsupportedStructureDiagnostic(
            BoxNodePath.Build(unsupportedNode),
            unsupportedNode.Role.ToString(),
            UnsupportedDiagnosticNames.Reasons.InlineBlockUnsupportedStructure,
            FormattingContextKind.InlineBlock);

        EmitUnsupportedStructure(diagnosticsSink, payload);

        throw new InvalidOperationException(
            $"Unsupported inline-block internal structure: {payload.StructureKind} at {payload.NodePath}.");
    }

    private static void EmitUnsupportedStructure(
        IDiagnosticsSink? diagnosticsSink,
        UnsupportedStructureDiagnostic payload)
    {
        diagnosticsSink?.Emit(new(
            GeometryDiagnosticNames.Stages.BoxTree,
            UnsupportedDiagnosticNames.Events.InlineBlockUnsupportedStructure,
            DiagnosticSeverity.Error,
            payload.Reason,
            null,
            DiagnosticFields.Create(
                DiagnosticFields.Field(GeometryDiagnosticNames.Fields.NodePath, payload.NodePath),
                DiagnosticFields.Field(GeometryDiagnosticNames.Fields.StructureKind, payload.StructureKind),
                DiagnosticFields.Field(GeometryDiagnosticNames.Fields.Reason, payload.Reason),
                DiagnosticFields.Field(
                    GeometryDiagnosticNames.Fields.FormattingContext,
                    DiagnosticValue.FromEnum(payload.FormattingContext))),
            DateTimeOffset.UtcNow));
    }

    private static BoxNode? FindUnsupportedInlineBlockStructure(BoxNode root)
    {
        var rootIsInlineBlockContext = root is BlockBox rootBlock && rootBlock.IsInlineBlockContext;
        var stack = new Stack<(BoxNode Node, bool InInlineBlockContext)>();
        stack.Push((root, rootIsInlineBlockContext));

        while (stack.Count > 0)
        {
            var (current, inInlineBlockContext) = stack.Pop();
            if (inInlineBlockContext && UnsupportedInlineBlockRoles.Contains(current.Role))
            {
                return current;
            }

            var childInlineBlockContext = inInlineBlockContext ||
                                          (current is BlockBox block && block.IsInlineBlockContext);

            for (var i = current.Children.Count - 1; i >= 0; i--)
            {
                stack.Push((current.Children[i], childInlineBlockContext));
            }
        }

        return null;
    }

    private static PublishedBlock? FindUnsupportedInlineBlockStructure(PublishedBlock root)
    {
        var stack = new Stack<(PublishedBlock Block, bool InInlineBlockContext)>();
        stack.Push((root, root.Display.FormattingContext == FormattingContextKind.InlineBlock));

        while (stack.Count > 0)
        {
            var (current, inInlineBlockContext) = stack.Pop();
            if (inInlineBlockContext && UnsupportedInlineBlockRoles.Contains(MapRole(current.Display.Role)))
            {
                return current;
            }

            var childInlineBlockContext = inInlineBlockContext ||
                                          current.Display.FormattingContext == FormattingContextKind.InlineBlock;

            foreach (var child in EnumeratePublishedChildBlocks(current).Reverse())
            {
                stack.Push((child, childInlineBlockContext));
            }
        }

        return null;
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

    private sealed record UnsupportedStructureDiagnostic(
        string NodePath,
        string StructureKind,
        string Reason,
        FormattingContextKind FormattingContext);
}