# Stage Ownership

This document defines internal ownership rules for the HTML to PDF pipeline. It
is for developers changing the codebase, not a public API contract.

## Ownership Matrix

| Stage | Project | Input | Owned Output | May Write | Must Not Write |
| --- | --- | --- | --- | --- | --- |
| Public Facade | `Html2x` | Consumer configuration consumed by the converter facade | Option contracts and mapping to stage-owned settings | `HtmlConverterOptions`, page options, resource options, CSS options, font options, diagnostics options, and facade mapping | Layout algorithms, renderer algorithms, runtime adapters, documents, fragments, layout contracts, geometry guards, diagnostics runtime, internal stage request models |
| Render Model | `Html2x.RenderModel` | None | Pure render facts | Units, style value facts, font request facts, resolved font facts, documents, and fragments | Runtime adapters, parser traversal, CSS computation, mutable boxes, layout algorithms, pagination algorithms, renderer state |
| Contracts | `Html2x.LayoutEngine.Contracts` | None | Internal pipeline handoff contracts | Immutable contract facts and validation helpers | Parser traversal, CSS computation, mutable boxes, layout algorithms, fragments, pagination pages, renderer state |
| Resources | `Html2x.Resources` | Image source, base directory, and byte size limit | Image metadata, loaded image bytes, load status, and intrinsic image size | Scoped path resolution, data URI parsing, byte limit checks, byte loading, and intrinsic image dimension decoding | Layout geometry, PDF drawing, diagnostics collection, public converter options |
| Text | `Html2x.Text` | Font requests, text measurement requests, file directory access, and diagnostics sink | Text measurement contracts and font resolution contracts | Text measurement seams, Skia-backed text measurement, font path resolution, font diagnostics, directory font matching, and typeface factory seams | Parser traversal, CSS computation, mutable boxes, layout engine implementation projects, fragment projection, pagination pages, renderer state |
| Style | `Html2x.LayoutEngine.Style` | Raw HTML, `StyleBuildSettings`, optional diagnostics sink | Contract `StyleTree` with `StyledElementFacts`, ordered `StyleContentNode` entries, and computed styles | AngleSharp document loading, user agent stylesheet application, CSS parsing, computed style construction, CSS length mapping, and style diagnostics | Box hierarchy, layout geometry, fragments, pagination pages, renderer state |
| Layout Geometry | `Html2x.LayoutEngine.Geometry` | Contract `StyleTree`, layout geometry request, and image metadata resolver | Contract `PublishedLayoutTree` and internal box geometry | Box roles, text runs with resolved font facts, list markers, unsupported mode diagnostics, image metadata resolution, image layout facts, table placements, `UsedGeometry` | CSS parsing, DOM traversal, parser objects, fragments, pagination pages, renderer state |
| Fragment | `Html2x.LayoutEngine.Fragments` | Contract `PublishedLayoutTree` | Fragment tree | Published layout traversal, fragment ID allocation, `VisualStyle` projection, and copying published text run facts | Mutable boxes, CSS, DOM, text or font adapter seams, pagination pages, renderer state |
| Pagination | `Html2x.LayoutEngine.Pagination` | Render model block fragments and `PaginationOptions` | `PaginationResult` with final `HtmlLayout` and audit facts | Translated fragment clones, `LayoutPage` assembly, page audit facts, placement audit facts, and pagination diagnostics | Source fragments, mutable boxes, style facts, geometry implementation engines, fragment projection, parser state, renderer state |
| Paint | `Html2x.Renderers.Pdf` | `HtmlLayout` and `PdfRenderSettings` | Paint commands and PDF bytes | Renderer-local settings, commands, and Skia objects | Layout pages, fragments, boxes, styles, parser objects, public converter options |

## Render Model Stage

Html2x.RenderModel owns pure render facts such as `SizePx`, `SizePt`,
`PaperSizes`, `ColorRgba`, `Spacing`, borders, `VisualStyle`, `FontKey`,
`FontWeight`, `FontStyle`, `ResolvedFont`, `HtmlLayout`, `LayoutPage`,
and renderer-facing fragments. These facts can be shared by
layout, text, fragment projection, pagination, renderers, options, and tests
without taking a dependency on runtime behavior.

