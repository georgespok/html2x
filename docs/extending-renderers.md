# Extending Renderers

Html2x is renderer-agnostic: the fragment tree is the contract. This guide explains how to add a new renderer (e.g., SVG, Canvas, alternate PDF engine) or extend the existing QuestPDF implementation.

## Architectural Overview

```
HtmlLayout (pages -> fragments)
        |
IFragmentRendererFactory -> IFragmentRenderer -> FragmentRenderDispatcher -> RenderTarget
```

- **Factory**: Creates `IFragmentRenderer` instances with renderer-specific dependencies.
- **Renderer**: Implements the visitor hooks for blocks, lines, images, rules, etc.
- **Dispatcher**: Traverses fragments, invoking the correct renderer methods.

## Adding a New Renderer Project

1. **Create a new project** (e.g., `Html2x.Svg`) referencing `Html2x.Abstractions`.
2. **Implement `IFragmentRenderer`**:
   - Honor the fragment contract (do not mutate fragments).
   - Handle block, line, image, and rule fragments. If a fragment is unsupported, log a warning.
3. **Implement `IFragmentRendererFactory`**:
   - Accept renderer-specific configuration via constructor parameters or options classes.
   - Ensure factory methods are idempotent; do not cache stateful renderer instances unless thread-safe.
4. **Provide a facade** similar to `PdfRenderer` if the renderer needs orchestration beyond fragment traversal.

## Extending QuestPDF Renderer

When enhancing the default PDF renderer:

- Update `QuestPdfFragmentRenderer` cautiously. Group changes behind clearly named helper methods (e.g., `RenderTextDecoration`).
- Use `PdfRendererLog` for new diagnostics rather than raw `logger.Log...` calls.
- Keep `PdfRenderer` oblivious to QuestPDF specifics-only the factory and fragment renderer should reference QuestPDF APIs.

## Logging Expectations

- Accept `ILogger<T>` where the component knows its category; use `ILoggerFactory` only when additional categories are required.
- Emit:
  - `Information` for high-level progress (page rendering).
  - `Debug` for layout decisions (skipped whitespace, spacing adjustments).
  - `Trace` for fragment-level traversal when helpful.
  - `Warning` when encountering unsupported fragments or styles.
  - `Error` when rendering fails. Re-throw the exception after logging.

## Testing Strategy

| Stage | Must Cover | Tools |
| --- | --- | --- |
| Unit | Individual renderer methods (`RenderLine`, `RenderImage`) using mocks or stubs. | Custom test doubles, snapshot comparisons for simple render targets. |
| Integration | Full document rendering using the new renderer. | `HtmlConverter` or renderer-specific facade. |
| Diagnostics | Logging and error pathways. | `TestOutputLoggerProvider` or equivalent. |

For non-PDF outputs, design verification helpers that assert semantic equality (e.g., compare SVG elements, not raw strings).

## Wiring the Renderer

- Expose the renderer through an options object or new facade. Example:
  ```csharp
  var renderer = new SvgRenderer(new SvgFragmentRendererFactory(svgOptions), loggerFactory);
  var svg = await renderer.RenderAsync(layout, svgOptions);
  ```
- Avoid static singletons. Allow multiple renderer instances with different configurations to coexist.

## Documentation & Samples

- Add usage instructions to `docs/architecture.md` or create a dedicated doc.
- Provide sample inputs/outputs under the new project (`samples/` or `html/` folders) to help future contributors test locally.

## Checklist

- [ ] Factory and renderer implementations created.
- [ ] Logging integrated at appropriate levels.
- [ ] Unit + integration tests cover rendering and diagnostics.
- [ ] Documentation and sample usage added.
- [ ] Optional: console or test harness updated to demo the new renderer.

By following these steps your renderer will plug into the existing pipeline cleanly and remain easy to evolve.

