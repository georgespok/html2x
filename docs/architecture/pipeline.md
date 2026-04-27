# Processing Pipeline

This document explains how Html2x turns HTML and CSS into PDF bytes.

## Stage 1: DOM And CSSOM

`AngleSharpDomProvider` parses raw HTML and associated CSS. AngleSharp types stay inside the layout engine. Later stages consume project-owned style and layout models, not parser-specific objects.

Input: raw HTML and CSS.
Output: parsed document and CSS rules ready for cascade evaluation.

## Stage 2: Style Tree

`CssStyleComputer` applies cascade, inheritance, user agent defaults, and value conversion. `StyleTraversal` walks supported elements and materializes project-owned `StyleNode` values. Unsupported or invalid declarations should be represented through diagnostics when diagnostics are enabled.

Input: DOM and CSS rules.
Output: `StyleTree` with computed style snapshots for layout.

## Stage 3: Box Tree And Layout Geometry

`BoxTreeBuilder` owns the style-to-box transition and geometry pass. `InitialBoxTreeBuilder` converts styled nodes into `BoxRole` values such as block, inline, inline-block, table, row, cell, image, list item, and rule. The geometry pass then resolves dimensions, margins, padding, borders, inline layout, image layout, and table placements through the block, inline, and table layout engines.

Input: computed styles.
Output: `BoxTree` with canonical `UsedGeometry`.

## Stage 4: Fragment Projection

`FragmentBuilder` and `BoxToFragmentProjector` project box geometry and render facts into renderer-facing fragments. Fragment projection may resolve fragment font metadata from the converter-owned font source, but it must not remeasure text or reconstruct geometry already owned by layout.

Input: laid-out boxes.
Output: fragments such as blocks, lines, text runs, images, tables, cells, and rules.

## Stage 5: Pagination

`BlockPaginator` places block fragments onto pages. It uses cloned translated fragments for page-local coordinates and must preserve fragment size and metadata.

Input: unpaginated fragment tree.
Output: page models and final `HtmlLayout.Pages`.

## Stage 6: PDF Rendering

`PdfRenderer` consumes `HtmlLayout` and `PdfOptions`, builds paint commands, and draws to a SkiaSharp PDF document.

Input: `HtmlLayout`.
Output: PDF bytes.

## Failure Model

- Parser and option failures can throw before layout begins.
- Unsupported CSS or structures should emit diagnostics and use the documented fallback when possible.
- Contract violations, such as missing required geometry after layout, should fail close to the stage that introduced the invalid state.
