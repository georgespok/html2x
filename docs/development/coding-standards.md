# Coding Standards

These standards keep Html2x maintainable as layout and rendering support expands.

## Layering

Cross-layer dependencies flow forward:

```text
Html2x.RenderModel
  -> Html2x.LayoutEngine.Contracts
  -> Html2x.LayoutEngine
  -> Html2x.Renderers.Pdf

Html2x
  -> stage-owned settings and requests
```

The facade owns public options and service wiring. Internal stages consume
stage-owned settings and requests, not facade option objects. Layout owns
HTML/CSS interpretation. Renderers consume fragments and pages. Diagnostics
contracts live in `Html2x.Diagnostics.Contracts`, while collection and JSON
serialization live in `Html2x.Diagnostics`.

## Implementation Conventions

- Target .NET 8 across all projects.
- Keep classes single-purpose and name the responsibility clearly.
- Prefer immutable records or readonly structs for value contracts.
- Use option objects and injected services instead of static mutable state.
- Use `var` when the right-hand type is obvious.
- Keep public APIs documented with XML comments.
- Keep methods short enough that validation, calculation, and projection steps are visible.
- Use guard clauses for required dependencies and invalid options.
- Use braces for all control-flow blocks.
- Keep layout, rendering, and diagnostics concerns separated by module ownership.
- Do not add OpenAPI, YAML, or web API contracts. Html2x is an in-process
  library with a console harness.

## Naming

- Use PascalCase for public APIs and types.
- Use camelCase for private fields, locals, and parameters.
- Suffix async methods with `Async`.
- Name types by the domain fact they represent or the transformation they
  perform. A reader should understand the module role from the type name before
  opening the file.
- Prefer a small, repeatable vocabulary of type roles across the codebase:
  `Construction`, `Layout`, `Measurement`, `Rules`, `Writer`, `Reader`,
  `Request`, `Result`, `Facts`, `Options`, `Settings`, `Snapshot`, `Record`,
  `Adapter`, and narrowly scoped `Rule`.
- Use `Construction` for modules that create an internal object graph from
  input facts.
- Use `Layout` for modules that place visual structures or produce geometry.
- Use `Measurement` for modules that compute sizes, extents, or metrics without
  mutating the measured source.
- Use `Rules` for pure domain decisions, scalar calculations, normalization
  policy, or selection logic.
- Use `Writer` only for modules that mutate internal state, write output facts,
  or emit serialized output. Prefer `Reader` only for modules whose main role is
  loading or reading input from a source.
- Use `Adapter` for wrappers over external dependencies, compatibility seams, or
  alternate concrete implementations. Name the adapted thing, not just the
  pattern.
- Use `Request`, `Result`, `Facts`, `Options`, `Settings`, `Snapshot`, and
  `Record` for data types crossing seams or describing observable state.
- Use singular `Rule` for an adapter selected from a rule set, such as a
  block-kind rule. Use plural `Rules` for pure rule collections with no hidden
  state.
- Avoid broad implementation-pattern suffixes as primary type names:
  `Manager`, `Helper`, `Utility`, `Processor`, `Handler`, `Service`, `Factory`,
  `Builder`, `Projector`, `Appender`, `Inserter`, `Resolver`, `Engine`,
  `Executor`, `Applier`, `Classifier`, `Calculator`, `Context`, `Mapper`,
  `Planner`, and `Materializer`.
- Existing types with broad suffixes may remain during incremental redesign.
  New types should use them only when the module has a real seam, the suffix is
  already established in that module, and a clearer domain role would be
  misleading.
- Apply the deletion test before extracting a type. If deleting the type only
  moves one short method back to its caller, keep the behavior in the deeper
  module.
- Prefer fewer deeper modules over many shallow modules named after local
  implementation steps.

## Logging And Diagnostics

- Use existing log helper classes such as `LayoutLog` and `PdfRendererLog`.
- Use `LoggerMessage.Define` for reusable events.
- Guard high-volume trace or debug events with `logger.IsEnabled`.
- Prefer structured diagnostics fields when a behavior needs testable evidence.

## Error Handling

- Validate early and throw clear exceptions.
- Log or emit diagnostics before rethrowing conversion failures when a diagnostics sink exists.
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
