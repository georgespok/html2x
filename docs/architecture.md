# Architecture Overview

**Html2x** is a modular, testable .NET library that converts **static** HTML/CSS into PDF (and later, other targets). It is **pure .NET** and **cross-platform** (Windows/Linux), with **no GDI+**, **no JavaScript**, and a focused CSS subset suitable for business reports.

```
HTML/CSS
  |
DOM + CSSOM (AngleSharp)
  |
Style Tree (computed styles)
  |
Box Tree (layout model)
  |
Fragment Tree (lines/pages)
  |
Renderer (PDF via SkiaSharp)
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
- **Implementation pattern**: `CssStyleComputer` combines `StyleTraversal`, `DefaultStyleDomFilter`, `CssValueConverter`, and `UserAgentDefaults`, yielding immutable records defined in `Html2x.Abstractions.Layout.Styles`.
- **Extension rules**: When introducing new CSS properties, extend the style computer and defaults under `Html2x.LayoutEngine.Style`. Keep calculations pure, returning data without mutating downstream stages.

### Box Tree (Layout Model)
- **Responsibility**: Translate styled elements into layout boxes that encode formatting contexts, dimensions, and flow.
- **Input**: Style tree with computed values.
- **Output**: Box hierarchy representing block and inline flows plus page level metrics.
- **Implementation pattern**: `BoxTreeBuilder` feeds `DisplayTreeBuilder`, `BlockLayoutEngine`, `InlineLayoutEngine`, and friends to produce immutable boxes plus pagination hints.
- **Extension rules**: Add new formatting contexts by introducing specialized box builders while keeping the resulting model immutable. Share geometry or measurement helpers via `Html2x.Abstractions` to prevent duplicated math.

### Fragment Tree (Lines and Pages)
- **Responsibility**: Convert boxes into positioned fragments, paginate content, and assemble `LayoutPage` instances.
- **Input**: Box tree enriched with geometry and page settings.
- **Output**: Fragment collection containing `LineBoxFragment`, `TextRun`, `ImageFragment`, and block containers ready for rendering.
- **Implementation pattern**: `FragmentBuilder` executes staged visitors (block, inline, specialized, z-order) while collaborating with `TextRunFactory`, `DefaultLineHeightStrategy`, and `DefaultTextWidthEstimator`.
- **Extension rules**: Introducing new fragment types requires updating interfaces and records in `Html2x.Abstractions` plus renderer dispatchers. Keep pagination logic self-contained to maintain deterministic outputs.

### Renderer (SkiaSharp)
- **Responsibility**: Serialize fragments into the final artifact. The default implementation renders PDF via SkiaSharp.
- **Input**: `HtmlLayout` plus `PdfOptions`.
- **Output**: Byte array containing the generated PDF.
- **Implementation pattern**: `PdfRenderer` creates an `SKDocument` and draws each page using `SkiaFragmentDrawer` and supporting drawers (images, borders, fonts).
- **Extension rules**: New render targets should consume fragments only. Rendering code should never reach back to the DOM.

## Solution Layout

| Path | Purpose | Primary Entry Points | Extension Notes |
| --- | --- | --- | --- |
| `src/Html2x.Abstractions` | Shared contracts for styles, fragments, diagnostics, measurements, and units. | `HtmlLayout`, `LayoutPage`, `Fragment`, `PageSize`, `ComputedStyle`. | Add or evolve primitives here first so layout and renderers stay in lockstep. |
| `src/Html2x.LayoutEngine` | DOM + CSS to fragment pipeline plus layout logging. | `LayoutBuilder`, `AngleSharpDomProvider`, `CssStyleComputer`, `BoxTreeBuilder`, `FragmentBuilder`, `LayoutLog`. | Keep stages pure; introduce measurement providers or observers instead of side effects. |
| `src/Html2x.Renderers.Pdf` | SkiaSharp PDF renderer and drawing pipeline. | `PdfRenderer`, `SkiaFragmentDrawer`, `SkiaFontCache`, `BorderShapeDrawer`, `ImageRenderer`. | Alternate renderers belong in sibling projects that reuse the fragment contract. |
| `src/Html2x` | Public facade consumed by host applications. | `HtmlConverter`, `LayoutBuilderFactory`. | Add configuration hooks or DI entry points here without embedding layout logic. |
| `src/Tests/Html2x.LayoutEngine.Test` | Layout unit and snapshot coverage. | `CssStyleComputerTests`, `BoxTreeBuilderTests`, `FragmentBuilderTests`. | Keep fixtures deterministic through builders rather than loose strings. |
| `src/Tests/Html2x.Renderers.Pdf.Test` | Renderer validation and PDF helpers. | `PdfRendererTests`, `BorderShapeDrawerTests`, `PdfWordParser`. | Only persist PDFs or fragment recordings when diagnosing failures. |
| `src/Tests/Html2x.Test` | End-to-end verification using the public API. | `HtmlConverterTests`, `IntegrationTestBase`. | Run with real fonts/options and capture logs through `TestOutputLoggerProvider`. |
| `src/Tests/Html2x.TestConsole` | Manual harness with console logging. | `Program`. | Use for ad hoc validation; keep dependencies minimal. |

## Projects & Responsibilities

### Html2x.Abstractions

- **Role**: Provide the stable vocabulary for styles, fragments, diagnostics, measurements, and service interfaces so every other project shares the same contracts.
- **Key namespaces**: `Html2x.Abstractions.Layout` (documents, fragments, styles), `Html2x.Abstractions.Measurements` (units, page sizes, spacing), `Html2x.Abstractions.Diagnostics` (structured log shapes), `Html2x.Abstractions.Utilities` (color helpers).
- **Inputs and outputs**: Exposes immutable records and light-weight interfaces. Carries zero runtime dependencies so renderers and layout can consume it without pulling extra packages.
- **Extension guidance**:
  1. Introduce new fragment types, measurement units, or diagnostic contracts here before touching other projects.
  2. Prefer records or readonly structs to keep value semantics obvious.
  3. Keep the assembly pure; do not add logging, IO, or renderer references.

### Html2x.LayoutEngine

- **Role**: Implement the complete HTML to fragment pipeline: DOM -> Style -> Box -> Fragment.
- **Components**:
  - `AngleSharpDomProvider` for DOM loading and normalization.
  - `CssStyleComputer`, `UserAgentDefaults`, `StyleTraversal`, and `DefaultStyleDomFilter` for cascade logic.
  - `BoxTreeBuilder`, `DisplayTreeBuilder`, inline/float/table layout engines, and pagination utilities.
  - `FragmentBuilder` with staged visitors, `TextRunFactory`, `FontMetricsProvider`, `DefaultLineHeightStrategy`, and the `LayoutBuilder`/`LayoutLog` facades.
- **Inputs and outputs**: Accepts HTML with `PageSize`, returns an `HtmlLayout` ready for rendering.
- **Extension guidance**:
  1. Keep stage logic deterministic. If a feature needs external data, pass it in through abstractions.
  2. Add new CSS properties in the style computer first, then propagate to boxes or fragments if geometry changes.
  3. Encapsulate pagination logic near the box-to-fragment conversion to limit regression scope.

### Html2x.Renderers.Pdf

- **Role**: Render HtmlLayout objects to PDF bytes via SkiaSharp while emitting diagnostics.
- **Components**:
  - `PdfRenderer` orchestrator that writes `SKDocument` output.
  - `SkiaFragmentDrawer` plus drawing helpers (`SkiaFontCache`, `BorderShapeDrawer`, `ImageRenderer`).
  - `BorderShapeDrawer` for custom independent border rendering via SkiaSharp.
- **Inputs and outputs**: Consumes fragment pages with `PdfOptions`; produces PDF byte arrays and structured logs.
- **Extension guidance**:
  1. Treat fragments as read-only facts. If data is missing, fix the layout stage.
  2. Use `LoggerMessage` helpers for new diagnostics to avoid allocation churn.
  3. To add a different renderer, create a sibling project that consumes fragments and leave this assembly focused on SkiaSharp PDF output.

### Html2x (Facade)

- **Role**: Provide the developer facing `HtmlConverter` API that wires layout, rendering, and diagnostics.
- **Inputs and outputs**: Accepts HTML and `PdfOptions`, returns PDF bytes. 
- **Extension guidance**: Keep this layer thin; add configuration knobs or logging hooks but redirect substantive behavior to the pipeline projects.

### Test Projects

- **Html2x.LayoutEngine.Test**: Unit and golden tests that guard DOM parsing, style computation, box construction, and fragmentation. Extend these fixtures when introducing new layout logic.
- **Html2x.Renderers.Pdf.Test**: Renderer tests that validate `PdfRenderer`, assert PDF structure, and support fragment recording.
- **Html2x.Test**: End-to-end coverage through `HtmlConverter`, piping logs into `TestOutputLoggerProvider` for diagnostics.
- **Html2x.TestConsole**: Manual harness for exploratory runs. Keep dependencies light and rely on console logging for visibility.

## Extension Guides

- **Adding CSS support**: See `docs/extending-css.md` for a step-by-step walkthrough of propagating new properties through the pipeline.
- **Building new renderers**: See `docs/extending-renderers.md` for factory patterns, logging expectations, and testing guidance.
- **Roadmap & documentation backlog**: `docs/plan.md` tracks upcoming guides and feature enablement tasks; update it when new work begins.

## Error Handling & Guarantees

- **Parsing resilience**: DOM and CSS parsing ignore unsupported tags or properties and log at `Warning` when logging is enabled. Failures that block parsing (like unreadable streams) bubble as exceptions so callers can surface meaningful errors.
- **Deterministic layout**: Every pipeline stage is pure. Given the same HTML, options, and supporting services, the resulting fragment tree is bit-for-bit identical. Cache or snapshot outputs when diagnosing regressions.
- **Renderer safety**: `PdfRenderer` wraps SkiaSharp calls, logs at `Error`, and rethrows so hosts can decide whether to retry or surface failures. Temporary resources are confined to local streams and disposed immediately.
- **Thread isolation**: The library avoids static mutable state. You can create multiple converters in parallel as long as upstream dependencies (fonts, file access) are thread safe.

## Design Principles

1. **Fragments are the contract**: Every renderer consumes fragments only. If you find yourself needing DOM or style nodes in a renderer, revisit the layout stage and extend the fragment model instead.
2. **Explicit stages**: Parsing, styling, box building, and rendering remain separate to keep reasoning simple and testing cheap. Do not blend responsibilities across stages.
3. **Configuration over convention**: New behaviors should be toggled through options or injected services, not hidden switches. This keeps the public API predictable.
4. **Cross-platform focus**: Stay with managed code paths (SkiaSharp, AngleSharp). If a feature requires native interop, isolate it behind interfaces to preserve portability.
5. **Observable by default**: When adding complex logic, add structured logging hooks at the same time so future debugging is easier.
## Diagnostics Logging

Diagnostics can be switched on per call by setting `HtmlConverterOptions.Diagnostics.EnableDiagnostics = true`. When enabled, `HtmlConverter` builds a `DiagnosticsSession` that collects time-stamped events plus payloads describing what happened.

- Event flow: `StartStage/LayoutBuild` (stores trimmed HTML), `EndStage/LayoutBuild` (stores a `layout.snapshot` built by `LayoutSnapshotMapper`), `StartStage/PdfRender`, `EndStage/PdfRender` (stores a `render.summary` with `pageCount` and `pdfSize` in bytes).
- Payload contracts: `HtmlPayload`, `LayoutSnapshotPayload` (pages, margins, fragment rectangles and text), and `RenderSummaryPayload` implement `IDiagnosticsPayload` with a `Kind` marker so serializers can discriminate.
- JSON export: `Html2x.Diagnostics.DiagnosticsSessionSerializer.ToJson(session)` produces indented, camelCase JSON with relaxed escaping. The TestConsole accepts `--diagnostics` and `--diagnostics-json <path>` or you can run `src/Tests/Html2x.TestConsole/diagnostics/run-diagnostics-json.ps1` to render a PDF and write `build/diagnostics/session.json`.
- Consumption: `Html2PdfResult.Diagnostics` returns the session so hosts can persist it, diff snapshots between runs, or feed it into visualization tools.
- Extensibility: emit new payloads by implementing `IDiagnosticsPayload` and adding a mapper entry in `DiagnosticsSessionSerializer`. Stage-level markers can be added in pipeline stages by passing the `DiagnosticsSession` through.

## Testing Strategy

| Layer | Purpose | Typical Fixtures | Tooling Notes |
| --- | --- | --- | --- |
| Core units | Validate geometry math, color conversions, style defaults, and fragment invariants. | Inline data driven tests covering edge cases (zero widths, negative margins, etc.). | Use xUnit `[Theory]` to document edge scenarios. |
| Layout stage | Guard DOM parsing, cascade, and pagination logic. | HTML snippets in `Html2x.LayoutEngine.Test` with approved fragment snapshots or targeted assertions. | Favor deterministic snapshots; when behavior changes intentionally, update fixtures with rationale. |
| Renderer stage | Ensure SkiaSharp drawing remains consistent. | Synthetic fragment trees rendered to PDF; assertions via `PdfValidator` and `PdfWordParser`. | Prefer geometry/text extraction assertions over binary diffing. |
| Integration | Exercise the full `HtmlConverter` pipeline. | Realistic documents rendered during tests with PDF header and metadata checks. | Plug in `TestOutputLoggerProvider` to capture diagnostics inside the test output. |
| Manual harness | Provide fast validation during development. | `Html2x.TestConsole` sample HTML files. | Point to local or temporary files; review console logs for layout traces. |

**When adding features**
1. Start with core or layout tests that express the new behavior.
2. Add renderer assertions only if fragment output changes do not fully capture the intent.
3. Update integration coverage once the end-to-end result stabilizes.
4. Record PDF artifacts sparingly and document the reason in test comments.


