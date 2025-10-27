# Html2x

A modern, cross-platform .NET library for converting **HTML + CSS** into multiple output formats.
The **primary** target is **PDF**, with an architecture designed to extend to **SVG, Canvas**, and more.

---

## Goals

* **Reusable & modular**: clean separation of parsing and rendering.
* **Cross-platform**: pure .NET (Windows/Linux; no GDI+, no headless Chromium).
* **Testable & maintainable**: clear boundaries, deterministic outputs, unit/integration test friendly.
* **Business-report ready**: supports a practical subset of HTML/CSS commonly used in reporting.

---

## Packages

* **`Html2x.Layout`** — Parses HTML/CSS and produces a **Fragment Tree**.
* **`Html2x.Pdf`** — Consumes a Fragment Tree and renders a **PDF** file.

> Additional renderers (e.g., `Html2x.Svg`, `Html2x.Canvas`) can be added later without changing the parser.

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
Renderer (PDF via QuestPDF; future: SVG/Canvas)
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
dotnet add package Html2x.Layout
dotnet add package Html2x.Pdf
```

---

## Quick Start

TBD

---

## Design Principles

* **Separation of concerns**: `Html2x.Layout` knows HTML/CSS and layout; converts it to Fragment tree.
* **Extensibility**: add new renderers (SVG/Canvas) without touching the parser.
* **Deterministic outputs**: inputs + options → stable Fragment Tree → identical PDF across platforms.
* **Pure .NET**: no native GUI stacks or browsers required.

---

## Versioning & Targets

* **.NET**: `net8.0`
* **OS**: Windows & Linux
* **Build**: distributed as **NuGet packages**

---

## Documentation

* [Architecture](docs/architecture.md) - Project structure and design
* [Coding Standards](docs/coding-standards.md) - Code quality guidelines
* [Testing Guidelines](docs/testing-guidelines.md) - Testing approach and standards

---

## License

MIT License 
