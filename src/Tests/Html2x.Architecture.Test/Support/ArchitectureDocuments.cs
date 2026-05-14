namespace Html2x.Architecture.Test.Support;

internal static class ArchitectureDocuments
{
    private const string Docs = "docs";
    private const string Architecture = "architecture";
    private const string Development = "development";
    private const string Internals = "internals";
    private const string Reference = "reference";

    public static readonly DocumentReference ArchitectureDiagnostics =
        new(Docs, Architecture, "diagnostics.md");

    public static readonly DocumentReference ArchitectureModuleBoundaries =
        new(Docs, Architecture, "module-boundaries.md");

    public static readonly DocumentReference ArchitecturePipeline =
        new(Docs, Architecture, "pipeline.md");

    public static readonly DocumentReference DevelopmentTestingStrategy =
        new(Docs, Development, "testing-strategy.md");

    public static readonly DocumentReference InternalGeometry =
        new(Docs, Internals, "geometry.md");

    public static readonly DocumentReference InternalPagination =
        new(Docs, Internals, "pagination.md");

    public static readonly DocumentReference ReferenceDiagnosticsEvents =
        new(Docs, Reference, "diagnostics-events.md");
}
