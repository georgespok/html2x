# Extending Renderers

Html2x is designed so renderers consume `HtmlLayout` and fragment contracts without depending on parser or layout internals.

## New Renderer Shape

A new renderer should:

- Reference `Html2x.RenderModel` and diagnostics contracts as needed.
- Accept `HtmlLayout` and renderer-specific options.
- Iterate pages in source order.
- Dispatch by fragment type.
- Treat fragments as read-only.
- Emit diagnostics or structured warnings for unsupported fragment types.

Example shape:

```csharp
public sealed class SvgRenderer
{
    public Task<byte[]> RenderAsync(HtmlLayout layout, SvgOptions options)
    {
        // Create an output surface, visit pages and fragments, return bytes.
    }
}
```

## Required Coverage

- Unit tests for drawing helpers or fragment dispatch.
- Integration tests with minimal documents.
- Diagnostics tests for unsupported fragments and failure paths.
- Semantic output assertions instead of raw string or binary equality when possible.

## Contract Rule

If the renderer needs data that is not present on fragments, extend the fragment contract and update layout projection. Do not make the renderer inspect DOM, CSS, style, or box objects.

Renderer-specific settings belong with the renderer. Converter facade options
belong in `Html2x` and should be mapped into renderer-owned settings by the
facade or composition layer.

## Fragment Policy

The render model fragment set is closed and repo-owned. Built-in pagination,
diagnostics snapshots, paint command resolution, and PDF rendering dispatch over
the known fragment types. Custom fragment subclasses are not a supported
extension model for the built-in pipeline.

Adding a fragment kind requires coordinated updates to:

- Fragment projection from published layout facts.
- Pagination clone and placement behavior.
- Diagnostics snapshot mapping.
- Renderer paint command resolution and drawing.
- Architecture and behavior tests for unsupported fragment handling.
