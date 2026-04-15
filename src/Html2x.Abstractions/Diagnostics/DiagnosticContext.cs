namespace Html2x.Abstractions.Diagnostics;

public sealed record DiagnosticContext(
    string? Selector,
    string? ElementIdentity,
    string? StyleDeclaration,
    string? StructuralPath,
    string? RawUserInput);
