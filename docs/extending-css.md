# Extending CSS Support

This guide walks you through adding support for a new CSS property or selector so that future contributions follow the same, predictable flow.

> Summary: extend style computation -> flow it into the box model -> expose on fragments -> render it -> test it.

## 1. Understand the Property

Before writing code, clarify:

- **Category**: typography, box model, positioning, colors, etc.
- **Value space**: strings, lengths, enumerations, color codes.
- **Inheritance**: does the property inherit by default?
- **Initial value**: what should be applied when the author omits the property?
- **Affected stages**: does the property influence layout (box tree), rendering-only (fragment -> renderer), or both?

Document these decisions in the PR description so reviewers know the intended behavior.

## 2. Update Style Computation

1. Add constants/parsing helpers under `Html2x.LayoutEngine.Style`.
2. Extend `CssStyleComputer` (and related traversal helpers) to read values from the computed style map.
3. Update `UserAgentDefaults` with the initial value.
4. Ensure the resolved value is carried in an immutable record within `ComputedStyle`.

### Tests

- Add unit tests in `Html2x.LayoutEngine.Test` that feed sample HTML with inline styles and verify the computed style output.
- Use `[Theory]` to cover valid and invalid values.

## 3. Propagate Through the Box Model

If the property influences layout (e.g., margins, line height):

1. Extend the relevant box type in `Html2x.Abstractions` with new fields.
2. Modify `BoxTreeBuilder` to map the computed style into the box.
3. Re-run TDD: add/adjust tests that validate the box structure.

If the property is render-only (e.g., text decoration), you may skip box changes and carry the value directly on fragments.

## 4. Surface on Fragments

1. Update fragment models (`TextRun`, `BlockFragment`, etc.) with the necessary data.
2. Ensure fragments remain immutable. Prefer constructor parameters to mutable setters.
3. Bump visitor interfaces if new fragment types are introduced.

### Tests

- Extend `FragmentBuilder` tests to ensure the new property flows from boxes to fragments.
- Use explicit assertions rather than snapshots when possible.

## 5. Render the Property

1. Update `QuestPdfFragmentRenderer` (or relevant renderer) to translate the fragment data.
2. Add logging traces if the property impacts rendering decisions (e.g., something is skipped).
3. Where QuestPDF lacks native support, log a `Warning` and document the limitation.

### Tests

- Add/extend tests under `Html2x.Renderers.Pdf.Test`:
  - Render a minimal layout exercising the property.
  - Use `PdfWordParser` or other helpers to verify output.
  - Assert warnings for unsupported values when appropriate.

## 6. End-to-End Validation

- Add an integration test (or update an existing one) in `Html2x.Test` that uses `HtmlConverter` with the new property.
- Capture logs via `TestOutputLoggerProvider` if diagnostics help validate the behavior.
- Use `SavePdfForInspectionAsync` while developing, but avoid committing generated PDFs.

## 7. Documentation & Change Log

- Update `docs/architecture.md` or relevant guides if the property introduces new patterns.
- Mention the new support in release notes / change log (if maintained separately).
- When the property is partially supported, document the limitations and fallback behavior.

## Checklist

- [ ] Style computation updated with defaults, inheritance, parsing.
- [ ] Box or fragment models extended as needed.
- [ ] Renderer covers rendering and diagnostics.
- [ ] Unit + integration tests across layout and renderer.
- [ ] Documentation updates committed.

Following this path keeps the pipeline predictable and makes it clear where future contributors should plug into the system.
