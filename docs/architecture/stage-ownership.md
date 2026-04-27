# Stage Ownership

This document defines internal ownership rules for the HTML to PDF pipeline. It is for developers changing the codebase, not a public API contract.

## Ownership Matrix

| Stage | Input | Owned Output | May Write | Must Not Write |
| --- | --- | --- | --- | --- |
| DOM | HTML and layout options | AngleSharp document state | Parser-owned DOM state | Style, boxes, fragments, pages, PDF output |
| Style | DOM and CSSOM | Style tree and computed styles | Style nodes and computed CSS values | Box hierarchy, geometry, fragments, renderer state |
| Initial Box | Style tree | Box node hierarchy and display roles | Box nodes, text runs, list markers, unsupported mode diagnostics | Used geometry, fragments, pages |
| Layout Geometry | Initial box tree and layout options | Box tree and used geometry | Box geometry, margins, padding, inline layout results, image layout facts, table placements | Style values, fragments, pagination placements |
| Fragment | Laid-out box tree and font source | Fragment tree | New fragments copied from layout facts | Boxes, CSS, pagination pages |
| Pagination | Fragment tree | `HtmlLayout.Pages` | Page models and translated fragment clones | Source fragments, boxes, styles |
| Paint | `HtmlLayout` | Paint commands and PDF bytes | Renderer-local commands and Skia objects | Layout pages, fragments, boxes |

## Mutation Policy

Earlier stage outputs become read-only inputs after handoff. Later stages may read them, but must not repair or reinterpret them.

Initial box construction is the last stage allowed to change the box hierarchy. Layout geometry is the last stage allowed to write normal-flow box geometry. Fragment projection copies geometry forward. Pagination owns page placement and uses cloned translated fragments when content moves between pages. Paint owns drawing only.

## Current Compatibility Surfaces

Some older mutable surfaces still exist and should be treated as compatibility mirrors:

- `BlockBox.X`, `Y`, `Width`, `Height`, and `MarkerOffset` mirror `UsedGeometry`.
- Table row and cell scalar fields mirror applied table geometry.
- `BlockBox.InlineLayout` is layout-stage output. Measurement paths must preserve prior state.

New code should consume the owned stage output rather than treating these mirrors as independent state.

Display roles are not a separate tree. They are carried as `BoxRole` during box construction and copied into fragment metadata when renderers need them.

## Extension Rule

If a later stage needs data that only exists in an earlier stage, add that data to the stage output consumed by the next stage. Do not add backward references.
