# Architecture Overview

**Html2x** is a modular, testable .NET library that converts **static** HTML/CSS into PDF (and later, other targets). It is **pure .NET** and **cross-platform** (Windows/Linux), with **no GDI+**, **no JavaScript**, and a focused CSS subset suitable for business reports.

```
HTML/CSS
  ↓
DOM + CSSOM (AngleSharp)
  ↓
Style Tree (computed styles)
  ↓
Box Tree (layout model)
  ↓
Fragment Tree (lines/pages)
  ↓
Renderer (PDF via QuestPDF)
```

## Pipeline Responsibilities

### DOM + CSSOM (AngleSharp)
- **Responsibility**: Parse markup, build the DOM, and resolve author stylesheets into a CSSOM.
- **Input**: Raw HTML plus inline and linked CSS pulled from the source document.
- **Output**: AngleSharp `IDocument` with associated CSS rule sets ready for cascade evaluation.
- **Implementation pattern**: `AngleSharpDomProvider` hides parser configuration, ensuring consistent options (character set, CSS support) across the codebase.
- **Extension rules**: Add feature flags or parser tweaks inside the provider. Keep DOM concerns isolated so later stages never depend on AngleSharp types directly.

### Style Tree (Computed Styles)
- **Responsibility**: Apply CSS cascade, inheritance, and defaults to produce `ComputedStyle` snapshots per element.
- **Input**: DOM plus CSSOM from the parser stage.
- **Output**: Style tree nodes that carry explicit values for layout.
- **Implementation pattern**: `CssStyleComputer` combines traversal helpers with `UserAgentDefaults`, yielding immutable data structures for predictable processing.
- **Extension rules**: When introducing new CSS properties, extend the style computer and defaults in `Html2x.Layout`. Keep calculations pure, returning data without mutating downstream stages.

### Box Tree (Layout Model)
- **Responsibility**: Translate styled elements into layout boxes that encode formatting contexts, dimensions, and flow.
- **Input**: Style tree with computed values.
- **Output**: Box hierarchy representing block and inline flows plus page level metrics.
- **Implementation pattern**: `BoxTreeBuilder` produces concrete box types and stores pagination hints (break before, break after, avoid) used later.
- **Extension rules**: Add new formatting contexts by introducing specialized box builders while keeping the resulting model immutable. Share measurement helpers via `Html2x.Core` to prevent duplicated math.

### Fragment Tree (Lines and Pages)
- **Responsibility**: Convert boxes into positioned fragments, paginate content, and assemble `LayoutPage` instances.
- **Input**: Box tree enriched with geometry and page settings.
- **Output**: Fragment collection containing `LineBoxFragment`, `TextRun`, `ImageFragment`, and block containers ready for rendering.
- **Implementation pattern**: `FragmentBuilder` walks the box tree, measures text, and assigns absolute coordinates so every renderer can consume the same artifact.
- **Extension rules**: Introducing new fragment types requires updating visitor interfaces in `Html2x.Core` and renderer dispatchers. Keep pagination logic self-contained to maintain deterministic outputs.

### Renderer (QuestPDF)
- **Responsibility**: Serialize fragments into the final artifact. The default implementation renders PDF via QuestPDF.
- **Input**: `HtmlLayout` plus `PdfOptions`.
- **Output**: Byte array containing the generated PDF.
- **Implementation pattern**: `PdfRenderer` orchestrates page construction, leverages `IFragmentRendererFactory`, and emits structured logs for diagnostics. `QuestPdfFragmentRenderer` maps fragments to QuestPDF fluent constructs.
- **Extension rules**: New render targets supply custom `IFragmentRenderer` implementations or alternate factories. Rendering code should never reach back to the DOM; it consumes fragments only.

## Solution Layout

| Path | Purpose | Primary Entry Points | Extension Notes |
| --- | --- | --- | --- |
| `src/Html2x.Core` | Shared contracts and value types for styles, boxes, fragments, units, and colors. | `Fragment`, `BlockFragment`, `LineBoxFragment`, `LayoutPage`, `ComputedStyle`. | Add new fragment or style primitives here first so downstream projects stay aligned. |
| `src/Html2x.Layout` | Pipeline from DOM + CSS to fragment tree. | `LayoutBuilder`, `AngleSharpDomProvider`, `CssStyleComputer`, `BoxTreeBuilder`, `FragmentBuilder`. | Keep each stage side effect free. New layout features should appear as new box types or fragment builders rather than injected logic elsewhere. |
| `src/Html2x.Pdf` | QuestPDF backed renderer plus logging helpers. | `PdfRenderer`, `QuestPdfFragmentRendererFactory`, `QuestPdfFragmentRenderer`, `PdfRendererLog`. | Alternate renderers belong in sibling projects that implement the same interfaces. |
| `src/Html2x` | Orchestration facade consumed by callers. | `HtmlConverter`. | Wire new diagnostics or configuration options here, keeping defaults lightweight. |
| `src/Html2x.TestConsole` | Manual smoke harness with console logging. | `Program`. | Use for ad hoc validation; keep dependencies minimal. |
| `src/Html2x.Layout.Test` | Unit tests for DOM, CSS, and layout stages. | xUnit test classes grouped by stage. | When extending layout, add golden tests here first. |
| `src/Html2x.Pdf.Test` | Renderer tests, PDF validators, recording helpers. | `PdfRendererTests`, `PdfValidator`. | Ensure renderer regressions are caught before integration. |
| `src/Html2x.Test` | End-to-end and integration coverage using the public API. | `HtmlConverterTests`. | Exercise full pipeline with realistic HTML. |

