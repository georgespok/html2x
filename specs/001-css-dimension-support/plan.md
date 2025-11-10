# Implementation Plan: CSS Height and Width Support

**Branch**: `001-css-dimension-support` | **Date**: November 10, 2025 | **Spec**: `specs/001-css-dimension-support/spec.md`  
**Input**: Feature specification from `/specs/001-css-dimension-support/spec.md`

## Summary

Enable deterministic handling of CSS `width` and `height` declarations for block-level containers and bordered placeholders by extending the style-to-box pipeline, restricting supported units to px/pt/percent, logging every resolution/fallback, and guarding the behavior with layout+PDF regression fixtures plus negative validation tests.

## Technical Context

**Language/Version**: .NET 8 (`net8.0` across Html2x assemblies)  
**Primary Dependencies**: AngleSharp for CSS parsing, Html2x.Core contracts, Html2x.Layout fragment builder, Html2x.Pdf renderer, QuestPDF output harness  
**Storage**: In-memory only; width/height metadata live on transient Requested/Resolved Dimension records plus FragmentDimension objects  
**Testing**: xUnit suites per module plus Pdf.TestConsole regression captures; new tests live in `Html2x.Layout.Test`, `Html2x.Pdf.Test`, and a console harness sample run scripted in `build/`  
**Target Platform**: Windows and Linux runners via `dotnet test Html2x.sln -c Release` and console smoke tests  
**Project Type**: Modular library with shared contracts and PDF renderer; no services introduced  
**Performance Goals**: Maintain deterministic fragment sizing within ±1pt tolerance and avoid extra layout passes beyond a single retry for percentage convergence  
**Constraints**: Managed code only, no `<img>` sizing implementation, supported units limited to px/pt/% with warnings for others, pipeline stage isolation must remain intact  
**Scale/Scope**: Touches `Html2x.Core` (dimension contracts), `Html2x.Layout` (style resolution + box builder), `Html2x.Pdf` (fragment consumption), plus tests/console assets; expected to handle templates with hundreds of bordered blocks per page without throughput regression

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] Stage isolation maintained (Principle I: Staged Layout Discipline).  
- [x] Deterministic rendering risks addressed with tests or instrumentation (Principle II).  
- [x] TDD approach defined, including failing tests to introduce (Principle III).  
- [x] Logging and diagnostics updates planned (Principle IV).  
- [x] Extension points documented with migration guidance (Principle V).

## Project Structure

### Documentation (this feature)

```
specs/001-css-dimension-support/
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
    Html2x.Core/
    Html2x.Layout/
    Html2x.Pdf/
tests/
    Html2x.Layout.Test/
    Html2x.Pdf.Test/
    html2x.IntegrationTest/
src/Html2x.TestConsole/
```

**Structure Decision**: Extend `Html2x.Core` dimension records with unit source metadata, wire `Html2x.Layout` style resolvers and box builders to honor px/pt/% widths and heights, propagate metrics unchanged into `Html2x.Pdf`, and seed new regression assets under `src/Html2x.TestConsole/html/width-height/`.

## Complexity Tracking

> Fill ONLY if Constitution Check has violations that must be justified.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|---------------------------------------|
| *(none)* | | |
