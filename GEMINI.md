# Html2x Context for Gemini

## Project Overview

**Html2x** is a modern, cross-platform .NET 8 library for converting **HTML + CSS** into **PDF** (primary target) and other formats. It features a modular architecture that cleanly separates HTML parsing/layout from the final rendering step, allowing for deterministic outputs and extensibility.

**Key Technologies:**
*   **Language:** C# (.NET 8.0)
*   **Parsing:** AngleSharp (DOM & CSSOM)
*   **PDF Rendering:** SkiaSharp
*   **Testing:** xUnit

## Workflows & Constraints

*   **Ignored Paths:** Do not use or update `specs/` or `.cursor/` directories. Ignore any "SpecKit" workflows found therein.
*   **Git Operations:** **Never** run `git` commands to stage or commit changes. The user handles source control manually.
*   **Testing Strategy:**
    *   **Primary:** Write comprehensive **Unit Tests** (xUnit) for all logic changes.
    *   **Rendering Verification:** **Do not parse generated PDFs.** Use the **Diagnostics Framework**:
        1. Enable `HtmlConverterOptions.Diagnostics`.
        2. Inspect the `LayoutSnapshotPayload` in `Html2PdfResult.Diagnostics` to verify the fragment tree (geometry, text, layout) programmatically.
    *   **UAT:** Use `src/Tests/Html2x.TestConsole` for User Acceptance Testing and visual verification.
    *   **Samples:** Reference `src/Tests/Html2x.TestConsole/html/` for standard HTML samples to test against.

## Architecture

The solution follows a strict pipeline architecture:

1.  **Parsing:** `Html2x.LayoutEngine` uses **AngleSharp** to parse HTML/CSS into a DOM.
2.  **Style:** `CssStyleComputer` resolves the cascade to produce a **Style Tree**.
3.  **Layout:** `BoxTreeBuilder` creates a **Box Tree** (block/inline formatting contexts).
4.  **Pagination:** `FragmentBuilder` converts boxes into a **Fragment Tree** (pages, lines, text runs).
5.  **Rendering:** `Html2x.Renderers.Pdf` consumes the Fragment Tree to draw the PDF using **SkiaSharp**.

**Project Structure:**
*   `src/Html2x`: Public facade/composition root.
*   `src/Html2x.Abstractions`: Core contracts, shared types (Fragments, Styles, Diagnostics). **Zero dependencies.**
*   `src/Html2x.LayoutEngine`: The core layout pipeline (HTML -> Fragments).
*   `src/Html2x.Renderers.Pdf`: PDF implementation using SkiaSharp.
*   `src/Html2x.Diagnostics`: Structured logging and diagnostics models.
*   `src/Tests/*`: Unit, Integration, and Console harness projects.

## Development Workflow

### Build
```powershell
dotnet build src/Html2x.sln -c Release
```

### Test
Use the helper script for convenience:
```powershell
# Run all tests
./scripts/test.ps1 all

# Run specific suites
./scripts/test.ps1 layout
./scripts/test.ps1 pdf
./scripts/test.ps1 integration
```
Or standard dotnet commands:
```powershell
dotnet test src/Html2x.sln -c Release
```

### Manual Verification (Console App)
For ad-hoc testing with visual output:
```powershell
dotnet run --project src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj -- --input src/Tests/Html2x.TestConsole/html/basic.html --output build/output.pdf
```

## Coding Conventions

*   **Style:** Standard C# conventions. 4-space indentation.
*   **Immutability:** Prefer immutable records for data models (Fragments, Styles).
*   **Separation of Concerns:**
    *   **LayoutEngine** should *never* know about SkiaSharp.
    *   **Renderers** should *never* touch the DOM/AngleSharp.
    *   **Abstractions** has *no* external dependencies.
*   **Logging:** Use structured logging (`LoggerMessage.Define`). High-volume logs (layout traces) should be guarded by `IsEnabled` checks.

## Extension Points

*   **New CSS Properties:**
    1.  Update `CssStyleComputer` in `LayoutEngine`.
    2.  Add properties to `ComputedStyle` (in `Abstractions`).
    3.  Update `BoxTreeBuilder` if it affects layout geometry.
*   **New Renderers:** Implement `IFragmentRenderer` in a new project. Consume `HtmlLayout` (Fragments) and output the target format.

## Documentation
*   `docs/architecture.md`: Detailed system design.
*   `docs/coding-standards.md`: Code style and patterns.
*   `docs/testing-guidelines.md`: How to write effective tests.
