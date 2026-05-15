# Diagnostics Events

This reference lists diagnostics record families, common fields, and a compact
JSON example. Generic contracts live in `Html2x.Diagnostics.Contracts`; JSON
export lives in `Html2x.Diagnostics`.

## Record Shape

Diagnostics are emitted as `DiagnosticRecord` values. A serialized record has
these stable concepts:

- Stage or owner.
- Event name.
- Severity.
- Message when useful.
- Context fields such as source path or node path when available.
- Event-specific fields under the generic diagnostics value model.

`DiagnosticFields` values may contain strings, numbers, booleans, enum names as
strings, nulls, diagnostic arrays, or nested diagnostic objects.

Timing is recorded on the report envelope as `startTime` and `endTime`.
Individual records do not include a per-record timestamp.

## Example JSON

```json
{
  "startTime": "2026-05-08T10:15:30+00:00",
  "endTime": "2026-05-08T10:15:31+00:00",
  "records": [
    {
      "stage": "stage/pagination",
      "name": "layout/pagination/page-created",
      "severity": "Info",
      "message": "Created page 2.",
      "fields": {
        "eventName": "layout/pagination/page-created",
        "pageNumber": 2
      }
    }
  ]
}
```

## Conversion Lifecycle

- `Configuration`
- `LayoutBuild`
- `PdfRender`

Lifecycle states:

- `Started`
- `Succeeded`
- `Failed`
- `Skipped`
- `Canceled`

Facade lifecycle fields:

- `LayoutBuild` `stage/started`: `htmlLength`, plus `html` and
  `htmlTruncated` only when raw HTML diagnostics are enabled.
- `LayoutBuild` `stage/succeeded`: `snapshot`.
- `PdfRender` `stage/succeeded`: `pdfSize` and `pageCount`.

Internal layout lifecycle records may use compatibility stage values such as
`stage/dom`, `stage/style`, `stage/box-tree`, `stage/fragment-tree`, and
`stage/pagination`. These serialized values are compatibility vocabulary, not
current implementation class names.

If configuration fails after diagnostics collection starts, `Configuration`
emits `stage/failed`, while `LayoutBuild` and `PdfRender` emit
`stage/skipped`. If `LayoutBuild` fails or is canceled, `PdfRender` is skipped.
If `PdfRender` fails or is canceled after it starts, the report is attached to
the thrown exception and no downstream stage is emitted.

## Style

- `style/unsupported-declaration`
- `style/unsupported-element`
- `style/ignored-declaration`
- `style/partially-applied-declaration`

Style diagnostics explain applied, ignored, partially applied, and unsupported
CSS declarations. Unsupported HTML elements are flattened into supported content
when possible and emit `style/unsupported-element`.

## Layout And Geometry

- `layout/geometry-snapshot`
- `layout/margin-collapse`
- `layout/unsupported-mode`

Geometry snapshots capture box geometry, fragment geometry, and pagination
audit placements for drift analysis. Pagination placement entries include
`decisionKind`, `isOversized`, placed rectangle fields, and metadata ownership
facts. `metadataConsumer` uses the stable value `Pagination`; it does not name
the private clone implementation. `metadataOwner` may still use the stable
value `FragmentBuilder` for fragment metadata created before pagination. Box
entries include
`establishesInlineBlockFormattingContext` when the box starts an inline-block
formatting context.

## Tables

- `layout/table`
- `layout/table/unsupported-structure`

Table diagnostics describe supported table decisions and unsupported structures such as invalid spans or unsupported row spans. Supported table cell facts include `columnIndex`, `columnSpan`, header identity, width, and height.

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

The `src` field is a bounded diagnostic display source, not guaranteed to be
the exact authored source. Data URI payloads are omitted from `src`; rooted or
parent-traversal paths may be reduced to a path display with the file name.
Raw image source context is omitted by default. When
`DiagnosticsOptions.IncludeRawHtml` is enabled, `context.rawUserInput` may
contain the raw image source capped by `DiagnosticsOptions.MaxRawHtmlLength`.

Known status values:

- `Ok`
- `Missing`
- `Oversized`
- `InvalidDataUri`
- `DecodeFailed`
- `OutOfScope`

## Fonts

- `font/resolve`

Font diagnostics should identify owner, consumer, request, configured path, resolved source, and outcome.

## Rendering

Render summary fields include PDF size and page count after `PdfRender` succeeds.
