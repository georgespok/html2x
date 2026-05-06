namespace Html2x.Diagnostics.Contracts;

public sealed record DiagnosticContext(
    string? Selector,
    string? ElementIdentity,
    string? StyleDeclaration,
    string? StructuralPath,
    string? RawUserInput);