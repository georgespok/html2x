---
description: "Task list for CSS width and height feature delivery"
---

# Tasks: CSS Height and Width Support

**Input**: Design documents from `/specs/001-css-dimension-support/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/schema.md

**Tests**: Follow the single-test TDD loop. Introduce exactly one failing test, implement the minimal passing change/refactor, then proceed to the next scenario once the suite is green. Promote deterministic layout evidence into Html2x.LayoutEngine.Test, Html2x.Renderers.Pdf.Test, and Html2x.TestConsole harness artifacts.

**Organization**: Tasks are grouped by user story to maintain incremental, independently testable delivery.

## Format: `[ID] [P?] [Story] Description`

- **[P]** indicates the task can proceed in parallel because it touches isolated files and does not involve an active failing test loop.
- **[Story]** maps tasks to spec.md user stories (US1, US2, US3).
- Every task references the exact file path it modifies or validates.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prove the repo is healthy and capture baseline fixtures before touching contracts.

- [X] T001 Run `dotnet restore Html2x.sln` to ensure NuGet dependencies resolve for Html2x.sln.
- [X] T002 Execute `dotnet test Html2x.sln -c Release --filter "Html2x.LayoutEngine.Test&&Category=Dimensions"` to record baseline results for `src/Tests/Html2x.LayoutEngine.Test`.
- [X] T003 Execute `dotnet test Html2x.sln -c Release --filter "Html2x.Renderers.Pdf.Test&&Category=BorderedBlocks"` to confirm renderer baseline in `src/Tests/Html2x.Renderers.Pdf.Test`.
- [ ] T004 Capture a reference PDF and diagnostics log by running `src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj` against `src/Tests/Html2x.TestConsole/html/width-height/grid.html` with output in `build/width-height/grid.pdf`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Extend shared contracts and helpers every story depends on.

- [X] T005 Define `RequestedDimension`, `ResolvedDimension`, and `FragmentDimension` records in `src/Html2x.Abstractions/Measurements/Dimensions` folder per data-model.md.
- [ ] T006 [P] Align `BlockDimensionQuery`, `BlockDimensionResult`, and `BlockDimensionDiagnostics` types in `src/Html2x.Abstractions/Diagnostics/BlockDimensionDiagnostics.cs` with `specs/001-css-dimension-support/contracts/schema.md`.
- [ ] T007 Implement the base px/pt/% validation service inside `src/Html2x.LayoutEngine/Style/DimensionValidator.cs` that enforces Decision 1 from research.md.
- [ ] T007A [P] Normalize locale-specific decimal separators for width/height parsing inside `src/Html2x.LayoutEngine/Style/CssValueConverter.cs`, logging the normalization for diagnostics.
- [ ] T007B [P] Introduce `DimensionStyleMapper` under `src/Html2x.LayoutEngine/Style/DimensionStyleMapper.cs` to centralize Requested/Resolved dimension creation.
- [ ] T008 Add failing regression coverage for the contract layer in `src/Tests/Html2x.LayoutEngine.Test/Dimensions/DimensionContractFacts.cs` referencing the new validation service.
- [ ] T008A Add locale-decimal parsing tests in `src/Tests/Html2x.LayoutEngine.Test/Dimensions/DimensionContractFacts.cs` to lock in the normalization behavior from T007A.

**Checkpoint**: Shared contracts, validation rules, and diagnostics scaffolding are live; user stories can now build specific behavior.

---

## Phase 3: User Story 1 - Fixed Width Blocks Render Predictably (Priority: P1)  MVP

**Goal**: Block-level containers respect px and pt `width` / `height` declarations end to end.  
**Independent Test**: Layout diagnostics prove fragment rectangles equal requested dimensions within 1pt tolerance using the grid fixture in `src/Tests/Html2x.TestConsole/html/width-height/grid.html`.

### Tests for User Story 1

- [ ] T009 [US1] Introduce the first failing px/pt dimension theory in `src/Tests/Html2x.LayoutEngine.Test/Dimensions/FixedSizeBlockTests.cs`; keep it the only failing test and unblock T011-T012 immediately afterward.
- [ ] T009A [US1] After the T009 loop is green, add a failing complementary-dimension theory that supplies only width or height and asserts the missing dimension is derived within 1 pt before moving on.
- [ ] T009B [US1] Add a failing inline-element height advisory test in `src/Tests/Html2x.LayoutEngine.Test/Dimensions/InlineHeightAdvisoryTests.cs` proving height on inline nodes is ignored but logged.
- [ ] T010 [US1] Once the T009/T011 loops are green, add the renderer regression in `src/Tests/Html2x.Renderers.Pdf.Test/Dimensions/FixedBlockSnapshotTests.cs` to inspect fragment rectangles for the grid fixture before starting any new failing tests.
- [ ] T010A [US1] After T010/T013 complete, introduce a failing auto-height variance regression (Layout + Pdf snapshot) that forces the engine to detect > 1 pt variance before implementing the fix.
- [ ] T010B [US1] Add a failing renderer regression in `src/Tests/Html2x.Renderers.Pdf.Test/Dimensions/MissingFontSnapshotTests.cs` that verifies rectangle comparisons remain stable when fonts are unavailable.

### Implementation for User Story 1

- [ ] T011 [US1] Extend CSS parsing and normalization inside `src/Html2x.LayoutEngine/Style/CssStyleComputer.cs` (and supporting `CssValueConverter`) to emit RequestedDimension data for px and pt units.
- [ ] T011A [US1] Update `src/Html2x.LayoutEngine/Style/CssStyleComputer.cs` (and/or helper converters) to derive the complementary dimension when only width or height is supplied, emitting the tolerance metadata needed by FR-004.
- [ ] T011B [US1] Wire `DimensionStyleMapper` plus diagnostics so inline-height declarations are treated as no-ops while emitting advisory logs per edge-case guidance.
- [ ] T012 [US1] Apply resolved dimensions inside `src/Html2x.LayoutEngine/Fragment/FragmentBuilder.cs` so block fragments honor normalized widths and heights.
- [ ] T013 [US1] Propagate fragment dimensions through the renderer in `src/Html2x.Renderers.Pdf/Pipeline/PdfRenderer.cs`, ensuring clip behavior for overflow.
- [ ] T013A [US1] Add variance tracking in `src/Html2x.LayoutEngine/LayoutBuilder.cs` (or equivalent) so auto-height passes enforce the +/-1 pt tolerance and fail fast when it is exceeded.
- [ ] T014 [US1] Emit structured diagnostics with requested versus resolved measurements in `src/Html2x.Renderers.Pdf/Diagnostics/DimensionLogger.cs`.
- [ ] T015 [US1] Refresh the grid harness sample in `src/Tests/Html2x.TestConsole/html/width-height/grid.html` plus its run script under `build/width-height/run-grid.ps1` to capture bounding boxes for QA.
- [ ] T015A [US1] Add a `missing-fonts.html` fixture under `src/Tests/Html2x.TestConsole/html/width-height/` and update the harness script so rectangle comparisons remain valid when fonts fall back.

**Checkpoint**: Fixed pixel dimensions render deterministically with diagnostics proving dimension lineage.

---

## Phase 4: User Story 2 - Bordered Blocks Show Consistent Footprints (Priority: P2)

**Goal**: Bordered placeholders accept percentage widths and pixel heights while keeping frame footprints aligned.  
**Independent Test**: Integration sample `src/Tests/Html2x.TestConsole/html/width-height/bordered-grid.html` renders consistent rectangles validated by Html2x.Renderers.Pdf.Test snapshots.

### Tests for User Story 2

- [ ] T016 [US2] Introduce a single failing bordered placeholder regression in `src/Tests/Html2x.Renderers.Pdf.Test/Dimensions/BorderedPlaceholderTests.cs`, pair it immediately with T018 before adding more tests.
- [ ] T017 [US2] After the T016/T018 loop is green, add layout theories in `src/Tests/Html2x.LayoutEngine.Test/Dimensions/BorderedPercentageTests.cs` covering container width resolution and retry logic, then proceed to T019 only after they pass.

### Implementation for User Story 2

- [ ] T018 [US2] Compute percentage widths against parent dimensions with single-pass retry inside `src/Html2x.LayoutEngine/Fragment/BorderedBlockBuilder.cs` (create this builder in the `Fragment` folder if it doesn’t exist yet).
- [ ] T019 [US2] Preserve border thickness when forwarding fragment rectangles in `src/Html2x.Renderers.Pdf/Rendering/QuestPdfFragmentRenderer.cs`.
- [ ] T020 [US2] Capture border-aware diagnostics fields (requestedWidth, resolvedWidth, borderThickness) in `src/Html2x.Abstractions/Diagnostics/BlockDimensionDiagnostics.cs`.
- [ ] T021 [US2] Author the bordered grid HTML and expected metrics under `src/Tests/Html2x.TestConsole/html/width-height/bordered-grid.html` with a run helper at `build/width-height/run-bordered-grid.ps1`.

**Checkpoint**: Percentage-driven bordered blocks align with brand guides and emit deterministic diagnostics.

---

## Phase 5: User Story 3 - Invalid Dimension Inputs Fail Gracefully (Priority: P3)

**Goal**: Unsupported units and conflicting constraints fall back to auto sizing while logging actionable diagnostics.  
**Independent Test**: Negative fixture `src/Tests/Html2x.TestConsole/html/width-height/invalid.html` produces warnings captured by Layout tests and console logs.

### Tests for User Story 3

- [ ] T022 [US3] Introduce one failing invalid-input test in `src/Tests/Html2x.LayoutEngine.Test/Dimensions/InvalidDimensionTests.cs` (negative values or unsupported units) and pair it immediately with T024 before adding the next scenario.
- [ ] T023 [P] [US3] Script a console regression in `build/width-height/run-invalid-grid.ps1` that asserts warnings in `build/logs/width-height/invalid.json`.

### Implementation for User Story 3

- [ ] T024 [US3] Build on T007 by adding unsupported-unit and conflicting-constraint fallbacks in `src/Html2x.LayoutEngine/Style/DimensionValidator.cs`, emitting warning payloads needed by diagnostics.
- [ ] T025 [US3] Surface warnings and fallbackReason fields through `src/Html2x.Renderers.Pdf/Diagnostics/DimensionLogger.cs` and wire them to console output.
- [ ] T026 [US3] Document remediation steps and diagnostics interpretation in `docs/testing-guidelines.md` plus `docs/release-notes.md`.

**Checkpoint**: Invalid inputs fail loudly without crashing layout, and operators have documented remediation paths.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finish release quality work once all stories pass.

- [ ] T027 [P] Update feature documentation and constitution references inside `specs/001-css-dimension-support/plan.md` and `docs/release-notes.md`.
- [ ] T028 Run `dotnet test Html2x.sln -c Release` plus Html2x.TestConsole scripts to regenerate `build/width-height/*.pdf` and attach diagnostics artifacts.
- [ ] T029 [P] Capture final evidence in `docs/testing-guidelines.md` and archive logs under `build/logs/width-height` for stakeholder review.
- [ ] T029A Script a timed diagnostics dry run (≤5 minutes) that replays the logged width/height warnings and records elapsed time to `build/logs/width-height/triage.json`.
- [ ] T029B Record “zero manual PDF edits” by storing the untouched Html2x.TestConsole outputs for the reporting cycle and referencing them in `docs/release-notes.md`.
- [ ] T030 Verify `dotnet run --project src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj` smoke test uses updated fixtures before merge.

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

- **US1**: Keep loops sequential—T009 -> T011/T012, T009A -> T011A, T009B -> T011B, T010 -> T013, T010A -> T013A—and never run more than one failing test at a time even though the projects differ. Run the missing-font regression (T010B) only after these loops and the inline-height advisory are green.  
- **US2**: Teams may own US2 work concurrently, but finish the T016 → T018 loop before starting T017 → T019 to preserve the single-test rule.  
- **US3**: US3 work can progress alongside other stories, yet each invalid-input scenario (T022 → T024, then T023 → T025) must remain a separate green loop before adding the next failing test.  
- **Cross Story**: T014 (diagnostics) and T020 (border diagnostics) run in parallel once `BlockDimensionDiagnostics` scaffolding (T006) exists.  
- **Polish**: T027 documentation updates and T029 evidence capture operate independently of the final smoke test (T030).

---

## Implementation Strategy

**MVP First (User Story 1)**  
1. Finish Phases 1 and 2.  
2. Complete Phase 3 as alternating loops:  
   - Loop 1: T009 -> T011/T012
   - Loop 2: T009A -> T011A
   - Loop 3: T009B -> T011B
   - Loop 4: T010 -> T013
   - Loop 5: T010A -> T013A
   Follow with diagnostics (T014) and harness refresh (T015).  
   Run the missing-font regression (T010B) and update the console fixtures (T015A) once the five loops are green.
3. Ship MVP once Pdf snapshots and console logs prove deterministic 1 pt tolerance.  

**Incremental Delivery**  
1. After MVP, branch teams tackle US2 and US3 concurrently, but each team must finish loops sequentially (T016 → T018, then T017 → T019; T022 → T024, then T023 → T025) before starting the next scenario.  
2. Merge user stories individually, ensuring each phase checkpoint is respected and `docs/testing-guidelines.md` gets updates (T026, T027).  
3. Close with Phase 6 full test sweeps.

**Rollback Strategy**  
- If new diagnostics or border handling regressions appear, revert the specific renderer files (`src/Html2x.Renderers.Pdf/Rendering/*Renderer.cs`) while leaving contracts intact so other stories remain functional.  
- Guard percentage retry changes behind feature flags inside `BorderedBlockBuilder.cs` to disable with minimal risk if convergence exceeds limits.  
- For invalid input handling regressions, toggle warning emission via configuration in `DimensionValidator.cs` to unblock runs while issues are triaged.

**Validation**  
- Each story has explicit independent test criteria (grid fixture, bordered grid, invalid fixture).  
- Tasks T009, T016, and T022 ensure failing coverage before code changes.  
- Console scripts under `build/width-height` double check diagnostics per quickstart.md guidance.









