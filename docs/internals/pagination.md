# Pagination

Html2x paginates after block measurement. Layout computes measured fragments first, then pagination distributes block fragments across pages without remeasuring content.

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
  -> BlockPaginator
  -> HtmlLayout.Pages
```

## Extension Points

- `BlockPaginator`: fit policy, new-page decisions, placement strategy.
- `FragmentCoordinateTranslator`: cloned translated fragment creation.
- `PaginationDiagnostics`: pagination trace events.
- `PaginationTracePayload`: diagnostics contract.

## Diagnostics Events

- `layout/pagination/page-created`
- `layout/pagination/block-placed`
- `layout/pagination/block-moved-next-page`
- `layout/pagination/oversized-block`
- `layout/pagination/empty-document`

## Guardrails

- Do not mutate source fragments during pagination.
- Preserve fragment width and height.
- Preserve nested text origins, line baselines, image content rectangles, and metadata.
- Add translator coverage before introducing a new fragment runtime type that can be paginated.
