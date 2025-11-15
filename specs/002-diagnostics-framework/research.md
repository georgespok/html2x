# Research Log – Diagnostics Framework

## Session Initialization Without Core Dependencies
- **Decision**: Html2x facade owns the diagnostics service provider, constructing `Html2x.Diagnostics` components only when callers enable sessions.
- **Rationale**: Keeps core rendering assemblies free of diagnostics references while still allowing opt-in instrumentation.
- **Alternatives considered**: (1) Linking diagnostics directly from `Html2x.Layout`—rejected because it violates Principle I; (2) Using reflection-based late binding—rejected for Principle III compliance and maintainability.

## Synchronous Observer Pipeline
- **Decision**: Observers and sinks execute synchronously on the rendering thread; if a sink throws, the exception propagates and diagnostics capture the failure for investigation.
- **Rationale**: Synchronous execution guarantees deterministic ordering and simplifies failure propagation; asynchronous dispatch would add buffering and race conditions.
- **Alternatives considered**: (1) Background dispatcher per sink-rejected because it breaks determinism; (2) Task-based queue-rejected due to additional allocations and scheduler variability.

## DiagnosticsModel Payload Shape
- **Decision**: Events include a diagnostics context map capturing stage-specific reasoning (e.g., shrink-to-fit widths), along with references to structured dumps.
- **Rationale**: Ensures downstream tools can replay decisions and correlate dumps; aligns with spec emphasis on reasoning metadata.
- **Alternatives considered**: (1) Separate event and context streams—rejected to avoid synchronization complexity; (2) Embedding raw dumps in every event—rejected to limit payload size.

## Sink Portfolio
- **Decision**: Ship JSON and console sinks immediately; define backlog stories for SVG/PDF sinks; evaluate a test-only `InMemorySink`.
- **Rationale**: JSON + console meet near-term debugging needs; SVG/PDF require visualization tooling outside current scope. InMemory sink may simplify contract tests if JSON serialization proves heavy.
- **Alternatives considered**: (1) Building all sinks now—rejected due to scope risk; (2) Skipping console sink—rejected because TestConsole requires live telemetry.

## Sensitive Data Handling
- **Decision**: Diagnostics capture full data (unredacted); downstream sinks are responsible for sanitization before persistence.
- **Rationale**: Guarantees accurate debugging context and matches spec clarification. Allows consumers to apply policy-specific filters.
- **Alternatives considered**: (1) Default redaction-rejected because it may mask critical layout inputs; (2) Configurable allowlist/denylist-rejected as unnecessary complexity for v1.

## ILogger Inventory (2025-11-14)
| Assembly | Key Files | Purpose Today | Migration Notes |
|----------|-----------|---------------|-----------------|
| Html2x (facade) | `HtmlConverter`, `LayoutBuilderFactory` | Creates `ILoggerFactory` instances so layout/rendering components can emit debug traces. | Will be wrapped by diagnostics decorators; `ILogger` stays internal with no console output. |
| Html2x.LayoutEngine | `LayoutBuilder`, `CssStyleComputer`, `LayoutLog`, `StyleLog` | Stage start/stop messages plus invalid spacing warnings. | Replace with diagnostics events/dumps; disable direct logging once T009 lands. |
| Html2x.Renderers.Pdf | `PdfRenderer`, `FragmentRenderDispatcher`, `QuestPdfFragmentRenderer*`, `PdfRendererLog` | Verbose tracing when emitting PDFs. | Translate to diagnostics sinks; leave `ILogger` hooks only for opt-in diagnostics runtime. |
| Html2x.Test | `TestOutputLoggerProvider`, `HtmlConverterTests` | Custom provider sending logs to xUnit output. | Remains for tests only; not part of shipping assemblies. |
| Html2x.TestConsole | Program uses `ILogger` only for stdout diagnostics (no direct `ILogger` references today but console output is allowed). | Sole component allowed to print via `ILogger` going forward; all other assemblies must route through diagnostics sinks. |

**Policy**: Html2x.TestConsole retains console printing for operator feedback. All other `ILogger` usages will either be wrapped by diagnostics (T009/T010) or silenced behind the diagnostics runtime to preserve the "no console noise" requirement.

## Backlog - Visualization & Metadata Sinks (T033)

**Goal**: Extend the sink portfolio beyond JSON/console/in-memory so operators can visualize layout decisions and embed diagnostics traces directly inside generated PDFs.

### SVG Visualization Sink
- **Scope**: Generate a deterministic SVG timeline/graph for each render showing stage durations, layout tree snapshots, and context annotations.
- **Dependencies**: Requires structured dump schemas for fragments/pagination plus a canonical palette documented in `docs/diagnostics.md`.
- **Open Questions**:
  1. How large can the SVG get before it impacts CI artifact limits? (Need benchmark against 200-page renders.)
  2. Should the sink inline structured dump excerpts or reference external JSON blobs?
- **Acceptance Signals**:
  - CLI flag `--diagnostics-svg <path>` flows through `DiagnosticsOptions`.
  - Contract test ensures SVG output diffable (stable ids, timestamps rounded).

### PDF Metadata Sink
- **Scope**: Annotate the produced PDF with a diagnostics attachment (XMP metadata or embedded JSON file) so downstream viewers can inspect renders without external artifacts.
- **Dependencies**: QuestPDF hooks for custom attachments, plus guidance on stripping metadata for privacy-sensitive tenants.
- **Open Questions**:
  1. Do we attach the full diagnostics JSON or a summary hash? Needs alignment with compliance.
  2. How does this interact with incremental PDF streaming (performance impact)?
- **Acceptance Signals**:
  - `DiagnosticsOptions.EnablePdfMetadataSink` toggles the behavior.
  - Regression test asserts PDF metadata contains session id + stage count.

### Next Actions
- Prototype spike documenting QuestPDF extensibility and a minimal SVG timeline.
- Update `docs/diagnostics.md` and `specs/002-diagnostics-framework/spec.md` once designs solidify.
- Track privacy review for both sinks before implementation.
