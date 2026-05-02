# Processing Pipeline

This document explains how Html2x turns HTML and CSS into PDF bytes.

## Composition

`LayoutBuilder` is the composition layer. It coordinates style, geometry,
fragment projection, pagination, and final layout assembly, but it does not own
HTML parsing or CSS computation directly.

Production dependency direction:

```text
Html2x.RenderModel
  owns pure render facts, documents, and fragments
  does not reference SkiaSharp, parser packages, layout engines, renderers, or filesystem seams

Html2x
  owns public converter options
  maps public options into stage-owned settings and requests

Html2x.LayoutEngine
  uses Html2x.LayoutEngine.Contracts
  uses Html2x.LayoutEngine.Style
  uses Html2x.LayoutEngine.Geometry
  uses Html2x.LayoutEngine.Fragments
  uses Html2x.LayoutEngine.Pagination
  uses Html2x.Text

Html2x.Text
  uses Html2x.RenderModel
  uses Html2x.Diagnostics.Contracts
  uses SkiaSharp and SkiaSharp.HarfBuzz internally
  owns text measurement contracts and font resolution contracts
  does not use facade options, layout engine projects, or renderers

Html2x.LayoutEngine.Geometry
  uses Html2x.LayoutEngine.Contracts
  uses Html2x.Text
  uses Html2x.Diagnostics.Contracts

Html2x.LayoutEngine.Style
  uses Html2x.LayoutEngine.Contracts
  uses Html2x.Diagnostics.Contracts
  uses AngleSharp internally
  consumes StyleBuildSettings instead of public options

Html2x.LayoutEngine.Fragments
  uses Html2x.LayoutEngine.Contracts
  uses Html2x.RenderModel

Html2x.LayoutEngine.Pagination
  uses Html2x.LayoutEngine.Contracts
  uses Html2x.RenderModel
  uses Html2x.Diagnostics.Contracts
  consumes render model block fragments and returns PaginationResult
  owns translated fragment clones and page assembly
  does not use style, geometry implementation engines, fragment projection, parser packages, renderers, or SkiaSharp

Html2x.Renderers.Pdf
  uses Html2x.RenderModel
  uses Html2x.Text
  uses Html2x.Diagnostics.Contracts
  consumes PdfRenderSettings instead of public converter options
```

AngleSharp and AngleSharp.Css are implementation details of
`Html2x.LayoutEngine.Style`. `Html2x.LayoutEngine.Contracts` owns internal
pipeline handoff contracts. Geometry and composition code consume those
project-owned models and must not depend on parser objects.

Image metadata contracts live in `Html2x.LayoutEngine.Contracts` because they
are geometry inputs. Geometry consumes `IImageMetadataResolver` for source,
status, and intrinsic size only. PDF rendering loads image bytes separately
through renderer-owned infrastructure.

Fragment projection lives in `Html2x.LayoutEngine.Fragments`. Composition calls
that module after geometry publishes layout facts. Renderer-facing fragment
models live in `Html2x.RenderModel`.

`Html2x.RenderModel` owns pure render facts: units, style value facts, font
request facts, resolved font facts, renderer-facing documents, renderer-facing
fragments, and render fact translation helpers. It has no project or package
dependencies and must stay free of runtime adapters.

`Html2x.Text` owns text measurement contracts and font resolution contracts. It
is explicitly Skia-backed in this transition: Skia text measurement, font path
resolution, directory font matching, typeface factory seams, and font
diagnostics live in the text module. The PDF renderer consumes resolved font
facts from `TextRun` and loads the referenced typefaces; it does not resolve
fonts through `IFontSource`.

Production composition uses high-level text construction:
`FontPathSource(string)` for the configured font path and
`SkiaTextMeasurer(IFontSource)` for layout measurement. It does not instantiate
file directory or typeface factory seams directly.

