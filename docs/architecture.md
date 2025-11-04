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

## Solution Layout

```
/src
  Html2x.Core      # core primitives (styles, boxes, fragments, pages, fonts, units)
  Html2x.Layout    # DOM→Style→Box→Fragment pipeline
  Html2x.Pdf       # QuestPDF-based renderer (Fragment→PDF)
  Html2x.Layout.Tests
  Html2x.Pdf.Tests
```

## Projects & Responsibilities

### Html2x.Core

Minimal, renderer-agnostic primitives.

* **Fragments (render-time model)**

  * `Fragment` (base), `BlockFragment`, `LineBoxFragment`, `TextRun`, `ImageFragment`
  * `LayoutPage`, `LayoutMetadata` (page size, margins, page number, etc.)
* **Box/Style**

  * `Box` (layout box with margins/borders/padding, inline/block flags)
  * `ComputedStyle` (resolved CSS subset)
* **Common Services (interfaces)**

  * `IFontResolver`, `IImageProvider`, `IUnitConverter`, `ILogger`
* **Units & Geometry**

  * `Length`, `Edges`, `Rect`, `Color`

> **Contract:** Renderers consume **Fragment Trees**; they never touch DOM/CSS directly.

### Html2x.Layout

DOM/CSS → Fragment Tree.

* **Parsing**: AngleSharp DOM + CSSOM
* **Styling**: `StyleComputer` → `ComputedStyle` per element (cascading + inheritance)
* **Layout**: `LayoutBuilder` builds Box Tree (block/inline), then paginates to **Fragments**
* **Pagination**: simple rules + page breaks from CSS subset (`page-break-before/after`, `break-inside`)
* **Outputs**: `LayoutDocument` { `Pages: IReadOnlyList<LayoutPage>` }

Key types:

* `LayoutBuilder` (facade): `Build(html, options)` → `LayoutDocument`
* `LayoutOptions`: page size/orientation, margins, fonts, DPI, image provider

### Html2x.Pdf

Fragment Tree → PDF (QuestPDF).

* `PdfRenderer`:

  * `Render(LayoutDocument, PdfOptions, Stream output)`
* `PdfOptions`: metadata, compression, font fallbacks, grayscale toggle

> **Replaceable:** new renderers implement `IRenderer.Render(LayoutDocument, …)`.

## Supported CSS (MVP)

Focused subset for reports; anything outside is ignored.

* **Typography:** `font-family`, `font-size`, `font-weight`, `font-style`, `line-height`, `color`, `text-align`, `white-space` (normal/pre)
* **Box Model:** `display` (block/inline), `margin/padding/border`, `width/height`, `max-*`, `background-color`
* **Lists:** `list-style-type` (decimal/disc), nested lists
* **Tables (MVP):** block layout (simple rows/cols, no complex spanning in v1)
* **Paged Media:** `page-break-before/after`, `break-inside: avoid`, page size via options

*Not supported (by design):* JS, media queries, transforms/filters, advanced typography (ligatures shaping beyond .NET fonts), color profiles, accessibility tagging (PDF/UA), forms.

## Data Flow (Short)

1. **Parse** (AngleSharp) → DOM + CSSOM
2. **Compute Styles** → `ComputedStyle` per node (cascade, inherit, defaults)
3. **Boxing & Line Layout** → Box Tree with block/inline formatting contexts
4. **Fragmentation** → `LayoutDocument` with `LayoutPage` → `Fragment` hierarchy
5. **Render** → `IRenderer` (PDF via QuestPDF in `Html2x.Pdf`)

## Extensibility Points

* **Fonts:** plug custom `IFontResolver` (system fonts, embedded, or memory streams)
* **Images:** plug `IImageProvider` (disk, HTTP, base64, cache)
* **Units:** swap `IUnitConverter` for DPI/calibration
* **Renderer:** implement `IRenderer` (e.g., `Html2x.Svg`, `Html2x.Canvas`) using fragments
* **Diagnostics:** inject `ILoggerFactory` to surface layout/renderer traces

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

* **Unit (Core):** box math, edges, lengths, style defaults, line breaking cases
* **Layout (Golden):** HTML fixtures → serialized Fragment snapshots (approve tests)
* **Renderer (Integration):** fragment to PDF smoke (page count, metadata), pixel diff on rasterized pages (tolerant)

## Error Handling & Guarantees

* **Fail-soft parsing:** unknown CSS/HTML → ignored (no throw)
* **Deterministic layout:** same inputs → same fragments
* **Thread-safe rendering:** render calls are independent (no static mutable state)

## Design Principles

* **Single Source of Truth:** All renderers consume **Fragments** only.
* **Pure Stages:** Parsing, Styling, Layout, Rendering are side-effect free and testable.
* **Minimal Surface:** Start small; extend CSS/HTML support by need, behind clear interfaces.
* **Cross-Platform:** no platform-specific graphics APIs.