## Projects & Responsibilities

### Html2x.Core

- **Role**: Provide the stable vocabulary for styles, boxes, fragments, geometry, and shared interfaces. The rest of the solution consumes these types.
- **Key namespaces**: `Html2x.Core.Layout` (fragments and pages), `Html2x.Core.Style` (computed style records), `Html2x.Core.Geometry` (dimensions, rectangles, colors), `Html2x.Core.Services` (interfaces such as `IFontResolver` and `IImageProvider`).
- **Inputs and outputs**: Exposes immutable records and interfaces. Holds no runtime dependencies so renderers and layout can reference it without pulling extra packages.
- **Extension guidance**:
  1. Introduce new fragment or style concepts here before touching other projects.
  2. Prefer records or readonly structs to keep value semantics obvious.
  3. Keep the assembly pure; do not add logging, IO, or rendering references.

### Html2x.Layout

- **Role**: Implement the full HTML to fragment pipeline: DOM → Style → Box → Fragment.
- **Components**:
  - `AngleSharpDomProvider` for DOM loading.
  - `CssStyleComputer`, `UserAgentDefaults`, `StyleTraversal` for cascade logic.
  - `BoxTreeBuilder`, inline layout helpers, and pagination utilities.
  - `FragmentBuilder` plus the `LayoutBuilder` façade (with optional logging).
- **Inputs and outputs**: Accepts HTML along with `Dimensions`, returns an `HtmlLayout` ready for rendering.
- **Extension guidance**:
  1. Keep stage logic deterministic. If a feature needs external data, pass it in through abstractions.
  2. Add new CSS properties in the style computer first, then propagate to boxes if geometry changes.
  3. Encapsulate pagination logic near the box to fragment conversion to limit regression scope.

### Html2x.Pdf

- **Role**: Render `HtmlLayout` objects to PDF bytes via QuestPDF while emitting diagnostics.
- **Components**:
  - `PdfRenderer` orchestrator and `PdfRendererLog` helper.
  - `IFragmentRendererFactory` plus `QuestPdfFragmentRendererFactory` for dependency injection.
  - `QuestPdfFragmentRenderer` and `FragmentRenderDispatcher` for fragment traversal.
- **Inputs and outputs**: Consumes fragment pages with `PdfOptions`; produces PDF byte arrays and structured logs.
- **Extension guidance**:
  1. Treat fragments as read-only facts. If data is missing, fix the layout stage.
  2. Use `LoggerMessage` helpers for new diagnostics to avoid allocation churn.
  3. To add a different renderer, create a sibling project that implements `IFragmentRenderer` and leave this assembly focused on QuestPDF.

### Html2x (Facade)

- **Role**: Provide the developer facing `HtmlConverter` API that wires layout, rendering, and diagnostics.
- **Inputs and outputs**: Accepts HTML and `PdfOptions`, returns PDF bytes. Optionally accepts `ILoggerFactory` to illuminate the pipeline.
- **Extension guidance**: Keep this layer thin; add configuration knobs or logging hooks but redirect substantive behavior to the pipeline projects.

### Test Projects

- **Html2x.Layout.Test**: Unit and golden tests that guard DOM parsing, style computation, box construction, and fragmentation. Extend these fixtures when introducing new layout logic.
- **Html2x.Pdf.Test**: Renderer tests that validate `PdfRenderer`, assert PDF structure, and support fragment recording.
- **Html2x.Test**: End-to-end coverage through `HtmlConverter`, piping logs into `ITestOutputHelper` for diagnostics.
- **Html2x.TestConsole**: Manual harness for exploratory runs. Keep dependencies light and rely on console logging for visibility.

## Extension Guides