`Html2x` owns active public converter options. `HtmlConverterOptions` is the
single public conversion request, with page, resources, CSS, fonts, and
diagnostics groups. `Html2x` is the only production mapping boundary from
public options into `StyleBuildSettings`, `LayoutBuildSettings`,
`LayoutGeometryRequest`, `PaginationOptions`, and `PdfRenderSettings`. That
keeps adapter seams owned by the text module while pure published facts stay
independent from text runtime.

No standalone options module sits between the facade and stages. Public
configuration stays with `Html2x`, while internal settings stay with the stage
that consumes them. Those settings and requests are internal stage request
models.

`FontKey`, `FontWeight`, `FontStyle`, and `ResolvedFont` live in
`Html2x.RenderModel`. They define the shared language for measurement,
resolution, resolved font facts, and renderer typeface loading without making
render facts depend on Skia-backed text infrastructure.

## Style

`Html2x.LayoutEngine.Style` owns raw HTML parsing, user agent stylesheet
application, CSS parsing, supported element traversal, computed style
construction, and style diagnostics.

Input: raw HTML, `StyleBuildSettings`, and an optional `IDiagnosticsSink`.

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
Geometry validation helpers stay inside the geometry module.

`InitialBoxTreeBuilder` converts styled nodes into `BoxRole` values such as
block, inline, inline-block, table, row, cell, image, list item, and rule. The
geometry pass then resolves dimensions, margins, padding, borders, inline
layout, image layout, and table placements.

Input: `StyleTree` and layout geometry options.

Output: contract-owned `PublishedLayoutTree`.

Published inline text runs include the resolved font facts used during geometry
measurement. Later stages must consume those facts instead of re-resolving font
identity for normal pipeline text.

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
- Renderer-facing documents and fragments remain independent of style
  implementation types and geometry source identity implementation types.

`Html2x.LayoutEngine.Contracts` also owns `LayoutGeometryRequest`, image
metadata contracts, `UsedGeometry`, shared HTML and CSS vocabulary constants
used across style and geometry, source identity records, and published layout
facts. It must not reference parser packages, geometry implementation projects,
fragment implementation code, renderer implementation code, diagnostics
serializers, or mutable box types.

## Fragment Projection

`Html2x.LayoutEngine.Fragments` owns the projection from published layout facts
into render model fragments. `FragmentBuilder` consumes `PublishedLayoutTree`,
allocates fragment IDs, and copies style, geometry, and published text run facts
into fragment models.

Input: `PublishedLayoutTree`.

Contract summary: PublishedLayoutTree in, FragmentTree out.

Output: `FragmentTree`, containing blocks, lines, text runs, images, tables,
cells, and rules.

Fragment projection does not consume mutable boxes, CSS parser state, DOM
objects, text or font adapter seams, pagination pages, or renderer state. It
must not remeasure text, resolve fonts, or reconstruct geometry already owned by
layout.

## Pagination

`Html2x.LayoutEngine.Pagination` owns page placement. `LayoutPaginator`
consumes measured render model block fragments plus `PaginationOptions` and
returns `PaginationResult`. The result contains the final `HtmlLayout` and
stable audit facts for page and placement diagnostics.

Input: unpaginated render model block fragments.

Output: `PaginationResult`.

Pagination owns translated fragment clones and page assembly. Source fragments
remain read-only inputs. The current internal algorithm is `BlockPaginator`,
which is block-boundary only: it moves whole block fragments between pages and
does not split lines, images, table rows, or paragraphs internally.

## PDF Rendering

`PdfRenderer` consumes `HtmlLayout` and `PdfRenderSettings`, builds paint
commands, and draws to a SkiaSharp PDF document.

Input: `HtmlLayout`.

Output: PDF bytes.

Renderer projects must not reference style implementation details or mutable
geometry internals. Text runs must carry `ResolvedFont` facts before rendering.
If rendering needs more data, add it to the published layout or fragment
contract in the owning stage.

## Failure Model

- Parser and option failures can throw before layout begins.
- Unsupported CSS or structures should emit diagnostics and use the documented
  fallback when possible.
- Contract violations, such as missing required geometry after layout, should
  fail close to the stage that introduced the invalid state.
