# Extending CSS Support

Use this path when adding a CSS property, selector behavior, or supported value.

## 1. Define The Contract

Before code changes, decide:

- Category: typography, box model, positioning, color, table, image, or pagination.
- Value space: keyword, length, percentage, color, string, or enum.
- Initial value.
- Inheritance behavior.
- Affected stages: style only, layout, fragments, renderer, diagnostics.
- Unsupported values and fallback behavior.

Record these decisions in the change summary and update [Supported HTML And CSS](../reference/supported-html-css.md).

## 2. Update Style Computation

- Extend parser or conversion helpers under layout style code.
- Add defaults through user agent or initial value handling.
- Carry the computed value on the appropriate style record.
- Emit style diagnostics for invalid, ignored, or partially applied declarations.

## 3. Propagate To Layout

If the property changes layout, update display, box, or formatting context code. Geometry-producing behavior belongs in layout and should use existing geometry helpers.

If the property is render-only, carry the fact to fragments without inventing layout geometry.

## 4. Update Fragments And Rendering

Add fragment fields only when renderers need the value. Update PDF drawing code to consume the fragment data. If rendering is not supported, emit a warning and document the limitation.

## 5. Test

Add focused coverage in this order:

1. Style computation tests.
2. Layout or fragment tests.
3. Renderer tests when drawing changes.
4. Integration tests when public converter behavior changes.
5. Diagnostics serializer tests when payloads change.
