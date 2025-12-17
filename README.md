# Html2x

A modern, cross-platform .NET library for converting **HTML + CSS** into multiple output formats.
The **primary** target is **PDF**, with an architecture designed to extend to **SVG, Canvas**, and more.

---

## Goals

* **Reusable & modular**: clean separation of parsing and rendering.
* **Cross-platform**: pure .NET (Windows/Linux; no GDI+, no headless Chromium).
* **Testable & maintainable**: clear boundaries, predictable outputs captured via diagnostics, unit/integration test friendly.
* **Business-report ready**: supports a practical subset of HTML/CSS commonly used in reporting.

---

## Packages & Modules

* **`Html2x.Abstractions`** - Contracts, diagnostics payloads, and shared utilities consumed by every engine and renderer.
* **`Html2x.LayoutEngine`** - Parses HTML/CSS (via AngleSharp), builds the style/box trees, and emits predictable fragments with diagnostics hooks.
* **`Html2x.Renderers.Pdf`** - Consumes fragments and renders PDFs using SkiaSharp; owns renderer-side diagnostics.
* **`Html2x`** - Composition facade for embedding scenarios; wires abstractions, layout, and renderers together.

Additional renderers (for example `Html2x.Renderers.Svg`) can plug into the same abstractions without touching the layout engine.

---

## High-Level Pipeline

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
Renderer (PDF via SkiaSharp; future: SVG/Canvas)
```

* **Display List** = a stable, device-independent set of drawing operations (text runs, paths, fills, borders, images, page breaks, etc.).
* This abstraction lets us add new output formats with minimal duplication.

---

## Scope & Non-Goals

**Supported (initially)**

* Static HTML and CSS (no scripting).
* Common layout for business reports: block/inline flow, lists, tables, page size/margins, page breaks.
* Basic text, fonts, colors, borders, backgrounds, images.

**Not in scope (by design)**

* JavaScript / dynamic DOM.
* Color profiles, accessibility tags, form fields, media queries beyond basics.
* Complex CSS features at first (e.g., transforms, filters, grid/flex — phased in later).

---

## Installation

> NuGet packages will be published under the `Html2x.*` namespace.

```powershell
dotnet add package Html2x.LayoutEngine
dotnet add package Html2x.Renderers.Pdf
```

---

## Quick Start

TBD

---

## Design Principles

* **Separation of concerns**: `Html2x.LayoutEngine` owns HTML/CSS parsing and layout; `Html2x.Renderers.Pdf` only draws.
* **Extensibility**: new renderers (SVG, Canvas, etc.) bind to `Html2x.Abstractions` without parser changes.
* **Predictable outputs**: inputs plus options produce a stable fragment tree; diagnostics highlight any platform-specific differences without parsing PDFs.
* **Pure .NET**: no native GUI stacks or browsers required.

---

## Versioning & Targets

* **.NET**: `net8.0`
* **OS**: Windows & Linux
* **Build**: distributed as **NuGet packages**

---

## Repository Layout

```
src/
  Html2x/                     (composition + public surface)
  Html2x.Abstractions/        (contracts, diagnostics, shared utilities)
  Html2x.LayoutEngine/        (style traversal, box + fragment builders)
  Html2x.Renderers.Pdf/       (SkiaSharp PDF pipeline, drawing, font resolution)
  Tests/
    Html2x.LayoutEngine.Test/
    Html2x.Renderers.Pdf.Test/
    Html2x.Test/              (integration glue + harness helpers)
    Html2x.TestConsole/       (manual harness, sample html/fonts)
build/
  width-height/               (generated artifacts, logs)
docs/                         (architecture, standards, testing)
```

---

## Documentation

* [Architecture](docs/architecture.md) - Project structure and design
* [Coding Standards](docs/coding-standards.md) - Code quality guidelines
* [Testing Guidelines](docs/testing-guidelines.md) - Testing approach and standards

---

## License

MIT License 

