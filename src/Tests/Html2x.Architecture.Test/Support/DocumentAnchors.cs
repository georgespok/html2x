namespace Html2x.Architecture.Test.Support;

internal static class DocumentAnchors
{
    public static class ArchitectureDiagnostics
    {
        public static readonly DocumentSection DependencyDirection =
            ArchitectureDocuments.ArchitectureDiagnostics.Section("Dependency Direction");

        public static readonly DocumentSection DiagnosticFieldsValueRules =
            ArchitectureDocuments.ArchitectureDiagnostics.Section("DiagnosticFields Value Rules");

        public static readonly DocumentSection EmissionRule =
            ArchitectureDocuments.ArchitectureDiagnostics.Section("Emission Rule");

        public static readonly DocumentSection FacadeBoundary =
            ArchitectureDocuments.ArchitectureDiagnostics.Section("Facade Boundary");

        public static readonly DocumentSection RuntimeFlow =
            ArchitectureDocuments.ArchitectureDiagnostics.Section("Runtime Flow");

        public static readonly DocumentSection RuntimeOwnership =
            ArchitectureDocuments.ArchitectureDiagnostics.Section("Runtime Ownership");
    }

    public static class ArchitectureModuleBoundaries
    {
        public static readonly DocumentSection ContractsStage =
            ArchitectureDocuments.ArchitectureModuleBoundaries.Section("Contracts Stage");

        public static readonly DocumentSection OwnershipMatrix =
            ArchitectureDocuments.ArchitectureModuleBoundaries.Section("Ownership Matrix");
    }

    public static class ArchitecturePipeline
    {
        public static readonly DocumentSection Composition =
            ArchitectureDocuments.ArchitecturePipeline.Section("Composition");

        public static readonly DocumentSection FragmentTreeBuilding =
            ArchitectureDocuments.ArchitecturePipeline.Section("Fragment Tree Building");
    }

    public static class DevelopmentTestingStrategy
    {
        public static readonly DocumentSection OwnershipRules =
            ArchitectureDocuments.DevelopmentTestingStrategy.Section("Ownership Rules");

        public static readonly DocumentSection TestProjects =
            ArchitectureDocuments.DevelopmentTestingStrategy.Section("Test Projects");
    }

    public static class InternalGeometry
    {
        public static readonly DocumentSection BlockFlowLocality =
            ArchitectureDocuments.InternalGeometry.Section("Block Flow Locality");

        public static readonly DocumentSection GeometryOwners =
            ArchitectureDocuments.InternalGeometry.Section("Geometry Owners");

        public static readonly DocumentSection HelperOwnership =
            ArchitectureDocuments.InternalGeometry.Section("Helper Ownership");
    }

    public static class InternalPagination
    {
        public static readonly DocumentSection ModuleSeam =
            ArchitectureDocuments.InternalPagination.Section("Module Seam");
    }

    public static class ReferenceDiagnosticsEvents
    {
        public static readonly DocumentSection Pagination =
            ArchitectureDocuments.ReferenceDiagnosticsEvents.Section("Pagination");
    }
}
