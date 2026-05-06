# Geometry

Geometry must be computed once during layout, then carried forward without reinterpretation.

`Html2x.LayoutEngine.Geometry` consumes parser-free `StyleTree` input from
`Html2x.LayoutEngine.Contracts.Style`. It may read `ComputedStyle`,
`StyledElementFacts`, and ordered `StyleContentNode` values. It must not parse
HTML or CSS, reference AngleSharp, or traverse DOM nodes.

## Authority

`UsedGeometry` is the canonical geometry value for block-level layout nodes. It carries:

- Border box rectangle.
- Content box rectangle.
- Baseline when available.
- Marker offset.
- Overflow allowance.

`UsedGeometryRules` owns geometry construction and normalization at layout boundaries. It handles finite-value normalization, non-negative sizes, content rectangle calculation, marker offsets, padding, and borders.

## Helper Ownership

Point-space geometry is owned by `Html2x.RenderModel` through `RectPt`,
`PointPt`, and `SizePt`. Pagination uses `RectPt.Translate` and
`PointPt.Translate` when translating cloned render model rectangles and text
run origins to page-local coordinates.

`UsedGeometry` translation remains geometry-owned in
`Html2x.LayoutEngine.Geometry`. Geometry may translate `UsedGeometry` because it
has to preserve geometry invariants through `UsedGeometryRules`.
Production geometry placement should route `UsedGeometry` translation through
`GeometryTranslator`; direct `UsedGeometry` transformation helpers are
compatibility conveniences, not the place for new layout behavior.

Page content area calculation is a layout-owned fact in
`Html2x.LayoutEngine.Contracts`. `PageContentArea` is shared by geometry and
pagination so page margin normalization has one implementation.

Naming:

- `BorderBoxRect` is the painted border box.
- `ContentBoxRect` is the raw content box after padding and border are removed.
- `ContentFlowArea` is the marker-adjusted content area used for child block and inline flow placement.
- Pagination placement `PageX` and `PageY` are translated page coordinates derived from the placed fragment.

## Naming Grammar

Repository-wide naming rules live in `docs/development/coding-standards.md`.
Geometry applies that shared grammar with these stage-specific meanings:

- `Construction`: creates the internal mutable box tree from style facts. It may
  create generated boxes, normalize text, assign source identity, and add list
  markers. It must not place geometry.
- `Layout`: places boxes and produces geometry. It may route mutation through a
  writer.
- `Measurement`: computes size or extent facts without mutating boxes.
- `Rules`: pure domain decisions and scalar calculations.
- `Writer`: the only module kind that mutates boxes or writes published facts.
- `Request`, `Result`, and `Facts`: data that crosses module seams.
- `Rule`: a block-kind adapter only.

Avoid introducing broad implementation-pattern suffixes in Geometry unless the
repository-wide naming rules allow the exception. The current source-level
Geometry exception is `LayoutGeometryBuilder`, which remains the stage entry
type. Historical diagnostic owner and consumer strings may still use retired
names for compatibility.

## Flow

Human-readable target:

```text
Style tree
  -> BoxTreeConstruction builds the mutable internal box tree
  -> BoxTreeLayout places the box tree
  -> PublishedLayoutWriter writes published layout facts
  -> fragments copy published geometry
  -> pagination translates fragment coordinates
  -> renderer draws fragment rectangles
```

Current implementation during the geometry redesign:

```text
Style tree
  -> BoxTreeConstruction builds mutable layout boxes from StyledElementFacts and StyleContentNode
  -> GeometryPipelineComposer creates page facts and layout services
  -> BoxTreeLayout selects the page content area and top-level layout candidates
  -> BlockBoxLayout coordinates the selected block stack through BlockFlowLayout and BlockLayoutRuleSet
  -> block rules resolve UsedGeometry and route mutable writes through LayoutBoxStateWriter
  -> PublishedLayoutWriter creates the published layout tree
  -> fragments copy published geometry
  -> pagination translates fragment coordinates
  -> renderer draws fragment rectangles
```

Fragments, pagination, diagnostics, and renderers must not recompute padding,
border, content rectangles, marker offsets, or block sizes. They may translate
already-published rectangles when pagination moves a fragment subtree to another
page.

## Block Flow Locality

These names are the current implementation. The target grammar has normalized
the main path around `BoxTreeConstruction`, `BoxTreeLayout`,
`BlockBoxLayout`, and `BlockFlowLayout`.

