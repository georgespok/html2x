# Architecture Overview

Html2x converts static HTML and CSS into PDF through explicit pipeline stages. The core design goal is stable stage ownership: each stage produces a named output that later stages consume without reaching backward into implementation details.

## Project Map

| Project | Responsibility | Must Not Do |
| --- | --- | --- |
| `Html2x` | Public converter facade, option wiring, shared service construction. | Contain layout or rendering algorithms. |
| `Html2x.Abstractions` | Contracts for options, layout documents, fragments, measurements, fonts, and diagnostics payloads. | Reference AngleSharp, SkiaSharp, file IO, or concrete renderers. |
| `Html2x.Diagnostics` | Diagnostics JSON serialization. | Own layout or renderer decisions. |
| `Html2x.LayoutEngine.Style` | HTML parsing, user agent stylesheet application, CSS parsing, computed style construction, style diagnostics, and parser-free `StyleTree` output. | Own layout geometry, fragments, pagination, or renderer state. |
| `Html2x.LayoutEngine.Geometry` | Geometry construction from `StyleTree` into published layout facts. | Parse HTML or CSS, reference parser objects, or expose mutable boxes as the public handoff. |
| `Html2x.LayoutEngine` | Pipeline composition, fragment projection, pagination, and `HtmlLayout` assembly. | Reference SkiaSharp, own parser implementation details, or mutate renderer state. |
| `Html2x.Renderers.Pdf` | PDF rendering with SkiaSharp from `HtmlLayout`. | Reach back to DOM, CSS, style tree, or box tree types. |

## Primary Data Flow

```text
HTML/CSS
  -> Style tree
  -> Published layout tree
  -> Fragment tree
  -> Pagination
  -> HtmlLayout
  -> PDF renderer
```

There is no separate display tree layer. Display roles are materialized as
`BoxRole` values inside geometry and then copied into published layout or
fragment metadata where later stages need them.

`StyleTree` is the parser-free handoff from style to geometry. The fragment and
page model is the renderer-facing contract. If the renderer lacks required data,
fix the layout or fragment stage instead of adding renderer lookups into earlier
stages.

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
