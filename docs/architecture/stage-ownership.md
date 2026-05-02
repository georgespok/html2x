# Stage Ownership

This document defines internal ownership rules for the HTML to PDF pipeline. It
is for developers changing the codebase, not a public API contract.

## Ownership Matrix

| Stage | Project | Input | Owned Output | May Write | Must Not Write |
| --- | --- | --- | --- | --- | --- |
| Public Facade | `Html2x` | Consumer configuration consumed by the converter facade | Option contracts and mapping to stage-owned settings | `HtmlConverterOptions`, page options, resource options, CSS options, font options, diagnostics options, and facade mapping | Layout algorithms, renderer algorithms, runtime adapters, documents, fragments, layout contracts, geometry guards, diagnostics runtime, internal stage request models |
| Render Model | `Html2x.RenderModel` | None | Pure render facts | Units, style value facts, font request facts, resolved font facts, documents, and fragments | Runtime adapters, parser traversal, CSS computation, mutable boxes, layout algorithms, pagination algorithms, renderer state |
| Contracts | `Html2x.LayoutEngine.Contracts` | None | Internal pipeline handoff contracts | Immutable contract facts and validation helpers | Parser traversal, CSS computation, mutable boxes, layout algorithms, fragments, pagination pages, renderer state |
| Text | `Html2x.Text` | Font requests, text measurement requests, file directory access, and diagnostics sink | Text measurement contracts and font resolution contracts | Text measurement seams, Skia-backed text measurement, font path resolution, font diagnostics, directory font matching, and typeface factory seams | Parser traversal, CSS computation, mutable boxes, layout engine implementation projects, fragment projection, pagination pages, renderer state |
| Style | `Html2x.LayoutEngine.Style` | Raw HTML, `StyleBuildSettings`, optional diagnostics sink | Contract `StyleTree` with `StyledElementFacts`, ordered `StyleContentNode` entries, and computed styles | AngleSharp document loading, user agent stylesheet application, CSS parsing, computed style construction, CSS dimension request and resolution facts, style diagnostics | Box hierarchy, layout geometry, fragments, pagination pages, renderer state |
| Layout Geometry | `Html2x.LayoutEngine.Geometry` | Contract `StyleTree`, layout geometry request, and image metadata resolver | Contract `PublishedLayoutTree` and internal box geometry | Box roles, text runs with resolved font facts, list markers, unsupported mode diagnostics, image metadata resolution, image layout facts, table placements, `UsedGeometry` | CSS parsing, DOM traversal, parser objects, fragments, pagination pages, renderer state |
| Fragment | `Html2x.LayoutEngine.Fragments` | Contract `PublishedLayoutTree` | Fragment tree | Published layout traversal, fragment ID allocation, `VisualStyle` projection, and copying published text run facts | Mutable boxes, CSS, DOM, text or font adapter seams, pagination pages, renderer state |
| Pagination | `Html2x.LayoutEngine.Pagination` | Render model block fragments and `PaginationOptions` | `PaginationResult` with final `HtmlLayout` and audit facts | Translated fragment clones, `LayoutPage` assembly, page audit facts, placement audit facts, and pagination diagnostics | Source fragments, mutable boxes, style facts, geometry implementation engines, fragment projection, parser state, renderer state |
| Paint | `Html2x.Renderers.Pdf` | `HtmlLayout` and `PdfRenderSettings` | Paint commands and PDF bytes | Renderer-local settings, commands, and Skia objects | Layout pages, fragments, boxes, styles, parser objects, public converter options |

## Render Model Stage

Html2x.RenderModel owns pure render facts such as `SizePx`, `SizePt`,
`PaperSizes`, `ColorRgba`, `Spacing`, borders, `VisualStyle`, `FontKey`,
`FontWeight`, `FontStyle`, `ResolvedFont`, `HtmlLayout`, `LayoutPage`,
`LayoutMetadata`, and renderer-facing fragments. These facts can be shared by
layout, text, fragment projection, pagination, renderers, options, and tests
without taking a dependency on runtime behavior.

The render model must not reference SkiaSharp, filesystem seams, diagnostics
runtime, parser packages, layout implementation projects, fragment projection,
or renderers. It may contain small value helpers on the facts themselves, but
not behavior-changing adapters.

## Public Facade

