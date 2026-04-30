# Processing Pipeline

This document explains how Html2x turns HTML and CSS into PDF bytes.

## Composition

`LayoutBuilder` is the composition layer. It coordinates style, geometry,
fragment projection, pagination, and final layout assembly, but it does not own
HTML parsing or CSS computation directly.

Production dependency direction:

```text
Html2x.LayoutEngine
  uses Html2x.LayoutEngine.Contracts
  uses Html2x.LayoutEngine.Style
  uses Html2x.LayoutEngine.Geometry
  uses Html2x.LayoutEngine.Fragments

Html2x.LayoutEngine.Geometry
  uses Html2x.LayoutEngine.Contracts
  uses Html2x.Diagnostics.Contracts

Html2x.LayoutEngine.Style
  uses Html2x.LayoutEngine.Contracts
  uses Html2x.Diagnostics.Contracts
  uses AngleSharp internally

Html2x.LayoutEngine.Fragments
  uses Html2x.Abstractions
  uses Html2x.LayoutEngine.Contracts

Html2x.Renderers.Pdf
  uses Html2x.Abstractions
  uses Html2x.Diagnostics.Contracts
```

AngleSharp and AngleSharp.Css are implementation details of
`Html2x.LayoutEngine.Style`. `Html2x.LayoutEngine.Contracts` owns internal
pipeline handoff contracts. Geometry and composition code consume those
project-owned models and must not depend on parser objects.

Fragment projection lives in `Html2x.LayoutEngine.Fragments`. Composition calls
that module after geometry publishes layout facts. Renderer-facing fragment
models still live in `Html2x.Abstractions.Layout.Fragments`.

## Style

`Html2x.LayoutEngine.Style` owns raw HTML parsing, user agent stylesheet
application, CSS parsing, supported element traversal, computed style
construction, and style diagnostics.

Input: raw HTML, `LayoutOptions`, and an optional `IDiagnosticsSink`.

Output: contract-owned `StyleTree`.

The `StyleTree` is the parser-free handoff to geometry and is owned by
`Html2x.LayoutEngine.Contracts`:

- `StyleTree.Root` is the body-rooted style tree.
- `StyleNode.Element` is `StyledElementFacts`.
- `StyledElementFacts` carries tag, local name, id, class attribute, and
  case-insensitive attributes required by layout.
- `StyleNode.Identity` is `StyleSourceIdentity`, assigned during parser
  traversal before geometry starts.
- `StyleNode.Content` preserves ordered `StyleContentNode` values for text,
  elements, and line breaks.
- `StyleContentNode.Identity` is `StyleContentIdentity`, assigned for every
  ordered text, element, and line break content item.
- `StyleNode.Children` preserves supported styled element children.
- Unsupported parser elements are flattened by the style module before geometry
  consumes content.

Style owns parser traversal and source identity assignment. Source paths are
diagnostic labels created from style ancestry. Geometry must consume them, not
rebuild them from parser state or CSS selectors.

## Layout Geometry

`Html2x.LayoutEngine.Geometry` consumes contract `StyleTree` input and resolves
layout facts. The module may read computed style values, `StyledElementFacts`,
and ordered style content from `Html2x.LayoutEngine.Contracts`. It must not read
DOM nodes, `IElement`, `INode`, child node collections, or AngleSharp types.

`InitialBoxTreeBuilder` converts styled nodes into `BoxRole` values such as
block, inline, inline-block, table, row, cell, image, list item, and rule. The
geometry pass then resolves dimensions, margins, padding, borders, inline
layout, image layout, and table placements.

Input: `StyleTree` and layout geometry options.

Output: contract-owned `PublishedLayoutTree`.

Source identity flow:

- Geometry consumes `StyleTree` and copies source identity into
  `BoxNode.SourceIdentity`.
- Geometry creates generated source identity for anonymous text boxes, list
  markers, inline-block content boxes, anonymous block wrappers, and other
  generated layout nodes.
- Published layout carries both layout identity and source identity.
  `PublishedBlockIdentity.NodePath` remains the layout path, while
  `PublishedBlockIdentity.SourceIdentity` carries the source identity.
- Published inline sources use the same split: `NodePath` is layout identity
  and `SourceIdentity` is source identity.
- Diagnostics may project source identity through primitive diagnostic fields.
- Renderer-facing fragments remain independent of style implementation types
  and geometry source identity implementation types.

`Html2x.LayoutEngine.Contracts` also owns `LayoutGeometryRequest`,
`UsedGeometry`, shared HTML and CSS vocabulary constants used across style and
geometry, source identity records, and published layout facts. It must not
reference parser packages, geometry implementation projects, fragment
implementation code, renderer implementation code, diagnostics serializers, or
mutable box types.

## Fragment Projection

`Html2x.LayoutEngine.Fragments` owns the projection from published layout facts
into renderer-facing fragments. `FragmentBuilder` consumes `PublishedLayoutTree`
and `IFontSource`, allocates fragment IDs, copies style and geometry into
fragment models, and resolves text run fonts.

Input: `PublishedLayoutTree` and `IFontSource`.

Contract summary: PublishedLayoutTree and IFontSource in, FragmentTree out.

Output: `FragmentTree`, containing blocks, lines, text runs, images, tables,
cells, and rules.

Fragment projection does not consume mutable boxes, CSS parser state, DOM
objects, pagination pages, or renderer state. It must not remeasure text or
reconstruct geometry already owned by layout.

## Pagination

`BlockPaginator` places block fragments onto pages. It uses cloned translated
fragments for page-local coordinates and must preserve fragment size and
metadata.

Input: unpaginated fragment tree.

Output: page models and final `HtmlLayout.Pages`.

## PDF Rendering

`PdfRenderer` consumes `HtmlLayout` and `PdfOptions`, builds paint commands, and
draws to a SkiaSharp PDF document.

Input: `HtmlLayout`.

Output: PDF bytes.

Renderer projects must not reference style implementation details or mutable
geometry internals. If rendering needs more data, add it to the published layout
or fragment contract in the owning stage.

## Failure Model

- Parser and option failures can throw before layout begins.
- Unsupported CSS or structures should emit diagnostics and use the documented
  fallback when possible.
- Contract violations, such as missing required geometry after layout, should
  fail close to the stage that introduced the invalid state.
