# Stage Ownership

This document defines internal ownership rules for the HTML to PDF pipeline. It
is for developers changing the codebase, not a public API contract.

## Ownership Matrix

| Stage | Project | Input | Owned Output | May Write | Must Not Write |
| --- | --- | --- | --- | --- | --- |
| Contracts | `Html2x.LayoutEngine.Contracts` | None | Internal pipeline handoff contracts | Immutable contract facts and validation helpers | Parser traversal, CSS computation, mutable boxes, layout algorithms, fragments, pagination pages, renderer state |
| Style | `Html2x.LayoutEngine.Style` | Raw HTML, layout options, optional diagnostics session | Contract `StyleTree` with `StyledElementFacts`, ordered `StyleContentNode` entries, and computed styles | AngleSharp document loading, user agent stylesheet application, CSS parsing, computed style construction, style diagnostics | Box hierarchy, layout geometry, fragments, pagination pages, renderer state |
| Layout Geometry | `Html2x.LayoutEngine.Geometry` | Contract `StyleTree` and layout geometry request | Contract `PublishedLayoutTree` and internal box geometry | Box roles, text runs, list markers, unsupported mode diagnostics, image layout facts, table placements, `UsedGeometry` | CSS parsing, DOM traversal, parser objects, fragments, pagination pages, renderer state |
| Fragment | `Html2x.LayoutEngine` | Contract `PublishedLayoutTree` and font source | Fragment tree | New fragments copied from published layout facts | Boxes, CSS, DOM, pagination pages |
| Pagination | `Html2x.LayoutEngine` | Fragment tree | `HtmlLayout.Pages` | Page models and translated fragment clones | Source fragments, boxes, styles |
| Paint | `Html2x.Renderers.Pdf` | `HtmlLayout` and PDF options | Paint commands and PDF bytes | Renderer-local commands and Skia objects | Layout pages, fragments, boxes, styles, parser objects |

## Contracts Stage

Html2x.LayoutEngine.Contracts owns internal pipeline handoff contracts. It is
not a public consumer API surface. It carries the parser-free style input,
layout geometry request, shared source identity records, final used geometry
value, and published layout facts that later internal stages consume.

Contracts may reference `Html2x.Abstractions` for existing value types such as
spacing, colors, text runs, image providers, and page sizes. It must not
reference AngleSharp, AngleSharp.Css, `Html2x.LayoutEngine.Style`,
`Html2x.LayoutEngine.Geometry`, `Html2x.LayoutEngine`, renderers, mutable box
types, parser DOM types, fragment implementation code, or diagnostics
serializers.

## Style Stage

The style stage owns AngleSharp and CSS computation. AngleSharp and
AngleSharp.Css belong inside `Html2x.LayoutEngine.Style`.

The style stage receives raw HTML and layout options through `IStyleTreeBuilder`
or `StyleTreeBuilder`, then returns a parser-free contract `StyleTree`.

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

## Geometry Stage

Geometry consumes contract `StyleTree` only. It may use `ComputedStyle`,
`StyledElementFacts`, and `StyleContentNode` values from
`Html2x.LayoutEngine.Contracts`. It must not reference AngleSharp, `IElement`,
`INode`, DOM child nodes, or CSSOM types.

Geometry owns BoxNode.SourceIdentity propagation and generated source identity.
`InitialBoxTreeBuilder` copies style-owned source identity into boxes. Geometry
creates generated source identity for anonymous text, list markers,
inline-block content boxes, normalization wrappers, and other layout nodes that
do not directly correspond to a styled element.

Geometry publishes contract `PublishedLayoutTree` instead of exposing mutable
box internals to later stages. Mutable box types remain internal implementation
details and compatibility surfaces for focused tests.

Published identity keeps two concepts separate:

- `NodePath` is layout identity owned by geometry.
- `SourceIdentity` is source identity copied or generated from style input.

## Diagnostics Ownership

Diagnostics may expose source identity only through the Phase 5 diagnostic contract.
Diagnostic payloads use nullable primitive fields such as `SourceNodeId`,
`SourceContentId`, `SourcePath`, `SourceOrder`, `SourceElementIdentity`, and
`GeneratedSourceKind`. They must not expose `StyleSourceIdentity`,
`StyleContentIdentity`, or `GeometrySourceIdentity`.

## Composition Stage

Composition owns orchestration and must not access parser or box internals.
`LayoutBuilder` calls style first, passes the resulting `StyleTree` to
geometry, projects fragments from contract published layout, paginates, and
returns the final layout. Composition orchestrates Style and Geometry
implementations but reads handoff facts through Contracts. If composition needs
more data, the owning stage must publish that data through its handoff
contract.

## Mutation Policy

Earlier stage outputs become read-only inputs after handoff. Later stages may
read them, but must not repair or reinterpret them.

The style stage is the last stage allowed to interpret parser state. Initial box
construction is the last stage allowed to change the box hierarchy. Layout
geometry is the last stage allowed to write normal-flow box geometry. Fragment
projection copies geometry forward. Pagination owns page placement and uses
cloned translated fragments when content moves between pages. Paint owns drawing
only.

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