The render model must not reference SkiaSharp, filesystem seams, diagnostics
runtime, parser packages, layout implementation projects, fragment projection,
or renderers. It may contain small value helpers on the facts themselves, but
not behavior-changing adapters or CSS parsers. `ColorRgba` is a pure color
value fact; CSS color syntax is interpreted by the style stage before render
facts are published.

## Public Facade

Html2x owns the active public converter options. `HtmlConverterOptions` is the
single public conversion request, and its groups reflect consumer concepts:
page, resources, CSS, fonts, and diagnostics. The `Html2x` facade maps those
public options into stage-owned settings and requests.

Page size has one public owner at `HtmlConverterOptions.Page.Size`. Resource
base directory and image size limit have one public owner under
`HtmlConverterOptions.Resources`. Font path has one public owner under
`HtmlConverterOptions.Fonts`.

When resource base directory is omitted, the facade resolves it to
`AppContext.BaseDirectory`. Runtime stages must not use the process current
directory as an implicit resource source.

Html2x must not pass public option objects into style, layout, pagination, or
PDF rendering. Internal stages consume `StyleBuildSettings`,
`LayoutBuildSettings`, `LayoutGeometryRequest`, `PaginationOptions`, and
`PdfRenderSettings`.

Public facade options must not define `ITextMeasurer`, `IFontSource`, or
`ResolvedFont`.

## Contracts Stage

Html2x.LayoutEngine.Contracts owns internal pipeline handoff contracts. It is
not a public consumer API surface. It carries the parser-free style input,
layout geometry request, image metadata contracts, shared source identity
records, final used geometry value, and published layout facts that later
internal stages consume.

Contract namespaces mirror the ownership folders:

- `Html2x.LayoutEngine.Contracts.Style` owns `StyleTree`, `ComputedStyle`,
  `HtmlCssConstants`, style source identity, content identity, and style
  content facts.
- `Html2x.LayoutEngine.Contracts.Geometry` owns `LayoutGeometryRequest`,
  `UsedGeometry`, `PageContentArea`, and geometry source identity facts.
- `Html2x.LayoutEngine.Contracts.Geometry.Images` owns
  `IImageMetadataResolver`, `ImageMetadataResult`, and
  `ImageLoadStatus` metadata outcomes.
- `Html2x.LayoutEngine.Contracts.Published` owns `PublishedLayoutTree` and
  the published block, inline, image, rule, table, display, and page facts.

Contract code must not use the deferred `Html2x.LayoutEngine.Models`,
`Html2x.LayoutEngine.Geometry.Published`, or
`Html2x.LayoutEngine.Geometry.Images` namespaces. Mutable box types are
geometry implementation state under `Html2x.LayoutEngine.Geometry.Models`,
not contract facts.

Contracts may reference `Html2x.RenderModel` for pure value, document, and
fragment facts. It must not reference AngleSharp, AngleSharp.Css,
facade option types, `Html2x.LayoutEngine.Style`,
`Html2x.LayoutEngine.Geometry`, `Html2x.LayoutEngine`, renderers, mutable box
types, parser DOM types, fragment implementation code, or diagnostics
serializers.

Image metadata contracts live under `Html2x.LayoutEngine.Contracts` because
they are geometry inputs, not published render facts or renderer byte-loading
adapters. `ImageLoadStatus` is the single image outcome vocabulary across
resources, metadata, published facts, fragments, and rendering diagnostics.
Public facade options must not define image provider or image metadata resolver
seams.

## Text Stage

Html2x.Text owns text measurement contracts and font resolution contracts. It is
the named module for text and font runtime seams used by geometry, rendering,
and composition.

The text module is explicitly Skia-backed in this transition. It owns
Skia-backed text measurement, font path resolution, font diagnostics, directory
font matching, typeface factory seams, and the file directory seam used by
font loading. The PDF renderer consumes resolved font facts from `TextRun` and
does not resolve fonts through `IFontSource`.

