# Implementation Plan: CSS Height and Width Support

**Branch**: `001-css-dimension-support` | **Date**: November 10, 2025 | **Spec**: `specs/001-css-dimension-support/spec.md`  
**Input**: Feature specification from `/specs/001-css-dimension-support/spec.md`

## Summary

Enable deterministic handling of CSS `width` and `height` declarations for block-level containers and bordered placeholders by extending the style-to-box pipeline, restricting supported units to px/pt/percent, logging every resolution/fallback, and guarding the behavior with layout+PDF regression fixtures plus negative validation tests.

## Technical Context

**Language/Version**: .NET 8 (`net8.0` across Html2x assemblies)  
**Primary Dependencies**: AngleSharp for CSS parsing, Html2x.Abstractions contracts, Html2x.LayoutEngine fragment builders, Html2x.Renderers.Pdf pipeline, QuestPDF output harness  
**Storage**: In-memory only; width/height metadata live on transient Requested/Resolved Dimension records plus FragmentDimension objects  
**Testing**: xUnit suites per module plus Html2x.TestConsole regression captures on the supported Windows runner (per SC-001); new tests live in `src/Tests/Html2x.LayoutEngine.Test`, `src/Tests/Html2x.Renderers.Pdf.Test`, and a console harness sample run scripted in `build/`
**Target Platform**: Windows runners via `dotnet test Html2x.sln -c Release` and console smoke tests  
**Project Type**: Modular library with shared contracts and PDF renderer; no services introduced  
**Performance Goals**: Maintain deterministic fragment sizing within ±1pt tolerance and avoid extra layout passes beyond a single retry for percentage convergence  
**Constraints**: Managed code only, no `<img>` sizing implementation, supported units limited to px/pt/% with warnings for others, pipeline stage isolation must remain intact  
**Scale/Scope**: Touches `Html2x.Abstractions` (dimension contracts), `Html2x.LayoutEngine` (style resolution + box builder), `Html2x.Renderers.Pdf` (fragment consumption), plus tests/console assets; expected to handle templates with hundreds of bordered blocks per page without throughput regression

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] Stage isolation maintained (Principle I: Staged Layout Discipline).  
- [x] Deterministic rendering risks addressed with tests or instrumentation (Principle II).  
- [x] TDD approach defined, enforcing the one-failing-test-at-a-time loop from Principle III.  
- [x] Logging and diagnostics updates planned (Principle IV).  
- [x] Extension points documented with migration guidance (Principle V).

_Constitution note_: Html2x.Abstractions is the renamed Html2x.Core boundary after the November 2025 refactor; all DOM→style→box contracts still live there, keeping Principle I intact.

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

The November 2025 structure refactor consolidated legacy `Html2x.Core/Layout/Pdf` projects into the current `Html2x.*` assemblies and relocated every test harness under `src/Tests/` so feature docs must reference the new paths.

```
src/
    Html2x/                     (composition + public API surface)
    Html2x.Abstractions/        (shared contracts + diagnostics)
    Html2x.LayoutEngine/        (style pipeline, box + fragment builders)
    Html2x.Renderers.Pdf/       (QuestPDF renderer + visitors)
    Tests/
        Html2x.LayoutEngine.Test/
        Html2x.Renderers.Pdf.Test/
        Html2x.Test/            (shared harness + integration glue)
        Html2x.TestConsole/     (manual harness + fixtures)
build/
    width-height/               (spec evidence + generated PDFs)
```

**Structure Decision**: Extend `Html2x.Abstractions` with unit source metadata, wire `Html2x.LayoutEngine` style resolvers and box builders to honor px/pt/% widths and heights, propagate metrics unchanged into `Html2x.Renderers.Pdf`, and seed new regression assets under `src/Tests/Html2x.TestConsole/html/width-height/`.

## Complexity Tracking

> Fill ONLY if Constitution Check has violations that must be justified.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|---------------------------------------|
| *(none)* | | |

## Implementation Strategy

1. **Phase 1-2 (Setup + Foundational)**: Keep the tree green while enabling the new contracts and validators that US1-US3 depend on. Introduce the `DimensionStyleMapper` helper plus locale-aware parsing (T007A/T008A) so Requested/Resolved dimension records flow through a single stage-aware entry point before US1 work begins.  
2. **Phase 3 (US1 single-test loops)**:  
   - Loop 1: T009 -> T011/T012
   - Loop 2: T009A -> T011A
   - Loop 3: T009B -> T011B
   - Loop 4: T010 -> T013
   - Loop 5: T010A -> T013A

   Run diagnostics (T014) and refresh the console fixture (T015) once all loops pass.  
3. **Phase 4 (US2 loops)**: Execute T016 -> T018, then T017 -> T019, wiring diagnostics with T020 and fixtures with T021.  
4. **Phase 5 (US3 loops)**: Execute T022 -> T024, then T023 -> T025, documenting remediation with T026.  
5. **Phase 6 (Polish)**: Capture full-suite evidence (T027–T029B) and verify the console smoke test (T030). Only one failing test may exist at any time throughout the plan.

## Validation & Evidence

- T028 regenerates deterministic PDFs and logs for every targeted test console scenario.  
- T029A scripts the five-minute diagnostics review and records the elapsed time in `build/logs/width-height/triage.json`.  
- T029B stores untouched Html2x.TestConsole outputs under `build/width-height/` and references them in docs/release notes to satisfy SC-004.  
- T030 confirms the Html2x.TestConsole harness runs with the updated fixtures before merge.
