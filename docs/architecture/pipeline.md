# Processing Pipeline

This document explains how Html2x turns HTML and CSS into PDF bytes.

## Stage 1: DOM And CSSOM

`AngleSharpDomProvider` parses raw HTML and associated CSS. AngleSharp types stay inside the layout engine. Later stages consume project-owned style and layout models, not parser-specific objects.

Input: raw HTML and CSS.
Output: parsed document and CSS rules ready for cascade evaluation.

## Stage 2: Style Tree

`CssStyleComputer` applies cascade, inheritance, user agent defaults, and value conversion. Unsupported or invalid declarations should be represented through diagnostics when diagnostics are enabled.

Input: DOM and CSS rules.
Output: computed style snapshots for layout.

## Stage 3: Display And Box Trees

Display tree construction converts styled nodes into layout roles such as block, inline, table, row, cell, image, list item, and rule. Box layout resolves dimensions, margins, padding, borders, inline layout, and table placements.

Input: computed styles.
Output: laid-out boxes with canonical geometry.

## Stage 4: Fragment Projection

Fragment building projects box geometry and render facts into renderer-facing fragments. Fragment builders must not remeasure text or reconstruct geometry already owned by layout.

Input: laid-out boxes.
Output: fragments such as blocks, lines, text runs, images, tables, cells, and rules.

## Stage 5: Pagination

`BlockPaginator` places block fragments onto pages. It may clone and translate fragments for page-local coordinates, but it must preserve fragment size and metadata.

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
