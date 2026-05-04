# Pagination

Html2x paginates after block measurement. Layout computes measured fragments
first, then `Html2x.LayoutEngine.Pagination` distributes block fragments across
pages without remeasuring content.

## Current Behavior

- First-fit pagination.
- Source-order-preserving placement.
- Split only at block boundaries.
- No line, image, or paragraph internal splitting.
- Page-local coordinates in final output.

## Pipeline Position

```text
DOM/CSSOM
  -> style tree
  -> box tree
  -> fragment tree
  -> LayoutPaginator
  -> PaginationResult
       -> HtmlLayout
       -> pagination audit facts
```

## Module Seam

- `LayoutPaginator`: internal pagination module seam.
- `PaginationOptions`: page size and page margin input facts.
- `PaginationResult`: final `HtmlLayout` plus stable audit facts.
- `PaginationDecisionKind`: stable decision vocabulary for audit records.

`LayoutPaginator.Paginate` accepts measured render model block fragments. It
does not remeasure content and does not repair source geometry. Callers receive
one `PaginationResult`:

- `Layout`: final `HtmlLayout` with `LayoutPage` values and page-local cloned
  fragments.
- `AuditPages`: page content area, page margin, and placement facts for
  diagnostics and drift analysis.
- `TotalPages` and `TotalPlacements`: convenience counts derived from audit
  facts.

`PaginationPlacementAudit` records fragment ID, page number, placed rectangle,
source order index, decision kind, oversized status, fragment kind, and copied
metadata facts such as display role, formatting context, table indices, and
marker offset.

Internal implementation details:

- `BlockPaginator`: current first-fit block-boundary algorithm.
- `FragmentPlacementCloner`: cloned translated fragment creation.
- `PaginationDiagnostics`: pagination trace event emission.

The pagination result does not expose `FragmentPlacementCloner` or internal
block placement models.

The current algorithm is intentionally block-boundary only. It may move a whole
block fragment to the next page and may mark a block as oversized, but it does
not split text lines, images, table rows, or paragraphs internally.

## Diagnostics Events

- `layout/pagination/page-created`
- `layout/pagination/block-placed`
- `layout/pagination/block-moved-next-page`
- `layout/pagination/oversized-block`
- `layout/pagination/empty-document`

Pagination emits these events through `IDiagnosticsSink` and depends only on
`Html2x.Diagnostics.Contracts`, not the diagnostics runtime.

Geometry snapshots consume `PaginationResult.AuditPages`. Placement metadata
uses `metadataConsumer = "Pagination"` as stable vocabulary. Fragment metadata
ownership may still identify `FragmentBuilder` because fragment metadata is
created before pagination.

## Guardrails

- Do not mutate source fragments during pagination.
- Preserve fragment width and height.
- Preserve nested text origins, line baselines, image content rectangles, and metadata.
- Add translator coverage before introducing a new fragment runtime type that can be paginated.
