# Implementation Plan: Basic HTML-to-PDF Essentials

**Branch**: `003-basic-html` | **Date**: 2025-11-17 | **Spec**: specs/003-basic-html/spec.md  
**Input**: Feature specification from `/specs/003-basic-html/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Deliver the foundational HTML-to-PDF surface by wiring predictable inline text (`<br>`), basic `<img>` sizing with aspect-ratio enforcement, per-side borders, and default display roles into the Html2x pipeline. Layout and renderer changes remain isolated to `Html2x.LayoutEngine` and `Html2x.Renderers.Pdf`, verified through diagnostics traces (no PDF parsing) and single-page fixtures.

## Technical Context

**Language/Version**: .NET 8  
**Primary Dependencies**: AngleSharp for DOM/CSS, QuestPDF for rendering, Html2x shared abstractions  
**Storage**: In-memory fragment trees only; no persistent assets beyond existing sample fonts/html  
**Testing**: xUnit via `dotnet test Html2x.sln -c Release` with diagnostics assertions instead of PDF diffing  
**Target Platform**: Windows and Linux build agents executing deterministic layouts  
**Project Type**: Modular libraries `src/Html2x.LayoutEngine`, `src/Html2x.Renderers.Pdf`, plus `src/Html2x.TestConsole` harness  
**Performance Goals**: Measure line-box generation and PDF rendering on `specs/003-basic-html/samples/basic.html`; keep runtime within ±5% of the current single-page baseline (record values in quickstart) and avoid increasing renderer allocations (track via diagnostics counters)
**Constraints**: Pure managed code; leverage `Html2x.Diagnostics` for validation; forbid remote image fetching (disk paths + data URIs only)  
**Scale/Scope**: Updates touch `Html2x.LayoutEngine` (style, box, fragment stages), `Html2x.Renderers.Pdf` (border + image visitors), diagnostics payloads, and tests under `Html2x.LayoutEngine.Test`, `Html2x.Renderers.Pdf.Test`, and `Html2x.Test`

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] Stage isolation maintained at assembly boundaries (Principle I: Staged Layout Discipline) and verified via dependency-audit/unit-test tasks (T007, T013, T022, T031).
- [x] Rendering predictability risks documented with `Html2x.Diagnostics` coverage instead of PDF parsing (Principle II).
- [x] TDD approach defined, explicitly sequencing one failing test at a time per Principle III.
- [x] `Html2x.Diagnostics` instrumentation scoped for new behavior (Principle IV).
- [x] Extension points documented with migration guidance (Principle V).
- [x] Goal-Driven Problem Solving loop captured (Principle VI).

## Project Structure

### Documentation (this feature)

```
specs/003-basic-html/
    plan.md
    research.md
    data-model.md
    quickstart.md
    contracts/
    tasks.md
```

### Source Code (repository root)

```
src/
    Html2x/
    Html2x.Abstractions/
    Html2x.LayoutEngine/
    Html2x.Diagnostics/
    Html2x.Renderers.Pdf/
tests/
    Html2x.LayoutEngine.Test/
    Html2x.Renderers.Pdf.Test/
    Html2x.Test/
src/Html2x.TestConsole/
```

**Structure Decision**: Layout work stays in `Html2x.LayoutEngine` (style tree, display role table, border model); renderer updates remain in `Html2x.Renderers.Pdf` (border drawing, image sizing). Diagnostics additions land in `Html2x.Abstractions` contracts plus console sample fixtures.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|---------------------------------------|
| *(none)* |  |  |

## Post-Design Constitution Check

- [x] Stage isolation plan still holds; only `Html2x.LayoutEngine` shares internal models while renderer consumes abstractions.
- [x] Diagnostics coverage defined for text, image, and border flows, keeping rendering predictability verifiable without PDFs.
- [x] Incremental TDD enforced through per-story failing tests captured in forthcoming tasks.
- [x] Instrumentation work scoped (diagnostic payloads + TestConsole toggles).
- [x] Extensibility documented via updated contracts and quickstart guidance.
- [x] Goal-Driven cadence reflected in research, data model, and quickstart deliverables.

