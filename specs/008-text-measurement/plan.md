# Implementation Plan: Font Accurate Text Measurement

**Branch**: `008-text-measurement` | **Date**: December 27, 2025 | **Spec**: `C:\Projects\html2x\specs\008-text-measurement\spec.md`
**Input**: Feature specification from `/specs/008-text-measurement/spec.md`

## Summary

Deliver font-accurate text measurement and wrapping using abstract measurement and font source contracts so layout decisions are correct, diagnosable, and testable without real fonts. The plan adds abstractions in Html2x.Abstractions, a concrete measurer in composition or renderer-adjacent code, layout wiring, wrapping utility, diagnostics, and tests.

## Technical Context

**Language/Version**: C# / .NET 8
**Primary Dependencies**: SkiaSharp, SkiaSharp.HarfBuzz, QuestPDF (existing), Html2x.Diagnostics
**Storage**: N/A (in-memory data, font files on disk)
**Testing**: xUnit with existing Html2x test projects
**Target Platform**: Cross-platform .NET (Windows and Linux baseline)
**Project Type**: single library solution
**Performance Goals**: No measurable regression vs current layout on representative documents
**Constraints**: No new renderers or output formats; layout must only depend on abstractions
**Scale/Scope**: Library feature used by Html2x consumers; no UI or web API surface

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **[I. Staged Layout]** Does the plan respect the Abstractions -> Layout -> Renderers dependency flow?
- [x] **[II. Predictability]** Are sources of nondeterminism isolated?
- [x] **[III. Test-First]** Does the plan prioritize behavior-focused tests over implementation details?
- [x] **[IV. Diagnostics]** Is `Html2x.Diagnostics` instrumentation included?
- [x] **[V. Extensibility]** Are new extension points documented in `docs/`?
- [x] **[VI. Goal-Driven]** Are state transitions and assumptions explicitly tracked?
- [x] **[VII. Accessible Docs]** Is the plan written in plain English with code sketches for complex logic?
- [x] **[Delivery]** Does the plan include a task to create a new HTML sample in `src/Tests/Html2x.TestConsole/html/`?

## Project Structure

### Documentation (this feature)

```text
specs/008-text-measurement/
    plan.md              # This file (/speckit.plan command output)
    research.md          # Phase 0 output (/speckit.plan command)
    data-model.md        # Phase 1 output (/speckit.plan command)
    quickstart.md        # Phase 1 output (/speckit.plan command)
    contracts/           # Phase 1 output (/speckit.plan command)
    tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
    Html2x/                     # Composition layer
    Html2x.Abstractions/         # Contracts, diagnostics payloads, utilities
    Html2x.LayoutEngine/         # Layout logic, wrapping
    Html2x.Renderers.Pdf/        # Rendering
    Html2x.Diagnostics/          # Diagnostics framework

src/Tests/
    Html2x.LayoutEngine.Test/
    Html2x.Renderers.Pdf.Test/
    Html2x.Test/
    Html2x.TestConsole/
```

**Structure Decision**: Single solution with existing module layout. New contracts go to Html2x.Abstractions and layout logic stays in Html2x.LayoutEngine. Concrete measurer lives in Html2x (composition) or renderer-adjacent code and is injected via composition.

## Goal-Driven Delivery Loop

### State Assessment
- Current: layout uses estimators and heuristics; font resolution is not strict
- Target: layout uses font-accurate measurement via abstractions; early failure on missing fonts; predictable wrapping
- Gap: missing measurement and font source contracts, measurer implementation, integration wiring, diagnostics, tests

### Action Decomposition
1. Define abstraction contracts and supporting entities
2. Implement concrete measurer and font resolution
3. Wire layout services and wrapping logic
4. Add diagnostics and tests
5. Update docs and sample HTML

### Path Planning
- Choose lowest-risk integration: introduce contracts first, adapt layout to depend only on abstractions, then implement measurer and wiring
- Contingencies: if font resolution fails in test console, add diagnostics and document expected font folder usage

### Adaptive Execution
- Validate each step with a single failing test followed by minimal implementation
- Adjust wrapping logic and baseline math based on diagnostics outputs

### Reflection Loop
- Record what changed measurement accuracy and where diagnostics helped
- Capture any recurring edge cases for future font shaping work

## Phase 0: Outline and Research

### Research Tasks
- Decision: Where to host the concrete measurer (composition layer vs renderer-adjacent) and how to keep LayoutEngine isolated
- Decision: Minimum diagnostic payloads needed to debug font resolution and measurement failures
- Decision: Wrapping behavior rules and their test vectors

### Phase 0 Output
- `research.md` with resolved decisions, rationale, and alternatives

## Phase 1: Design and Contracts

### Data Model
- Derive entities and attributes from the spec into `data-model.md`

### Contracts
- Create internal contract specs in `contracts/` (markdown or C# interface sketches)
- Note: OpenAPI/GraphQL is not applicable because Html2x has no web API surface

### Code Sketches (for accessibility)

```text
ITextMeasurer
  MeasureWidth(font, size, text) -> width in points
  GetMetrics(font, size) -> ascent, descent in points

IFontSource
  Resolve(requested font key) -> resolved font descriptor

LayoutServices
  TextMeasurer + FontSource (if not encapsulated)
```

### Agent Context Update
- Run update agent context script after contracts and data model are written

## Phase 1 Outputs
- `data-model.md`
- `contracts/`
- `quickstart.md`
- Updated agent context

## Constitution Re-check (post-design)

- [x] **[I. Staged Layout]** Abstractions isolate layout from renderers
- [x] **[II. Predictability]** Diagnostics capture font resolution and measurements
- [x] **[III. Test-First]** Tests lead implementation steps
- [x] **[IV. Diagnostics]** New diagnostics are planned
- [x] **[V. Extensibility]** Documentation updates included
- [x] **[VI. Goal-Driven]** State transitions and assumptions captured
- [x] **[VII. Accessible Docs]** Code sketches included
- [x] **[Delivery]** HTML sample required and planned

## Phase 2: Planning (tasks)

- Phase 2 tasks are produced by `/speckit.tasks` after this plan is approved

## Complexity Tracking

None.
