# Renderer Internals

Html2x does not expose a public renderer extension point. Renderer work is an
internal codebase change: the built-in PDF renderer consumes `HtmlLayout` and
fragment contracts without depending on parser or layout internals.

## Internal Renderer Shape

An internal renderer should:

- Reference render model and diagnostics contracts as needed.
- Accept `HtmlLayout` and renderer-specific options.
- Iterate pages in source order.
- Dispatch by fragment type.
- Treat fragments as read-only.
- Emit diagnostics or structured warnings for unsupported fragment types.

## Required Coverage

- Unit tests for drawing helpers or fragment dispatch.
- Integration tests with minimal documents.
- Diagnostics tests for unsupported fragments and failure paths.
- Semantic output assertions instead of raw string or binary equality when possible.

## Settings Ownership

Renderer-specific settings belong with the renderer and can stay internal.
Converter facade options belong in `Html2x` and should be mapped into
renderer-owned settings by the facade composition layer.

## Contract Rule

If the renderer needs data that is not present on fragments, extend the fragment contract and update fragment tree building. Do not make the renderer inspect DOM, CSS, style, or box objects.

## Fragment Policy

The render model fragment set is closed and repo-owned. Built-in pagination,
diagnostics snapshots, paint command resolution, and PDF rendering dispatch over
the known fragment types. Custom fragment subclasses are not a supported
extension model for the built-in pipeline.

Adding a fragment kind requires coordinated updates to:

- Fragment tree building from published layout facts.
- Pagination clone and placement behavior.
- Diagnostics snapshot mapping.
- Renderer paint command resolution and drawing.
- Architecture and behavior tests for unsupported fragment handling.

See [Fragment Kinds](fragment-kinds.md) before adding a new fragment type.