Html2x owns the active public converter options. `HtmlConverterOptions` is the
single public conversion request, and its groups reflect consumer concepts:
page, resources, CSS, fonts, and diagnostics. The `Html2x` facade maps those
public options into stage-owned settings and requests.

Page size has one public owner at `HtmlConverterOptions.Page.Size`. Resource
base directory and image size limit have one public owner under
`HtmlConverterOptions.Resources`. Font path has one public owner under
`HtmlConverterOptions.Fonts`.

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

Contracts may reference `Html2x.RenderModel` for pure value, document, and
fragment facts. It must not reference AngleSharp, AngleSharp.Css,
facade option types, `Html2x.LayoutEngine.Style`,
`Html2x.LayoutEngine.Geometry`, `Html2x.LayoutEngine`, renderers, mutable box
types, parser DOM types, fragment implementation code, or diagnostics
serializers.

Image metadata contracts live under `Html2x.LayoutEngine.Contracts` because
they are geometry inputs, not published render facts or renderer byte-loading
adapters. Public facade options must not define image provider or image
metadata resolver seams.

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

The style stage receives raw HTML and `StyleBuildSettings` through
`IStyleTreeBuilder` or `StyleTreeBuilder`, then returns a parser-free contract
`StyleTree`.

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

Style owns CSS dimension request and resolution facts used while interpreting
width and height declarations. Those facts are implementation detail unless a
later stage consumes them through an explicit handoff contract.

## Geometry Stage

Geometry consumes contract `StyleTree` only. It may use `ComputedStyle`,
`StyledElementFacts`, and `StyleContentNode` values from
`Html2x.LayoutEngine.Contracts`. It must not reference AngleSharp, `IElement`,
`INode`, DOM child nodes, or CSSOM types.

Geometry owns geometry validation helpers such as `GeometryGuard`. These are
implementation-local guards, not public option contracts.

Geometry owns BoxNode.SourceIdentity propagation and generated source identity.
`InitialBoxTreeBuilder` copies style-owned source identity into boxes. Geometry
creates generated source identity for anonymous text, list markers,
inline-block content boxes, normalization wrappers, and other layout nodes that
do not directly correspond to a styled element.

Geometry publishes contract `PublishedLayoutTree` instead of exposing mutable
box internals to later stages. Mutable box types remain internal implementation
details and compatibility surfaces for focused tests.

Geometry measures normal inline text through `Html2x.Text` and publishes the
resulting resolved font facts on each normal pipeline `TextRun`. Fragment
projection and rendering must treat those resolved facts as input.

Geometry owns image metadata resolution as a layout input. It consumes
`IImageMetadataResolver` through `LayoutGeometryRequest` and uses only status
and intrinsic size. Rendering separately loads image bytes through renderer
infrastructure.

Published identity keeps two concepts separate:

- `NodePath` is layout identity owned by geometry.
- `SourceIdentity` is source identity copied or generated from style input.

## Fragment Stage

Html2x.LayoutEngine.Fragments owns published layout traversal, fragment ID
allocation, style-to-`VisualStyle` conversion, and specialized image, rule, and
table fragment projection.

The fragment stage consumes `PublishedLayoutTree` from Contracts. It copies
geometry and published text run facts forward into render model fragments and
must not reference mutable boxes, style implementation code,
geometry implementation engines, parser state, text or font adapter seams,
pagination pages, or renderer state.

Renderers do not reference fragment projection. They consume `HtmlLayout` and
renderer-facing fragments after composition and pagination have finished.

## Pagination Stage

Html2x.LayoutEngine.Pagination owns the page placement module. Its public entry
point is `LayoutPaginator`, not `BlockPaginator`. `LayoutPaginator` consumes
already measured render model block fragments and `PaginationOptions`, then
returns `PaginationResult` with the final `HtmlLayout` plus stable audit facts.

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
mirrors:

- `BlockBox.X`, `Y`, `Width`, `Height`, and `MarkerOffset` mirror
  `UsedGeometry`.
- Table row and cell scalar fields mirror applied table geometry.
- `BlockBox.InlineLayout` is layout-stage output. Measurement paths must
  preserve prior state.

New code should consume the owned stage output rather than treating these
mirrors as independent state.

Display roles are not a separate tree. They are carried as `BoxRole` during box
construction and copied into published layout or fragment metadata when later
stages need them.

## Extension Rule

If a later stage needs data that only exists in an earlier stage, add that data
to the stage output consumed by the next stage. Do not add backward references.
