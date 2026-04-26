# Diagnostics Events

This reference lists the main diagnostics event families. Payload contracts live in `Html2x.Abstractions`; JSON export lives in `Html2x.Diagnostics`.

## Stage Lifecycle

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

Geometry snapshots capture box geometry, fragment geometry, and pagination placements for drift analysis.

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

## Images

- `image/render`

Missing and oversized images are warnings. Successful image rendering is informational.

## Fonts

- `font/resolve`

Font diagnostics should identify owner, consumer, request, configured path, resolved source, and outcome.

## Rendering

Render summary payloads include PDF size and page count after `PdfRender` succeeds.