- **Adding CSS support**: See `docs/extending-css.md` for a step-by-step walkthrough of propagating new properties through the pipeline.
- **Building new renderers**: See `docs/extending-renderers.md` for factory patterns, logging expectations, and testing guidance.
- **Roadmap & documentation backlog**: `docs/plan.md` tracks upcoming guides and feature enablement tasks; update it when new work begins.

## Error Handling & Guarantees

- **Parsing resilience**: DOM and CSS parsing ignore unsupported tags or properties and log at `Warning` when logging is enabled. Failures that block parsing (like unreadable streams) bubble as exceptions so callers can surface meaningful errors.
- **Deterministic layout**: Every pipeline stage is pure. Given the same HTML, options, and supporting services, the resulting fragment tree is bit-for-bit identical. Cache or snapshot outputs when diagnosing regressions.
- **Renderer safety**: `PdfRenderer` wraps QuestPDF calls, logs at `Error`, and rethrows so hosts can decide whether to retry or surface failures. Temporary resources are confined to local streams and disposed immediately.
- **Thread isolation**: The library avoids static mutable state. You can create multiple converters in parallel as long as upstream dependencies (fonts, file access) are thread safe.

## Design Principles

1. **Fragments are the contract**: Every renderer consumes fragments only. If you find yourself needing DOM or style nodes in a renderer, revisit the layout stage and extend the fragment model instead.
2. **Explicit stages**: Parsing, styling, box building, and rendering remain separate to keep reasoning simple and testing cheap. Do not blend responsibilities across stages.
3. **Configuration over convention**: New behaviors should be toggled through options or injected services, not hidden switches. This keeps the public API predictable.
4. **Cross-platform focus**: Stay with managed code paths (QuestPDF, AngleSharp). If a feature requires native interop, isolate it behind interfaces to preserve portability.
5. **Observable by default**: When adding complex logic, add structured logging hooks at the same time so future debugging is easier.
## Diagnostics Logging

The pipeline now exposes structured diagnostics across the layout and rendering layers.

* **Factory wiring:** Pass an `ILoggerFactory` into `HtmlConverter` (or directly into `LayoutBuilderFactory` / `PdfRenderer`) to light up logging. When omitted, the system falls back to `NullLogger` to avoid noisy output.
* **Severity bands:**
  * `Information` — layout start/end, renderer layout summaries
  * `Debug` — per-page renderer metrics, layout stage completion
  * `Trace` (treat as Verbose) — fragment traversal details in both the renderer dispatcher and nested block rendering
  * `Warning` — unsupported fragments such as images or unknown block children
  * `Error` — unhandled exceptions captured in `PdfRenderer.RenderAsync`
* **Correlation data:** Renderer logs include the fragment type, absolute coordinates, and a hash of the active `PdfOptions` so multi-run comparisons stay lightweight.
* **Performance:** Logging is pay-for-play; `LoggerMessage.Define` avoids allocations when levels are disabled. Verbose/Trace calls guard `IsEnabled` checks so high-volume traces stay dormant until explicitly enabled.
* **Future hooks:** Layout logging touches only the façade (`LayoutBuilder`). Additional stages (style resolution, pagination heuristics) can extend `LayoutLog.StageComplete` or add new events without constructor churn.

With these hooks you can capture a full breadcrumb trail from HTML ingestion through PDF emission during troubleshooting, then turn it off cleanly for regular runs.

## Testing Strategy

| Layer | Purpose | Typical Fixtures | Tooling Notes |
| --- | --- | --- | --- |
| Core units | Validate geometry math, color conversions, style defaults, and fragment invariants. | Inline data driven tests covering edge cases (zero widths, negative margins, etc.). | Use xUnit `[Theory]` to document edge scenarios. |
| Layout stage | Guard DOM parsing, cascade, and pagination logic. | HTML snippets in `Html2x.Layout.Test` with approved fragment snapshots or targeted assertions. | Favor deterministic snapshots; when behavior changes intentionally, update fixtures with rationale. |
| Renderer stage | Ensure QuestPDF translation remains consistent. | Synthetic fragment trees rendered to PDF; assertions via `PdfValidator` and `PdfWordParser`. | Add fragment recording tests when debugging traversal behavior. |
| Integration | Exercise the full `HtmlConverter` pipeline. | Realistic documents rendered during tests with PDF header and metadata checks. | Plug in `TestOutputLoggerProvider` to capture diagnostics inside the test output. |
| Manual harness | Provide fast validation during development. | `Html2x.TestConsole` sample HTML files. | Point to local or temporary files; review console logs for layout traces. |

**When adding features**
1. Start with core or layout tests that express the new behavior.
2. Add renderer assertions only if fragment output changes do not fully capture the intent.
3. Update integration coverage once the end-to-end result stabilizes.
4. Record PDF artifacts sparingly and document the reason in test comments.


