# Supported HTML And CSS

Html2x supports a practical static subset of HTML and CSS for deterministic business-report PDF generation. It is not a browser engine.

## Supported Scope

- Static HTML.
- Inline and stylesheet CSS parsed by AngleSharp.
- Block and inline flow.
- Text wrapping, line height, text alignment, font state, text color, and basic text decoration.
- Margins, padding, borders, width, height, min and max dimensions.
- Background colors.
- Images with intrinsic dimensions, HTML width and height attributes, CSS dimensions, padding, and borders.
- Lists with generated marker text and marker geometry.
- Horizontal rules.
- Basic rectangular tables without spans.
- Block-level pagination.
- Diagnostics snapshots for layout, geometry, tables, pagination, images, fonts, and renderer summaries.

## Table Support

Supported:

- Rectangular table structures.
- Table, row, and cell fragments.
- Equal-width column derivation when explicit column widths are absent.
- Header cell identity.
- Row and cell backgrounds.
- Borders and padding.

Unsupported:

- `colspan`.
- `rowspan`.
- Complex browser table layout behavior.

## Unsupported Or Partial CSS

Current documented fallbacks:

- Floats are parsed as unsupported layout modes and omitted from rendered normal flow while preserving following normal-flow content.
- `position: absolute` is not implemented as positioned layout and remains in normal flow.
- `display: flex` does not create a flex formatting context and falls back to block-like flow.
- Grid, transforms, filters, advanced media queries, and scripting are out of scope.

Unsupported or ignored declarations should emit diagnostics when diagnostics are enabled.

## Determinism Notes

Determinism depends on using the same HTML, options, fonts, image files, operating system font behavior, and dependency versions. Use repository test fonts for stable test scenarios.
