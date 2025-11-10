---
description: "Task list for CSS width and height feature delivery"
---

# Tasks: CSS Height and Width Support

**Input**: Design documents from `/specs/001-css-dimension-support/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/schema.md

**Tests**: Author failing tests before implementation per user story priorities. Promote deterministic layout evidence into Html2x.Layout.Test, Html2x.Pdf.Test, and Pdf.TestConsole harness artifacts.

**Organization**: Tasks are grouped by user story to maintain incremental, independently testable delivery.

## Format: `[ID] [P?] [Story] Description`

- **[P]** indicates the task can proceed in parallel because it touches isolated files.
- **[Story]** maps tasks to spec.md user stories (US1, US2, US3).
- Every task references the exact file path it modifies or validates.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prove the repo is healthy and capture baseline fixtures before touching contracts.

- [X] T001 Run `dotnet restore Html2x.sln` to ensure NuGet dependencies resolve for Html2x.sln.
- [X] T002 Execute `dotnet test Html2x.sln -c Release --filter "Html2x.Layout.Test&&Category=Dimensions"` to record baseline results for `tests/Html2x.Layout.Test`.
- [X] T003 Execute `dotnet test Html2x.sln -c Release --filter "Html2x.Pdf.Test&&Category=BorderedBlocks"` to confirm renderer baseline in `tests/Html2x.Pdf.Test`.
- [ ] T004 Capture a reference PDF and diagnostics log by running `src/Html2x.Pdf.TestConsole/Html2x.Pdf.TestConsole.csproj` against `src/Html2x.Pdf.TestConsole/html/width-height/grid.html` with output in `build/width-height/grid.pdf`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Extend shared contracts and helpers every story depends on.

- [ ] T005 Define `RequestedDimension`, `ResolvedDimension`, and `FragmentDimension` records plus enums under `src/Html2x.Core/Dimensions/DimensionContracts.cs` per data-model.md.
- [ ] T006 [P] Align `BlockDimensionQuery`, `BlockDimensionResult`, and `BlockDimensionDiagnostics` types in `src/Html2x.Core/Diagnostics/BlockDimensionDiagnostics.cs` with `specs/001-css-dimension-support/contracts/schema.md`.
- [ ] T007 Implement a reusable validation service for px/pt/percent handling inside `src/Html2x.Layout/Styles/DimensionValidator.cs` that enforces Decision 1 from research.md.
- [ ] T008 [P] Add failing regression coverage for the contract layer in `tests/Html2x.Layout.Test/Dimensions/DimensionContractFacts.cs` referencing the new validation service.

**Checkpoint**: Shared contracts, validation rules, and diagnostics scaffolding are live; user stories can now build specific behavior.

---

## Phase 3: User Story 1 - Fixed Width Blocks Render Predictably (Priority: P1)  MVP

**Goal**: Block-level containers respect px and pt `width` / `height` declarations end to end.  
**Independent Test**: Layout diagnostics prove fragment rectangles equal requested dimensions within 1pt tolerance using the grid fixture in `src/Html2x.Pdf.TestConsole/html/width-height/grid.html`.

### Tests for User Story 1

- [ ] T009 [P] [US1] Create failing px and pt dimension theories in `tests/Html2x.Layout.Test/Dimensions/FixedSizeBlockTests.cs` covering width and height normalization.
- [ ] T010 [P] [US1] Add a renderer regression in `tests/Html2x.Pdf.Test/Dimensions/FixedBlockSnapshotTests.cs` that inspects fragment rectangles for the grid fixture.

### Implementation for User Story 1

- [ ] T011 [US1] Extend CSS parsing and normalization in `src/Html2x.Layout/Styles/CssDimensionResolver.cs` to emit RequestedDimension data for px and pt units.
- [ ] T012 [US1] Apply resolved dimensions inside `src/Html2x.Layout/Fragments/FragmentBuilder.cs` so block fragments honor normalized widths and heights.
- [ ] T013 [US1] Propagate fragment dimensions through the renderer in `src/Html2x.Pdf/Rendering/BlockRenderer.cs` ensuring clip behavior for overflow.
- [ ] T014 [US1] Emit structured diagnostics with requested versus resolved measurements in `src/Html2x.Pdf/Diagnostics/DimensionLogger.cs`.
- [ ] T015 [US1] Refresh the grid harness sample in `src/Html2x.Pdf.TestConsole/html/width-height/grid.html` plus its run script under `build/width-height/run-grid.ps1` to capture bounding boxes for QA.

**Checkpoint**: Fixed pixel dimensions render deterministically with diagnostics proving dimension lineage.

---

## Phase 4: User Story 2 - Bordered Blocks Show Consistent Footprints (Priority: P2)

**Goal**: Bordered placeholders accept percentage widths and pixel heights while keeping frame footprints aligned.  
**Independent Test**: Integration sample `src/Html2x.Pdf.TestConsole/html/width-height/bordered-grid.html` renders consistent rectangles validated by Html2x.Pdf.Test snapshots.

### Tests for User Story 2

- [ ] T016 [P] [US2] Add failing bordered placeholder regression in `tests/Html2x.Pdf.Test/Dimensions/BorderedPlaceholderTests.cs` that asserts border alignment for percent widths.
- [ ] T017 [P] [US2] Introduce layout theories in `tests/Html2x.Layout.Test/Dimensions/BorderedPercentageTests.cs` covering container width resolution and retry logic.

### Implementation for User Story 2

- [ ] T018 [US2] Compute percentage widths against parent dimensions with single-pass retry inside `src/Html2x.Layout/Fragments/BorderedBlockBuilder.cs`.
- [ ] T019 [US2] Preserve border thickness when forwarding fragment rectangles in `src/Html2x.Pdf/Rendering/BorderedBlockRenderer.cs`.
- [ ] T020 [US2] Capture border-aware diagnostics fields (requestedWidth, resolvedWidth, borderThickness) in `src/Html2x.Core/Diagnostics/BlockDimensionDiagnostics.cs`.
- [ ] T021 [US2] Author the bordered grid HTML and expected metrics under `src/Html2x.Pdf.TestConsole/html/width-height/bordered-grid.html` with a run helper at `build/width-height/run-bordered-grid.ps1`.

**Checkpoint**: Percentage-driven bordered blocks align with brand guides and emit deterministic diagnostics.

---

## Phase 5: User Story 3 - Invalid Dimension Inputs Fail Gracefully (Priority: P3)

**Goal**: Unsupported units and conflicting constraints fall back to auto sizing while logging actionable diagnostics.  
**Independent Test**: Negative fixture `src/Html2x.Pdf.TestConsole/html/width-height/invalid.html` produces warnings captured by Layout tests and console logs.

### Tests for User Story 3

- [ ] T022 [P] [US3] Add failing invalid-input tests to `tests/Html2x.Layout.Test/Dimensions/InvalidDimensionTests.cs` covering negative values and unsupported units.
- [ ] T023 [P] [US3] Script a console regression in `build/width-height/run-invalid-grid.ps1` that asserts warnings in `build/logs/width-height/invalid.json`.

### Implementation for User Story 3

- [ ] T024 [US3] Implement validation and fallback responses for unsupported units and conflicting constraints in `src/Html2x.Layout/Styles/DimensionValidator.cs`.
- [ ] T025 [US3] Surface warnings and fallbackReason fields through `src/Html2x.Pdf/Diagnostics/DimensionLogger.cs` and wire them to console output.
- [ ] T026 [US3] Document remediation steps and diagnostics interpretation in `docs/testing-guidelines.md` plus `docs/release-notes.md`.

**Checkpoint**: Invalid inputs fail loudly without crashing layout, and operators have documented remediation paths.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finish release quality work once all stories pass.

- [ ] T027 [P] Update feature documentation and constitution references inside `specs/001-css-dimension-support/plan.md` and `docs/release-notes.md`.
- [ ] T028 Run `dotnet test Html2x.sln -c Release` plus Pdf.TestConsole scripts to regenerate `build/width-height/*.pdf` and attach diagnostics artifacts.
- [ ] T029 [P] Capture final evidence in `docs/testing-guidelines.md` and archive logs under `build/logs/width-height` for stakeholder review.
- [ ] T030 Verify `dotnet run --project src/Html2x.Pdf.TestConsole/Html2x.Pdf.TestConsole.csproj` smoke test uses updated fixtures before merge.

---

## Dependencies & Execution Order

1. Setup (Phase 1) has no prerequisites and must complete before altering contracts.  
2. Foundational (Phase 2) depends on Setup completion; it unblocks all user stories.  
3. User Story 1 (Phase 3) can start immediately after Phase 2 and delivers the MVP scope.  
4. User Story 2 (Phase 4) depends on Phase 3 diagnostics structures but can run parallel to late-stage US1 tasks once `DimensionValidator` stabilizes.  
5. User Story 3 (Phase 5) depends on validation hooks from Phase 2 and diagnostic plumbing from US1.  
6. Polish (Phase 6) runs after all targeted user stories finish and analytics artifacts exist.

### Dependency Graph

Setup → Foundational → {US1 → US2, US3 depends on US1 diagnostics} → Polish  
US2 and US3 share Phase 2 assets but do not block each other once US1 logging is merged.

---

## Parallel Execution Examples

- **US1**: T009 (layout tests) and T010 (renderer regression) can run concurrently since they target different test projects.  
- **US2**: T016 (Pdf snapshot) and T017 (Layout theories) proceed in parallel while implementation tasks wait for test failures.  
- **US3**: T022 (invalid unit tests) and T023 (console script) can be developed by separate contributors to speed up validation tooling.  
- **Cross Story**: T014 (diagnostics) and T020 (border diagnostics) run in parallel once `BlockDimensionDiagnostics` scaffolding (T006) exists.  
- **Polish**: T027 documentation updates and T029 evidence capture operate independently of the final smoke test (T030).

---

## Implementation Strategy

**MVP First (User Story 1)**  
1. Finish Phases 1 and 2.  
2. Complete Phase 3 tasks in order: failing tests (T009–T010), core layout changes (T011–T013), diagnostics (T014), and harness refresh (T015).  
3. Ship MVP once Pdf snapshots and console logs prove deterministic 1pt tolerance.

**Incremental Delivery**  
1. After MVP, branch teams tackle US2 and US3 concurrently, driven by the failing tests from T016–T017 and T022–T023.  
2. Merge user stories individually, ensuring each phase checkpoint is respected and `docs/testing-guidelines.md` gets updates (T026, T027).  
3. Close with Phase 6 full test sweeps.

**Rollback Strategy**  
- If new diagnostics or border handling regressions appear, revert the specific renderer files (`src/Html2x.Pdf/Rendering/*Renderer.cs`) while leaving contracts intact so other stories remain functional.  
- Guard percentage retry changes behind feature flags inside `BorderedBlockBuilder.cs` to disable with minimal risk if convergence exceeds limits.  
- For invalid input handling regressions, toggle warning emission via configuration in `DimensionValidator.cs` to unblock runs while issues are triaged.

**Validation**  
- Each story has explicit independent test criteria (grid fixture, bordered grid, invalid fixture).  
- Tasks T009, T016, and T022 ensure failing coverage before code changes.  
- Console scripts under `build/width-height` double check diagnostics per quickstart.md guidance.