Low-level runtime seams such as `IFileDirectory` and `ISkiaTypefaceFactory` are
internal implementation details. Normal composition should use
`FontPathSource(string)` and `SkiaTextMeasurer(IFontSource)` instead of
constructing file directory or typeface factory adapters directly.

Project references are intentionally narrow: `Html2x.Text` may reference
`Html2x.RenderModel`, `Html2x.Diagnostics.Contracts`, `SkiaSharp`, and
`SkiaSharp.HarfBuzz` only. It must not reference facade options, layout engine
projects, fragment projection, or renderers.

Font identity primitives, `FontKey`, `FontWeight`, and `FontStyle`, are owned by
`Html2x.RenderModel`. They are the request language for `ITextMeasurer`,
`IFontSource`, `TextMeasurement`, `ResolvedFont`, geometry text construction,
and renderer typeface loading. Keeping them as pure facts lets text runtime
depend on stable data instead of making published render facts depend on Skia
text infrastructure.

## Style Stage

The style stage owns AngleSharp and CSS computation. AngleSharp and
AngleSharp.Css belong inside `Html2x.LayoutEngine.Style`.

The style stage receives raw HTML and internal `StyleBuildSettings` through
the internal style module seam, then returns a parser-free contract `StyleTree`.

The `StyleTree` handoff contract is:

- `StyleTree.Root` is the body-rooted style tree.
- `StyleNode.Element` is `StyledElementFacts`.
- `StyledElementFacts` carries tag, local name, id, class attribute, and
  case-insensitive attributes required by layout.
- `StyleNode.Identity` carries `StyleSourceIdentity`.
- `StyleContentNode.Identity` carries `StyleContentIdentity`.
- `StyleNode.Content` preserves ordered text, element, and line break content.
- `StyleNode.Children` preserves supported styled element children.
- Unsupported parser elements are flattened by the style module before geometry
  consumes content.

Style owns StyleNodeId, StyleContentId, StyleSourceIdentity, and StyleContentIdentity assignment.
`StyleTraversal` assigns source identity while it still has parser context.
Later stages must treat those identities as input facts.

Style owns CSS length interpretation while applying width and height
declarations to `ComputedStyle`. It also owns CSS color interpretation before
colors become render-model `ColorRgba` values. Stale dimension request and
resolution records should not be reintroduced unless a later stage consumes
them through an explicit handoff contract.

## Geometry Stage

Geometry consumes contract `StyleTree` only. It may use `ComputedStyle`,
`StyledElementFacts`, and `StyleContentNode` values from
`Html2x.LayoutEngine.Contracts`. It must not reference AngleSharp, `IElement`,
`INode`, DOM child nodes, or CSSOM types.

Geometry owns geometry validation helpers such as `GeometryGuard`. These are
implementation-local guards, not public option contracts.

Geometry owns BoxNode.SourceIdentity propagation and generated source identity.
`BoxTreeConstruction` copies style-owned source identity into boxes. Geometry
creates generated source identity for anonymous text, list markers,
inline-block content boxes, normalization wrappers, and other layout nodes that
do not directly correspond to a styled element.

Geometry publishes contract `PublishedLayoutTree` instead of exposing mutable
box internals to later stages. Mutable box types remain internal implementation
details for construction and focused algorithm tests.

Block kind dispatch and publication orchestration are owned by
`BlockBoxLayout`. Internal block-kind behavior is selected through
`BlockLayoutRuleSet`. Normal block-flow sequencing is owned by
`BlockFlowLayout`, and non-mutating stacked block measurement is owned
by `BlockFlowMeasurement` so layout and measurement share block-flow
policy. `BlockSizingRules` owns shared block sizing facts. Image block placement,
table grid calculation, table placement and diagnostics, published layout
caching, inline publishing, and shared mutable layout writes belong in focused
internal modules rather than accumulating in the orchestrator.

Geometry measures normal inline text through `Html2x.Text` and publishes the
resulting resolved font facts on each normal pipeline `TextRun`. Fragment
projection and rendering must treat those resolved facts as input.

