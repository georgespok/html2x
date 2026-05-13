# Glossary

This page defines canonical Html2x terminology and capitalization.

| Term | Meaning |
| --- | --- |
| HTML | Static input markup parsed by the style stage. |
| CSS | Static styles parsed and reduced to supported computed values. |
| PDF | Output document rendered by `Html2x.Renderers.Pdf`. |
| Facade | The public `Html2x` entry point that maps public options to internal settings. |
| Stage | A pipeline owner that consumes named input facts and produces named output facts. |
| Fact | Immutable or treated-as-immutable data consumed across a boundary. |
| Contract | A stable handoff shape between internal projects or public consumers. |
| Style tree | Parser-free style output named `StyleTree`. |
| Geometry | Layout placement and sizing owned by `Html2x.LayoutEngine.Geometry`. |
| Used geometry | Canonical block-level geometry named `UsedGeometry`. |
| Published layout | Immutable geometry output named `PublishedLayoutTree`. |
| Fragment tree building | Conversion from `PublishedLayoutTree` to renderer-facing `FragmentTree`. |
| Fragment tree | Pre-pagination render model fragments named `FragmentTree`. |
| Pagination | Page placement stage that returns `PaginationResult` and `HtmlLayout`. |
| Render model | Pure renderer-facing documents, fragments, geometry values, style values, image facts, and font facts. |
| Source identity | Identity copied or generated from styled input so diagnostics can point back to source-like context. |
| Layout identity | Geometry-owned node path or fragment path that identifies a layout output. |
| Diagnostics | Structured records that explain lifecycle, fallback, unsupported input, layout, pagination, image, font, and rendering decisions. |
| Resource scope | Base directory and byte-limit policy used for image metadata and image byte loading. |
| Resolved font | `ResolvedFont` fact produced during measurement and consumed during rendering. |

Use `Html2x`, `HTML`, `CSS`, `PDF`, `StyleTree`, `PublishedLayoutTree`,
`FragmentTree`, `PaginationResult`, `HtmlLayout`, `UsedGeometry`,
`ImageLoadStatus`, and `ResolvedFont` exactly when referring to those project
concepts or types.
