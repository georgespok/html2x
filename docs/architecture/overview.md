# Architecture Overview

Html2x converts static HTML and CSS into PDF through explicit pipeline stages. The core design goal is stable stage ownership: each stage produces a named output that later stages consume without reaching backward into implementation details.

## Project Map

| Project | Responsibility | Must Not Do |
| --- | --- | --- |
| `Html2x` | Public converter facade, facade-owned options, public-to-stage settings mapping, and shared service construction. | Contain layout or rendering algorithms, or pass public option objects into internal stages. |
| `Html2x.RenderModel` | Pure render facts such as units, style values, font request facts, resolved font facts, documents, and fragments. | Reference runtime adapters, diagnostics runtime, parser packages, layout engines, fragment projection, renderers, or SkiaSharp. |
| `Html2x.Text` | Text measurement contracts, font resolution contracts, and Skia-backed text/font implementation. | Reference facade options, layout engine projects, fragment projection, or renderers. |
| `Html2x.Diagnostics.Contracts` | Generic diagnostics emission contracts and constrained diagnostic field values. | Reference layout, renderer, parser, or diagnostics runtime projects. |
| `Html2x.Diagnostics` | Diagnostics collection, report model, and JSON serialization. | Own layout or renderer decisions. |
| `Html2x.LayoutEngine.Style` | HTML parsing, user agent stylesheet application, CSS parsing, computed style construction, style diagnostics, style-owned settings, and parser-free `StyleTree` output. | Own layout geometry, fragments, pagination, or renderer state. |
| `Html2x.LayoutEngine.Geometry` | Geometry construction from `StyleTree` into published layout facts. | Parse HTML or CSS, reference parser objects, or expose mutable boxes as the public handoff. |
| `Html2x.LayoutEngine.Fragments` | Projection from published layout facts into renderer-facing fragment trees. | Consume mutable boxes, parser objects, pagination pages, or renderer state. |
| `Html2x.LayoutEngine.Pagination` | Page placement for render model fragments. Returns `PaginationResult` with final `HtmlLayout` and pagination audit facts. | Reference style, geometry implementation engines, fragment projection, parser packages, renderers, or SkiaSharp. |
| `Html2x.LayoutEngine` | Pipeline composition from style, geometry, fragment projection, and pagination into the converter-facing layout result. Owns layout build settings and maps them into stage requests. | Own parser, geometry, fragment projection, pagination, text measurement, or rendering algorithms. |
| `Html2x.Renderers.Pdf` | PDF rendering with SkiaSharp from `HtmlLayout`, resolved font facts, and renderer-owned PDF render settings. | Reach back to DOM, CSS, style tree, box tree types, public converter options, or font source adapters. |

## Primary Data Flow

```text
HTML/CSS
  -> Html2x.LayoutEngine.Style -> Style tree
  -> Html2x.LayoutEngine.Geometry -> Published layout tree
  -> Html2x.LayoutEngine.Fragments -> Fragment tree
  -> Html2x.LayoutEngine.Pagination -> PaginationResult
  -> HtmlLayout
  -> PDF renderer
```

There is no separate display tree layer. Display roles are materialized as
`BoxRole` values inside geometry and then copied into published layout or
fragment metadata where later stages need them.

`StyleTree` is the parser-free handoff from style to geometry. Render model
fragments and layout documents are the renderer-facing facts. If the renderer
lacks required data, fix the layout or fragment stage instead of adding
renderer lookups into earlier stages.

## Design Principles

- Keep stages explicit and testable.
- Preserve deterministic output for the same input, options, fonts, and image sources.
- Prefer immutable records and value types for published stage output.
- Use diagnostics to explain unsupported input, fallback behavior, and rendering decisions.
- Keep cross-layer dependencies one-directional.

## Developer Entry Points

- Pipeline details: [Processing Pipeline](pipeline.md)
- Stage boundaries: [Stage Ownership](stage-ownership.md)
- Geometry rules: [Geometry](geometry.md)
- Diagnostics flow: [Diagnostics Architecture](diagnostics.md)