`BoxTreeLayout` owns the constructed box-root and page-content-area
resolution step. It selects top-level layout candidates, creates the page facts,
and asks block layout to resolve the selected stack.

`BlockBoxLayout` coordinates individual block layout, block-kind rule
dispatch, default block rule ordering, and block publication. Block-flow
locality lives in smaller internal modules:

- `BlockFlowLayout` owns laid-out block-flow sequencing: cursor state,
  margin collapse, inline flushing, child ordering, and flow item ordering.
  It receives child block layout as a delegate and does not know rule-set or
  block-kind rule types.
- `BlockFlowMeasurement` owns non-mutating stacked block measurement so
  layout and measurement share the same block-flow policy.
- `BlockLayoutRuleSet` selects the internal rule for supported block kinds.
- `StandardBlockLayoutRule`, `ImageBlockLayoutRule`, `RuleBlockLayoutRule`, and
  `TableBlockLayoutRule` localize block-kind behavior.
- `BlockSizingRules` produces shared block sizing facts for layout and
  measurement.
- `ImageSizingRules` resolves replaced image sizing facts.
- `ImageBlockLayoutWriter` writes image metadata and image block geometry.
- `TableGridLayout` produces supported table row and cell geometry without
  mutating source boxes.
- `TableCellMeasurement` measures table cell content without mutation.
- `TableBlockLayout` owns table diagnostics and table placement.
- `TablePlacementWriter` writes table, row, and cell geometry.
- `LayoutBoxStateWriter` owns mutable writes to block, image, table, inline
  layout, and atomic inline box content.
- `PublishedLayoutWriter` owns published block caching, source order, rule
  result publication, inline flow item publication, and inline publishing.

Extension boundaries:

- Add new block behavior by adding an internal `IBlockLayoutRule` and placing it
  in `BlockBoxLayout.CreateDefaultRuleSet`.
- Keep table column, row span, and col span behavior in `TableStructure` and
  `TableGridLayout` before touching placement or publication.
- Keep replaced inline box measurement in `AtomicInlineBoxLayout` and
  placement in `AtomicInlineBoxPlacementWriter`; text line layout should consume
  only atomic inline box metrics.
- Block-kind rules must not publish directly. Measurement modules must not
  mutate boxes or write published facts.

Normalization status:

- `BoxTreeConstruction` builds the internal box tree and owns generated boxes,
  text normalization, list markers, and source identity.
- `BoxTreeLayout` owns top-level box-tree placement.
- `BlockBoxLayout` owns block-kind layout dispatch and publication.
- `BlockFlowLayout` owns normal block-flow sequencing.
- `InlineFlowLayout` owns inline flow layout and explicit inline measurement
  over shared run construction and text line layout.
- `BlockContentExtentMeasurement`, `BlockContentSizeMeasurement`,
  `BlockContentHeightMeasurement`, and `BlockContentSizeFacts` separate
  aggregate extent measurement, single-block size measurement, height-only
  measurement, and carried facts.
- `MarginCollapseRules` owns margin collapse policy and diagnostics while
  preserving historical owner string compatibility.

## Compatibility Fields

`BlockBox` layout facts such as margin, padding, inline layout, used geometry, and inline-block context are internal mutable compatibility state. New downstream code should consume `PublishedLayoutTree` or render model fragments, not these fields.

Table layout may still expose scalar row and cell metadata for fragment projection. New code should prefer `UsedGeometry` and published table facts.

## Validation Policy

Layout construction is forgiving at the boundary where raw layout calculations enter `UsedGeometryRules`. It may clamp or normalize invalid intermediate values.

Published geometry is strict. `UsedGeometry` and renderable fragments should reject non-finite coordinates and negative sizes so invalid geometry fails close to the producing stage.

## Invariants

- Every laid-out block has `UsedGeometry`.
- Mutable layout geometry is published from `UsedGeometry`.
- Fragment rectangles come from layout geometry.
- Pagination preserves width and height.
- Pagination translation preserves nested baselines, text origins, image content rectangles, line occupied rectangles, and block-like metadata.

## Diagnostics

When diagnostics are enabled, `layout/geometry-snapshot` captures box geometry, fragment geometry, and pagination placements. Use it to investigate drift between layout, fragment projection, and pagination.

The snapshot metadata owner value for box geometry remains
`BlockLayoutEngine` for compatibility with existing diagnostics consumers. That
string is historical metadata, not the current Geometry class name.
