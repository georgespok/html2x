# Release Notes

## Unreleased

### Added
- Block-level pagination with ordered multi-page flow in the layout pipeline.
- Page model and placement model (`PageModel`, `BlockFragmentPlacement`, `PaginationResult`).
- Pagination diagnostics events:
  - `layout/pagination/page-created`
  - `layout/pagination/block-placed`
  - `layout/pagination/block-moved-next-page`
  - `layout/pagination/oversized-block`
  - `layout/pagination/empty-document`
- Scenario coverage for deterministic pagination snapshots.
- Manual TestConsole sample: `pagination-block-flow.html`.
- Baseline table rendering for simple non-spanning tables with equal-width column derivation, borders, padding, header cell identity, row backgrounds, and cell background override.
- Table diagnostics for supported and unsupported table outcomes via `layout/table` and `layout/table/unsupported-structure`.
- Manual TestConsole sample: `basic-table-structure.html`.
- Diagnostics export coverage for known payload fields, unknown payload kind preservation, layout visual snapshot fields, granular table context, pagination severity/context, and canonical image render diagnostics.

### Changed
- `LayoutBuilder` now emits paginated `HtmlLayout.Pages` from fragment output.
- Empty/whitespace HTML input returns one empty page through pagination path.
- Diagnostics JSON serialization now includes full pagination trace payload fields.
- Image render diagnostics now use the canonical `image/render` event name with severity, status, rendered size, source context, and raw source input. This replaces the previous `ImageRender` event name.

### Fixed
- Moved blocks now translate nested child fragment coordinates (line/text/image/rule) with parent page relocation, preventing hidden or overlapping text after page breaks.
