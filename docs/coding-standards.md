# Coding Standards

This document captures the conventions and engineering principles that keep Html2x maintainable as the feature-set expands.

## Foundation

- **SOLID first**: Each class owns a single responsibility. Build extension points through interfaces and factories rather than `if/else` forks.
- **DRY + KISS**: Prefer composable helpers and targeted abstractions. Avoid speculative generalisations; refactor when duplication reveals the underlying pattern.
- **Readable by default**: Code should explain itself. When intent is non-obvious, add short comments or extract well named helpers.
- **Managed-only**: The solution must remain pure .NET (SkiaSharp, AngleSharp). Any unavoidable native integration must be wrapped behind interfaces with clear contracts.

## Layering & Boundaries

| Area | Responsibilities | Must Not |
| --- | --- | --- |
| `Html2x.Abstractions` | Data contracts (styles, fragments, diagnostics, measurements), shared geometry, service interfaces. | Reference AngleSharp or SkiaSharp, or perform file I/O. |
| `Html2x.LayoutEngine` | HTML -> Fragment pipeline (DOM, style, box, fragment). | Reference SkiaSharp or mutate fragment data after creation. |
| `Html2x.Renderers.Pdf` | Fragment -> PDF rendering and diagnostics. | Reach back into DOM/style/box types; mutate fragment state. |
| `Html2x` (facade) | Orchestrate layout + renderer wiring for consumers. | Contain layout or rendering logic. |

Cross-layer calls must always travel forward (Abstractions -> LayoutEngine -> Renderer). If you need information from an earlier stage, add it to the fragment model rather than reaching back.

## Implementation Conventions

### Constructors & Options

- Provide guard clauses for required dependencies and use optional parameters for feature toggles.
- Expose configuration via option objects (`PdfOptions`) or factories instead of static state.

### Logging

- Use `LoggerMessage.Define` helpers for reusable events (`PdfRendererLog`, `LayoutLog`).
- Guard high-volume events (`Trace` / `Debug`) with `logger.IsEnabled` to avoid unnecessary allocations.
- Emit context-rich messages (fragment type, coordinates, options hashes) so diagnostics remain actionable.

### Error Handling

- Propagate exceptions to the caller after logging. Do not swallow failures silently.
- Prefer early validation with clear messages (e.g., `ArgumentNullException`) to prevent downstream faults.
- Keep throwing code close to the root cause (e.g., DOM provider for HTML parsing failures).

### Extensibility Hotspots

- **New CSS property**: Update `CssStyleComputer`, extend the box model if geometry changes, and add fragment fields if renderers need the data.
- **New fragment type**: Define it under `Html2x.Abstractions.Layout`, update visitors, builders, and renderers. Maintain immutability.
- **New renderer**: Implement `IFragmentRenderer` and provide a factory. Reuse the dispatcher pattern for traversal.
- **New diagnostics**: Add to the relevant log helper class (`PdfRendererLog`, `LayoutLog`) instead of ad-hoc `logger.Log...` calls.

### Shared Formatting and Failure Modes

- Route block semantics through shared formatting contracts (`BlockFormattingRequest` / `BlockFormattingResult` via `IBlockFormattingContext`) instead of duplicating behavior in top-level and inline-block paths.
- Treat inline-block formatting as canonical when block descendants exist. Avoid approximation-only merges (for example, taking `Math.Max` between unrelated height models without context checks).
- Keep fail-fast scope explicit: unsupported structure errors are allowed for inline-block internal formatting only. Do not widen fail-fast behavior to unrelated contexts without a dedicated spec and tests.
- Emit deterministic diagnostics payloads:
  - Preserve stable fragment ordering for snapshot mapping.
  - Use consistent tie-break rules when coordinates are equal (position, kind, then text key).
  - Ensure repeated runs on the same machine produce identical payload ordering.
- For diagnostics ordering changes, update both unit-level assertions (`Html2x.LayoutEngine.Test`) and scenario-level assertions (`Html2x.Test`) to lock behavior at multiple layers.

## Coding Style

- Follow `.editorconfig` (4-space C#/HTML, 2-space XML). No tabs.
- Prefer expression-bodied members only when they improve readability.
- Use `var` when the right-hand type is obvious; otherwise be explicit.
- Public APIs require XML documentation comments describing behavior and parameters.
- Name tests using `MethodName_Scenario_Expectation` and production members using standard .NET casing (PascalCase / camelCase).

## Review Checklist

Before submitting a change, validate:

1. **Layering**: Did the change respect the module boundaries? Are dependencies one-directional?
2. **Logging**: Are new behaviors observable at appropriate levels? Do we avoid noisy default logs?
3. **Tests**: Do new features arrive with failing tests first? Are snapshots updated with justification?
4. **Docs**: When introducing new extension points or patterns, add or update documentation in `docs/`.
5. **Naming**: Do class and method names convey intent? Could future contributors pick up the thread quickly?

When in doubt, prefer clarity over cleverness. Our goal is a codebase that any contributor can extend without surprises.

