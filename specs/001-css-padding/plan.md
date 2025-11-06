# Implementation Plan: CSS Padding Support

**Branch**: `001-css-padding` | **Date**: 2025-11-06 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/001-css-padding/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Add CSS padding support to Html2x, enabling developers to apply padding to block and inline elements using individual properties (`padding-top`, `padding-right`, `padding-bottom`, `padding-left`) and shorthand (`padding`). Padding values flow through the staged layout pipeline (style computation → box model → fragments) following the established margin pattern. Only `px` units are supported in this iteration, with design extensibility for future table cell support.

## Technical Context

**Language/Version**: .NET 8  
**Primary Dependencies**: AngleSharp (CSS parsing), QuestPDF (rendering), Html2x shared libraries (`Html2x.Core`, `Html2x.Layout`)  
**Storage**: In-memory (padding values stored in `ComputedStyle` and box models)  
**Testing**: xUnit via `dotnet test Html2x.sln -c Release`  
**Target Platform**: Windows (primary development), Linux (assumed compatible via .NET Core)  
**Project Type**: Modular library (`src/Html2x.*`) with test console harness  
**Performance Goals**: Preserve deterministic fragment generation; padding parsing adds minimal overhead (single CSS property lookup per element)  
**Constraints**: Pure managed code; follow existing margin implementation pattern; maintain stage isolation; px-only units (no em/rem/% support)  
**Scale/Scope**: Impacts `Html2x.Layout` (style computation, box model), `Html2x.Core` (if `ComputedStyle` needs extension), `Html2x.Layout.Test` (unit tests), `Html2x.Pdf.Test` (renderer tests). No changes to `Html2x.Pdf` renderer expected (padding affects layout, not rendering directly).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] Stage isolation maintained (Principle I: Staged Layout Discipline). Padding values flow through style computation → box model → fragments without bypassing stages. No direct DOM access from layout/rendering stages.
- [x] Deterministic rendering risks addressed with tests or instrumentation (Principle II). Padding parsing uses same deterministic conversion logic as margins (px → pt). Tests verify identical inputs produce identical fragment geometry.
- [x] TDD approach defined, including failing tests to introduce (Principle III). Three user stories each have independent failing test defined: `CssStyleComputerTests` for parsing, `BoxTreeBuilderTests`/`LayoutIntegrationTests` for layout impact.
- [x] Logging and diagnostics updates planned (Principle IV). Structured logging for invalid padding values, unsupported units, with element context. Logs traceable via `Html2x.Pdf.TestConsole`.
- [x] Extension points documented with migration guidance (Principle V). Design extensible for table cell support. Documentation updates in `docs/extending-css.md` planned.

## Project Structure

### Documentation (this feature)

```
specs/001-css-padding/
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
        Layout/
            (ComputedStyle may need extension - verify location)
    Html2x.Layout/
        Style/
            CssStyleComputer.cs (extend)
            StyleModels.cs (extend ComputedStyle)
            HtmlCssConstants.cs (add padding constants)
            CssValueConverter.cs (reuse existing px→pt conversion)
        Box/
            BoxTreeBuilder.cs (extend to propagate padding)
            BlockLayoutEngine.cs (extend to apply padding to content area)
tests/
    Html2x.Layout.Test/
        CssStyleComputerTests.cs (add padding parsing tests)
        BoxTreeBuilderTests.cs (add padding layout tests)
        LayoutIntegrationTests.cs (add end-to-end padding tests)
    Html2x.Pdf.Test/
        (no changes expected - padding affects layout, not rendering)
src/Html2x.Pdf.TestConsole/
    (manual smoke testing)
```

**Structure Decision**: Padding follows the margin implementation pattern. No new projects or major structural changes. Extensions limited to:
- `HtmlCssConstants.cs`: Add padding property constants
- `ComputedStyle` (in `Html2x.Layout.Style.StyleModels.cs`): Add `PaddingTopPt`, `PaddingRightPt`, `PaddingBottomPt`, `PaddingLeftPt` properties
- `CssStyleComputer.cs`: Add padding parsing logic (individual + shorthand)
- `BoxTreeBuilder.cs` / `BlockLayoutEngine.cs`: Apply padding to content area calculations
- Test files: Add padding-specific test cases

## Complexity Tracking

> Fill ONLY if Constitution Check has violations that must be justified.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|---------------------------------------|
| (none) | | |

## Phase 0: Research (Complete)

**Status**: ✅ Complete  
**Artifact**: `research.md`

All research objectives resolved:
- CSS padding shorthand parsing rules confirmed (4 forms)
- Padding inheritance behavior verified (non-inheritable, defaults to 0)
- Box model interaction understood (content-box, padding inside border)
- Unit conversion confirmed (reuse existing px→pt conversion)
- Implementation pattern established (follow margin pattern)

## Phase 1: Design & Contracts (Complete)

**Status**: ✅ Complete  
**Artifacts**: 
- `data-model.md` - Entity extensions and data flow documented
- `quickstart.md` - Step-by-step implementation guide
- Agent context updated (`.cursor/rules/specify-rules.mdc`)

**Design Decisions**:
- Reuse `Spacing` class for padding (consistency with margin)
- Extend `ComputedStyle` with four padding properties (matches margin pattern)
- Parse shorthand in `CssStyleComputer` (similar to `ApplyPageMargins` pattern)
- Apply padding in `BlockLayoutEngine` to reduce content area

**Contracts**: N/A - This is a CSS property extension, not an API contract change. Internal pipeline contracts remain unchanged.

## Phase 2: Task Breakdown

**Status**: Pending - To be generated via `/speckit.tasks`

**Next Steps**:
1. Generate task breakdown from user stories
2. Assign priorities (P1, P2, P3)
3. Define test-first implementation steps
4. Include logging and documentation tasks
