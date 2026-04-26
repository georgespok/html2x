# Coding Standards

These standards keep Html2x maintainable as layout and rendering support expands.

## Layering

Cross-layer dependencies flow forward:

```text
Html2x.Abstractions
  -> Html2x.LayoutEngine
  -> Html2x.Renderers.Pdf
  -> Html2x
```

The facade wires services together. Layout owns HTML/CSS interpretation. Renderers consume fragments and pages. Diagnostics contracts live in abstractions, while JSON serialization lives in `Html2x.Diagnostics`.

## Implementation Conventions

- Keep classes single-purpose and name the responsibility clearly.
- Prefer immutable records or readonly structs for value contracts.
- Use option objects and injected services instead of static mutable state.
- Use `var` when the right-hand type is obvious.
- Keep public APIs documented with XML comments.
- Keep methods short enough that validation, calculation, and projection steps are visible.
- Use guard clauses for required dependencies and invalid options.

## Logging And Diagnostics

- Use existing log helper classes such as `LayoutLog` and `PdfRendererLog`.
- Use `LoggerMessage.Define` for reusable events.
- Guard high-volume trace or debug events with `logger.IsEnabled`.
- Prefer structured diagnostics payloads when a behavior needs testable evidence.

## Error Handling

- Validate early and throw clear exceptions.
- Log or emit diagnostics before rethrowing stage failures when a diagnostics session exists.
- Do not silently swallow invalid input that changes output.
- Unsupported but recoverable features should produce deterministic fallback behavior and diagnostics.

## Formatting

- Follow `.editorconfig`.
- Use 4-space indentation for C# and HTML.
- Use 2-space indentation for XML-like files.
- Do not mix tabs and spaces.
- Use ASCII punctuation in repository files.
- Avoid `ref` parameters. Return a value object or introduce a small context type when multiple values move together.

## Review Checklist

Before finishing a change, verify:

1. Module boundaries remain intact.
2. New behavior has focused tests.
3. Unsupported behavior has diagnostics or an explicit documented fallback.
4. Geometry is produced by layout and projected forward.
5. Renderer changes consume fragments and do not reach backward into layout internals.
6. Documentation was updated when a contract, extension point, or workflow changed.
