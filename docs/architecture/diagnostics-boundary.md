# Diagnostics Boundary

This document defines the production dependency boundary for diagnostics.

## Dependency Direction

Diagnostics producers depend on the contracts project only. The diagnostics
runtime depends on the same contracts and owns collection and serialization.

```text
Html2x.LayoutEngine.Style -> Html2x.Diagnostics.Contracts
Html2x.LayoutEngine.Geometry -> Html2x.Diagnostics.Contracts
Html2x.LayoutEngine -> Html2x.Diagnostics.Contracts
Html2x.Renderers.Pdf -> Html2x.Diagnostics.Contracts
Html2x.Diagnostics -> Html2x.Diagnostics.Contracts
Html2x.Abstractions -> no diagnostics types
```

`Html2x.Diagnostics.Contracts` owns only generic emission contracts and
primitive diagnostics values such as `DiagnosticSeverity` and
`DiagnosticContext`. `Html2x.Diagnostics` owns collection, reports,
serialization, and JSON shape. Diagnostic producer modules own the mapper code
that flattens local domain models into diagnostic fields.

## Dependency Rules

1. Diagnostic producer projects may reference `Html2x.Diagnostics.Contracts`.
2. Diagnostic producer projects must not reference `Html2x.Diagnostics`.
3. `Html2x.Diagnostics` must not reference Style, Geometry, LayoutEngine, Renderer, AngleSharp, or SkiaSharp.
4. `Html2x.Abstractions` must not expose diagnostics contracts, collections, report models, snapshots, or serializers.
5. Central diagnostics code must not understand producer-local types such as `TableBox`, geometry boxes, layout fragments, renderer image data, or font resolution internals.
6. Producer-local diagnostics helpers may accept local domain models and convert them into generic diagnostic fields.

## DiagnosticFields Value Rules

`DiagnosticFields` must not accept arbitrary `object`. The allowed value set is intentionally narrow:

- string
- number
- bool
- enum represented as string
- null
- diagnostic array
- nested diagnostic object

These rules keep JSON serialization generic while preventing domain objects from leaking into the central diagnostics package.

## Runtime Flow

Producer projects receive `IDiagnosticsSink?` through method parameters and
reference `Html2x.Diagnostics.Contracts` only. The public facade creates
`DiagnosticsCollector` when diagnostics are enabled and exposes the resulting
`DiagnosticsReport` on `Html2PdfResult`.

Renderer diagnostics flow through the contracts project boundary, the same as
style, geometry, layout, pagination, image, and font diagnostics.

## Runtime Ownership

The sink-based runtime path is owned by `Html2x.Diagnostics`:

- `DiagnosticsCollector` implements `IDiagnosticsSink`.
- `DiagnosticsReport` is the immutable read model returned by the collector.
- `DiagnosticsReportSerializer` serializes `DiagnosticsReport`.

The report serializer must remain generic. It may reference
`Html2x.Diagnostics.Contracts` and diagnostics-owned report types. It must not
reference producer-specific models, snapshot DTOs, producer modules,
AngleSharp, or SkiaSharp.

## Abstractions Boundary

`Html2x.Abstractions` does not contain a diagnostics namespace. Diagnostics
types are split between `Html2x.Diagnostics.Contracts` and
`Html2x.Diagnostics`.

`Html2x.Diagnostics.Contracts` owns:

- `IDiagnosticsSink`
- `DiagnosticRecord`
- `DiagnosticSeverity`
- `DiagnosticContext`
- `DiagnosticFields`
- `DiagnosticObject`
- `DiagnosticArray`
- `DiagnosticValue`
- `NullDiagnosticsSink`

`Html2x.Diagnostics` owns:

- `DiagnosticsCollector`
- `DiagnosticsReport`
- `DiagnosticsReportSerializer`

## Emission Rule

Production code emits diagnostics through
`IDiagnosticsSink.Emit(DiagnosticRecord)`. Producers do not mutate shared
diagnostics collections directly.

## Enforcement

Architecture tests in `Html2x.LayoutEngine.Test` guard this boundary. They
enforce no runtime diagnostics references from producer projects, no forbidden
dependencies in `Html2x.Diagnostics`, no diagnostics types in
`Html2x.Abstractions`, and no direct production mutation of diagnostics
collections.
