# Contracts And Invariants

This page records the handoff contracts and invariants that keep the pipeline
testable. Use it when adding data to a stage output or changing a subsystem
boundary.

## Stage Handoffs

| Producer | Handoff | Consumer |
| --- | --- | --- |
| Public facade | `StyleBuildSettings`, `LayoutBuildSettings`, `LayoutGeometryRequest`, `PaginationOptions`, `PdfRenderSettings` | Internal stages and renderer |
| Style | `StyleTree` | Geometry |
| Geometry | `PublishedLayoutTree` | Fragment projection |
| Fragment projection | `FragmentTree` | Pagination |
| Pagination | `PaginationResult` and `HtmlLayout` | Converter and renderer |
| Renderer | PDF bytes | Public result |

## Read-Only Handoff Policy

Earlier stage outputs become read-only inputs after handoff. Later stages may
read them, but must not repair, reinterpret, or mutate them.

The style stage is the last stage allowed to interpret parser state. Geometry
is the last stage allowed to write normal-flow geometry. Fragment projection
copies geometry forward. Pagination owns page placement and uses cloned,
translated render model fragments. Paint owns drawing only.

## Geometry Invariants

- Every laid-out block has `UsedGeometry`.
- Mutable layout geometry is published from `UsedGeometry`.
- Fragment rectangles come from published layout geometry.
- Pagination preserves fragment width and height.
- Pagination translation preserves nested baselines, text origins, image
  content rectangles, line occupied rectangles, and block metadata.

## Validation Policy

Raw layout calculations may be normalized at the boundary where they enter
`UsedGeometryRules`. Published geometry is strict. `UsedGeometry` and renderable
fragments should reject non-finite coordinates and negative sizes so invalid
geometry fails close to the producing stage.

## Extension Rule

If a later stage needs data that only exists in an earlier stage, add that data
to the owning stage output consumed by the next stage. Do not add backward
references to parser objects, mutable boxes, fragment projection internals, or
renderer state.

## Diagnostics Rule

Diagnostics may expose source identity and layout identity through primitive
fields only. Diagnostic records should not expose `StyleSourceIdentity`,
`StyleContentIdentity`, `GeometrySourceIdentity`, mutable boxes, fragments, or
producer-local domain models directly.
