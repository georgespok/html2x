# Coding Standards

This document captures the conventions and engineering principles that keep Html2x maintainable as the feature-set expands.

## Foundation

- **SOLID first**: Each class owns a single responsibility. Build extension points through interfaces and factories rather than `if/else` forks.
- **DRY + KISS**: Prefer composable helpers and targeted abstractions. Avoid speculative generalisations; refactor when duplication reveals the underlying pattern.
- **Readable by default**: Code should explain itself. When intent is non-obvious, add short comments or extract well named helpers.
- **Managed-only**: The solution must remain pure .NET (QuestPDF, AngleSharp). Any unavoidable native integration must be wrapped behind interfaces with clear contracts.

## Layering & Boundaries

| Area | Responsibilities | Must Not |
| --- | --- | --- |
| `Html2x.Core` | Data contracts (styles, boxes, fragments), shared geometry, service interfaces. | Depend on AngleSharp, QuestPDF, logging implementations, or I/O primitives. |
| `Html2x.Layout` | HTML → Fragment pipeline (DOM, style, box, fragment). | Reference QuestPDF or mutate fragment data after creation. |
| `Html2x.Pdf` | Fragment → PDF rendering and diagnostics. | Reach back into DOM/style/box types; mutate fragment state. |
| `Html2x` (facade) | Orchestrate layout + renderer wiring for consumers. | Contain layout or rendering logic. |

Cross-layer calls must always travel forward (Core → Layout → Renderer). If you need information from an earlier stage, add it to the fragment model rather than reaching back.

## Implementation Conventions

### Constructors & Options

- Provide guard clauses for required dependencies and use optional parameters for feature toggles.
- Constructors should accept interfaces (or the minimal abstraction) and prefer `ILogger<T>` when the consumer knows the category. Use `ILoggerFactory` only when you must create multiple categories.
- Expose configuration via option objects (`PdfOptions`) or factories instead of static state.

### Logging

- Use `LoggerMessage.Define` helpers for reusable events (`RendererLog`, `LayoutLog`).
- Guard high-volume events (`Trace` / `Debug`) with `logger.IsEnabled` to avoid unnecessary allocations.
- Emit context-rich messages (fragment type, coordinates, options hashes) so diagnostics remain actionable.

### Error Handling

- Propagate exceptions to the caller after logging. Do not swallow failures silently.
- Prefer early validation with clear messages (e.g., `ArgumentNullException`) to prevent downstream faults.
- Keep throwing code close to the root cause (e.g., DOM provider for HTML parsing failures).

### Extensibility Hotspots

- **New CSS property**: Update `CssStyleComputer`, extend the box model if geometry changes, and add fragment fields if renderers need the data.
- **New fragment type**: Define it under `Html2x.Core.Layout`, update visitors, builders, and renderers. Maintain immutability.
- **New renderer**: Implement `IFragmentRenderer` and provide a factory. Reuse the dispatcher pattern for traversal.
- **New diagnostics**: Add to the relevant log helper class (`RendererLog`, `LayoutLog`) instead of ad-hoc `logger.Log...` calls.

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
