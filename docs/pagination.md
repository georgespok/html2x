# Pagination in Html2x

## Overview

Html2x applies pagination after block measurement. The layout engine produces measured block fragments first, then `BlockPaginator` distributes them across pages without re-measuring content.

Current behavior:
- First-fit, source-order-preserving pagination.
- Split only at block boundaries.
- No line/image/paragraph internal splitting.
- Page-local coordinates in paginated output.

## Pipeline Position

Pagination runs in `LayoutBuilder` after fragment building:
- DOM/CSSOM -> style tree -> box tree -> fragment tree
- fragment tree blocks -> `BlockPaginator.Paginate(...)`
- pagination result -> `HtmlLayout.Pages`

This keeps measurement deterministic and isolated from page-flow decisions.

## Extension Points

Primary extension points:
- `src/Html2x.LayoutEngine/Pagination/BlockPaginator.cs`
  - Fit policy (`FitsInRemainingSpace`)
  - New-page decision (`ShouldStartNewPage`)
  - Placement strategy (`ResolvePlacementY`)
  - Fragment translation behavior for moved blocks
- `src/Html2x.LayoutEngine/Diagnostics/PaginationDiagnostics.cs`
  - Pagination event emission and payload shape
- `src/Html2x.Abstractions/Diagnostics/PaginationTracePayload.cs`
  - Diagnostics payload contract

Recommended extension approach:
1. Add a failing behavior-focused test in `BlockPaginatorTests`.
2. Implement the smallest paginator/diagnostics change.
3. Keep deterministic iteration and ordering guarantees intact.

## Diagnostics Events

Emitted trace events:
- `layout/pagination/page-created`
- `layout/pagination/block-placed`
- `layout/pagination/block-moved-next-page`
- `layout/pagination/oversized-block`
- `layout/pagination/empty-document`

When serialized through `DiagnosticsSessionSerializer`, payload fields include page and placement details (`pageNumber`, `fragmentId`, `localY`, `remainingSpace*`, `blockHeight`, `reason`).

## Failure Modes and Triage

### 1) Hidden text after page breaks
Symptom:
- Parent block appears on next page, but text appears off-page or overlaps other content.

Cause:
- Moved block Y is updated, but child fragment coordinates are not translated.

Triage:
1. Inspect `layout.snapshot` diagnostics for moved page blocks.
2. Compare parent block bounds to nested line/text Y values.
3. Verify child translation in paginator clone path.

### 2) Missing pagination fields in diagnostics JSON
Symptom:
- `layout.pagination.trace` payload shows only `kind`.

Cause:
- Serializer does not map `PaginationTracePayload` fields.

Triage:
1. Check `DiagnosticsSessionSerializer.MapPayload`.
2. Ensure pagination payload properties are included in the mapped anonymous object.

### 3) TestConsole run fails with missing fonts
Symptom:
- `PdfOptions.FontPath '.\\fonts' does not exist`.

Cause:
- TestConsole run invoked from a working directory without output `fonts` folder.

Triage:
1. Run from `src/Tests/Html2x.TestConsole/bin/<Config>/net8.0`.
2. Or change font path resolution to an absolute output-relative path.

### 4) Unexpected page count drift
Symptom:
- Same input produces different page assignments.

Cause candidates:
- Nondeterministic input ordering before pagination.
- Mutation side effects in fragment reuse/clone paths.

Triage:
1. Re-run determinism tests.
2. Compare ordered placement tuples `(fragmentId, pageNumber, localY, orderIndex)`.
3. Check whether fragment lists are materialized before iteration.