Geometry owns image metadata consumption as a layout input. It consumes
`IImageMetadataResolver` through `LayoutGeometryRequest` and uses only status
and intrinsic size. `Html2x.Resources` owns data URI parsing, path scope, byte
limits, and intrinsic dimension decoding. Rendering uses that same module for
image bytes and does not duplicate resource policy.

Published identity keeps two concepts separate:

- `NodePath` is layout identity owned by geometry.
- `SourceIdentity` is source identity copied or generated from style input.

## Fragment Stage

Html2x.LayoutEngine.Fragments owns published layout traversal, fragment ID
allocation, style-to-`VisualStyle` conversion, and specialized image, rule, and
table fragment projection. Its implementation types are internal composition
surface, not consumer API surface.

The fragment stage consumes `PublishedLayoutTree` from Contracts. It copies
geometry and published text run facts forward into render model fragments and
must not reference mutable boxes, style implementation code,
geometry implementation engines, parser state, text or font adapter seams,
pagination pages, or renderer state.

Renderers do not reference fragment projection. They consume `HtmlLayout` and
renderer-facing fragments after composition and pagination have finished.
`HtmlLayout.Pages` is a read-only page list at the rendering seam.

## Pagination Stage

Html2x.LayoutEngine.Pagination owns the page placement module. Its internal
entry point is `LayoutPaginator`, not `BlockPaginator`. `LayoutPaginator`
consumes already measured render model block fragments and `PaginationOptions`,
then returns `PaginationResult` with the final `HtmlLayout` plus stable audit
facts.

Pagination may clone and translate render model fragment subtrees to produce
page-local coordinates. It may assemble `LayoutPage` values and emit
pagination diagnostics. It must not mutate source fragments, read mutable boxes,
interpret style or parser state, call fragment projection, or use renderer
state.

The current implementation uses an internal `BlockPaginator` algorithm. It
splits only at block boundaries. `SplitAcrossPages` and `ForcedBreak` are audit
vocabulary reserved for future behavior, not current behavior.

## Diagnostics Ownership

Diagnostics may expose source identity only through generic diagnostic fields.
Diagnostic records use nullable primitive fields such as `SourceNodeId`,
`SourceContentId`, `SourcePath`, `SourceOrder`, `SourceElementIdentity`, and
`GeneratedSourceKind`. They must not expose `StyleSourceIdentity`,
`StyleContentIdentity`, or `GeometrySourceIdentity`.

## Composition Stage

Composition owns orchestration and must not access parser or box internals.
`LayoutBuilder` calls style first, passes the resulting `StyleTree` to
geometry, calls the fragment module to project from contract published layout,
calls the pagination module, and returns the final layout from
`PaginationResult.Layout`. Composition orchestrates Style, Geometry, Fragment,
and Pagination modules but reads handoff facts through Contracts or
renderer-facing render facts. If composition needs more data, the owning stage
must publish that data through its handoff contract.

## Mutation Policy

Earlier stage outputs become read-only inputs after handoff. Later stages may
read them, but must not repair or reinterpret them.

The style stage is the last stage allowed to interpret parser state. Initial box
construction is the last stage allowed to change the box hierarchy. Layout
geometry is the last stage allowed to write normal-flow box geometry. Fragment
projection copies geometry forward. Pagination owns page placement and uses
cloned translated render model fragments when content moves between pages.
Paint owns drawing only.

## Current Compatibility Surfaces

Some older mutable surfaces still exist and should be treated as compatibility
state:

- `BlockBox.Margin`, `Padding`, `TextAlign`, `MarkerOffset`, and
  `InlineLayout` are mutable layout-stage facts.
- `BlockBox.UsedGeometry` is written by layout and read while publishing.
- Table row and cell scalar metadata is written for fragment projection.
- `InlineBox.Width`, `Height`, `BaselineOffset`, and `Fragment` are inline
  layout facts.

New code should consume the owned stage output rather than treating these
mutable fields as independent state.

Display roles are not a separate tree. They are carried as `BoxRole` during box
construction and copied into published layout or fragment metadata when later
stages need them.

## Extension Rule

If a later stage needs data that only exists in an earlier stage, add that data
to the stage output consumed by the next stage. Do not add backward references.
