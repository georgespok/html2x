# Diagnostics Events

This reference lists the main diagnostics record families. Generic contracts
live in `Html2x.Diagnostics.Contracts`; JSON export lives in
`Html2x.Diagnostics`.

## Conversion Lifecycle

- `LayoutBuild`
- `PdfRender`

Lifecycle states:

- `Started`
- `Succeeded`
- `Failed`
- `Skipped`

## Style

- `style/unsupported-declaration`
- `style/ignored-declaration`
- `style/partially-applied-declaration`

Style diagnostics explain applied, ignored, partially applied, and unsupported CSS declarations.

## Layout And Geometry

- `layout/geometry-snapshot`
- `layout/margin-collapse`
- `layout/unsupported-mode`

Geometry snapshots capture box geometry, fragment geometry, and pagination
audit placements for drift analysis. Pagination placement entries include
`decisionKind`, `isOversized`, placed rectangle fields, and metadata ownership
facts. `metadataConsumer` uses the stable value `Pagination`; it does not name
the private clone implementation.

## Tables

- `layout/table`
- `layout/table/unsupported-structure`

Table diagnostics describe supported table decisions and unsupported structures such as spans or non-rectangular grids.

## Pagination

- `layout/pagination/page-created`
- `layout/pagination/block-placed`
- `layout/pagination/block-moved-next-page`
- `layout/pagination/oversized-block`
- `layout/pagination/empty-document`

Pagination records are emitted by `Html2x.LayoutEngine.Pagination` through
`IDiagnosticsSink`. Event names, severity, and fields are owned by that module.
All pagination records use stage `stage/pagination` and structural paths such
as `page[2]` or `page[2]/fragment[32]`.

Common fields:

- `eventName`
- `pageNumber`
- `fragmentId`
- `reason`

Move and placement fields:

- `fromPage`
- `toPage`
- `localY`
- `remainingSpace`
- `remainingSpaceBefore`
- `remainingSpaceAfter`
- `blockHeight`
- `pageContentHeight`

Geometry snapshots use `PaginationDecisionKind` values for stable audit
vocabulary:

- `Placed`
- `MovedToNextPage`
- `Oversized`
- `SplitAcrossPages`
- `ForcedBreak`

The last two values are reserved vocabulary only; current pagination remains
block-boundary only.

## Images

- `image/render`

Recoverable image failures are warnings. Successful image rendering is informational.

Known status values:

- `Ok`
- `Missing`
- `Oversize`
- `InvalidDataUri`
- `DecodeFailed`
- `OutOfScope`

## Fonts

- `font/resolve`

Font diagnostics should identify owner, consumer, request, configured path, resolved source, and outcome.

## Rendering

Render summary fields include PDF size and page count after `PdfRender` succeeds.
