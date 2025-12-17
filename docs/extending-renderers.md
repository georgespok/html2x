# Extending Renderers

Html2x is renderer-agnostic: the fragment tree is the contract. This guide explains how to add a new renderer (e.g., SVG, Canvas, alternate PDF engine) or extend the existing SkiaSharp PDF implementation.

## Architectural Overview

The current PDF renderer (`Html2x.Renderers.Pdf`) is a direct drawing pipeline:

```
HtmlLayout (pages -> fragments)
        |
PdfRenderer -> SKDocument + SKCanvas
        |
SkiaFragmentDrawer (fragment-type dispatch)
```

## Adding a New Renderer Project

1. **Create a new project** (e.g., `Html2x.Svg`) referencing `Html2x.Abstractions`.
2. **Implement a renderer facade** (similar to `PdfRenderer`):
   - Own output orchestration (file/bytes/stream).
   - Iterate pages and create a per-page drawing surface.
3. **Implement a fragment drawer** (similar to `SkiaFragmentDrawer`):
   - Honor the fragment contract (do not mutate fragments).
   - Handle block, line, image, and rule fragments.
   - If a fragment is unsupported, log a warning or throw, depending on policy.


## Testing Strategy

| Stage | Must Cover | Tools |
| --- | --- | --- |
| Unit | Individual drawing methods (text, borders, images) using mocks or stubs. | Custom test doubles, geometry assertions. |
| Integration | Full document rendering using the new renderer. | `HtmlConverter` or renderer-specific facade. |
| Diagnostics | Logging and error pathways. | `TestOutputLoggerProvider` or equivalent. |

For non-PDF outputs, design verification helpers that assert semantic equality (e.g., compare SVG elements, not raw strings).

## Wiring the Renderer

- Expose the renderer through an options object or new facade. Example:
  ```csharp
  var renderer = new SvgRenderer(svgOptions);
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

