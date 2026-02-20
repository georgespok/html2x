using Html2x.Abstractions.Layout.Fragments;

namespace Html2x.Abstractions.Diagnostics;

public sealed class UnsupportedStructurePayload : IDiagnosticsPayload
{
    public string Kind => "layout.inline-block.unsupported-structure";

    public string NodePath { get; init; } = string.Empty;

    public string StructureKind { get; init; } = string.Empty;

    public string Reason { get; init; } = string.Empty;

    public FormattingContextKind FormattingContext { get; init; } = FormattingContextKind.InlineBlock;
}
