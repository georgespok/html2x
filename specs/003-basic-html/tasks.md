---

description: "Task list template for feature implementation"
---

# Tasks: Basic HTML-to-PDF Essentials

**Input**: Design documents from `/specs/003-basic-html/`  
**Prerequisites**: plan.md (required), spec.md (user stories), research.md, data-model.md, contracts/

**Tests**: Introduce exactly one failing automated test at a time and list it explicitly for each user story. Pair every failing-test task with the minimal implementation/refactor tasks required to turn the suite green before adding the next test.

**Goal-Driven Cadence**: For each user story capture State Assessment, Action Decomposition, Path Planning (with dependencies + rollback steps), Adaptive Execution checkpoints, and Reflection tasks so Principle VI stays explicit.

**Organization**: Tasks are grouped by user story to keep delivery incremental and independently testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm baseline project health before feature work begins.

- [X] T001 Run `dotnet restore Html2x.sln` and capture baseline notes in `specs/003-basic-html/quickstart.md`.
- [X] T002 Build the solution via `Html2x.sln` and ensure analyzers are green before modifications.
- [X] T003 Seed `specs/003-basic-html/samples/basic.html` with representative `<p>`, `<img>`, and border markup for reuse in tests.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before any user story starts.

- [X] T004 Extend diagnostics contracts in `src/Html2x.Abstractions/Diagnostics/Contracts/FragmentDiagnostics.cs` to include `LineIndex`, `SourceType`, and per-side border payloads as defined in `contracts/diagnostics-contract.md`.
- [X] T005 Wire TestConsole options in `src/Tests/Html2x.TestConsole/Options/DiagnosticsOptions.cs` to emit the new diagnostics fields into JSON dumps.
- [X] T006 Introduce the default display-role lookup table skeleton in `src/Html2x.LayoutEngine/Style/DisplayRoleMap.cs` covering block/inline/list basics for later stories.
- [X] T007 Verify stage-isolation dependencies by running `dotnet msbuild src/Html2x.sln /t:ProjectReferenceGraph` and documenting results in `specs/003-basic-html/research.md` to confirm Abstractions → LayoutEngine → Renderers is preserved.

**Checkpoint**: Foundational diagnostics scaffolding ready; user stories can add behavior on top.

---

## Phase 3: User Story 1 - Flow Text With Line Breaks (Priority: P1)  MVP

**Goal**: Preserve predictable inline flow with `<br>`, baseline CSS (color, line-height, text-align), and deterministic diagnostics.
**Independent Test**: Layout tests validate fragment order and spacing; integration test inspects diagnostics JSON without parsing PDFs.
**State Assessment**: Inline processing currently collapses `<br>` output into a single `LineBoxFragment`, preventing deterministic diagnostics assertions about line order.
**Action Decomposition**: Add failing tests → update inline fragment stage → update diagnostics writer → verify console output → document results.
**Path Planning & Risks**: Depends on diagnostics contract (T004) and sample HTML (T003); rollback by reinstating the previous merge behavior if regressions appear.
**Adaptive Checkpoints**: After failing tests land, confirm deterministic line splitting before implementation; second checkpoint after diagnostics writer update.
**Reflection Hook**: Record lessons in `research.md` once diagnostics-only verification proves stable.

### Tests for User Story 1

- [X] T008 [US1] Add failing layout test verifying `<br>` produces multiple `LineBoxFragment` entries in `tests/Html2x.LayoutEngine.Test/Text/LineBoxFragmentTests.cs`.
- [X] T009 [P] [US1] Add failing integration test asserting `line-height`, `text-align`, and `color` metadata via diagnostics in `tests/Html2x.Test/Scenarios/BasicHtmlDiagnosticsTests.cs`.

### Implementation for User Story 1

- [X] T010 [US1] Update `src/Html2x.LayoutEngine/Fragment/Stages/InlineFragmentStage.cs` so `<br>` forces a new `LineBoxFragment` while preserving CSS color/line-height/text-align data.
- [X] T011 [US1] Update diagnostics writer `src/Html2x.Abstractions/Diagnostics/Writers/FragmentDiagnosticsWriter.cs` to include `LineIndex`, text alignment, and line height.
- [X] T012 [US1] Adjust renderer alignment handling in `src/Html2x.Renderers.Pdf/Text/TextRenderer.cs` to respect the new fragment data.
- [X] T013 [US1] Add unit test in `tests/Html2x.Renderers.Pdf.Test/Dependencies/RendererIsolationTests.cs` verifying renderer code touched in US1 only consumes abstractions (no layout/style types).
- [X] T014 [US1] Refresh docs in `specs/003-basic-html/spec.md` (User Story 1 section) with observed reflection notes once diagnostics-only verification passes.
- [X] T015 [US1] Review and refactor inline/text changes (layout + renderer) to remove duplication and log takeaways in `specs/003-basic-html/research.md`.

**Checkpoint**: User Story 1 fully functional, independently testable, and observable.

---

## Phase 4: User Story 2 - Embed Basic Images (Priority: P1)

**Goal**: Support explicit width/height on `<img>` tags with aspect-ratio fallback and `max-width` enforcement using diagnostics traces.
**Independent Test**: Layout unit tests for sizing math; integration test checks diagnostics JSON for final dimensions/source type.
**State Assessment**: Image fragments currently assume both dimensions and allow remote assets; diagnostics lack source metadata.
**Action Decomposition**: Add failing tests → enforce sizing rules in layout → annotate diagnostics → update TestConsole sample → refactor shared helpers.
**Path Planning & Risks**: Depends on diagnostics extensions (T004) and text story completion for shared helpers; rollback via feature flag gating the aspect-ratio logic.
**Adaptive Checkpoints**: After dimension-derivation test, re-evaluate before adding diagnostics; second checkpoint after console manual run.
**Reflection Hook**: Capture sizing edge cases in `research.md` post-validation.

### Tests for User Story 2

- [ ] T016 [US2] Add failing layout test covering explicit width/height and ratio derivation in `tests/Html2x.LayoutEngine.Test/Images/ImageFragmentBuilderTests.cs`.
- [ ] T017 [US2] Add failing image-source resolver test in `tests/Html2x.LayoutEngine.Test/Images/ImageSourceResolverTests.cs` verifying HTTP/HTTPS assets are rejected and a diagnostics warning is emitted.
- [ ] T018 [P] [US2] Add failing integration test ensuring diagnostics record `WidthPx`, `HeightPx`, `SourceType`, and `MaxWidthPx` in `tests/Html2x.Test/Scenarios/ImageDiagnosticsTests.cs`.

### Implementation for User Story 2

- [ ] T019 [US2] Update image builder logic in `src/Html2x.LayoutEngine/Fragments/ImageFragmentBuilder.cs` to calculate missing dimensions and clamp to `max-width`.
- [ ] T020 [US2] Enforce disk/data-URI restrictions (reject HTTP/HTTPS and log diagnostics) in `src/Html2x.LayoutEngine/Assets/ImageSourceResolver.cs`.
- [ ] T021 [US2] Emit image diagnostics fields in `src/Html2x.Abstractions/Diagnostics/Writers/FragmentDiagnosticsWriter.cs` and ensure JSON output matches the contract.
- [ ] T022 [US2] Extend dependency guard tests in `tests/Html2x.Renderers.Pdf.Test/Dependencies/RendererIsolationTests.cs` to ensure new image code keeps renderer-to-layout isolation intact.
- [ ] T023 [US2] Document manual verification steps for images in `specs/003-basic-html/quickstart.md` (reference the sample HTML, including failed remote asset attempts).
- [ ] T024 [US2] Review and refactor the image sizing + source resolver changes to remove duplicated math and summarize results in `specs/003-basic-html/research.md`.

**Checkpoint**: User Story 2 meets diagnostics parity and deterministic sizing goals.

---

## Phase 5: User Story 3 - Apply Borders and Default Display Roles (Priority: P2)

**Goal**: Respect per-side border settings and default HTML display roles to keep layout stacking predictable without advanced formatting contexts.
**Independent Test**: Layout tests verify border metadata and display roles; integration test inspects list-item diagnostics.
**State Assessment**: Display role table placeholder exists (T006) but lacks mappings; borders currently uniform and diagnostics omit per-side payloads.
**Action Decomposition**: Add failing tests → implement display-role map + border metadata → update renderer + diagnostics → refresh docs → refactor shared logic.
**Path Planning & Risks**: Depends on foundational diagnostics work and prior stories; rollback by limiting border styles to uniform if regressions detected.
**Adaptive Checkpoints**: After mapping table implemented, validate against sample HTML; second checkpoint after renderer border pass.
**Reflection Hook**: Note display-role edge cases in `research.md` for future multi-page work.

### Tests for User Story 3

- [ ] T025 [US3] Add failing test covering per-side borders in `tests/Html2x.Renderers.Pdf.Test/Borders/BorderDiagnosticsTests.cs`.
- [ ] T026 [US3] Add failing clamp test in `tests/Html2x.Renderers.Pdf.Test/Borders/BorderClampTests.cs` to render borders thicker than 20px and assert renderer + diagnostics clamp to the maximum.
- [ ] T027 [P] [US3] Add failing list-item display-role test in `tests/Html2x.LayoutEngine.Test/Display/DisplayRoleMapTests.cs`.
- [ ] T028 [US3] Extend integration scenario verifying unordered/ordered lists in `tests/Html2x.Test/Scenarios/DisplayRoleDiagnosticsTests.cs`.

### Implementation for User Story 3

- [ ] T029 [US3] Populate the display-role table in `src/Html2x.LayoutEngine/Style/DisplayRoleMap.cs` with block/inline/inline-block/list-item defaults.
- [ ] T030 [US3] Attach `FragmentBorderMetadata` to block fragments in `src/Html2x.LayoutEngine/Fragments/BlockFragmentFactory.cs`.
- [ ] T031 [US3] Render per-side borders in `src/Html2x.Renderers.Pdf/Borders/BorderRenderer.cs` using the metadata and enforce the clamp behavior introduced in T026.
- [ ] T032 [US3] Extend dependency guard tests in `tests/Html2x.Renderers.Pdf.Test/Dependencies/RendererIsolationTests.cs` to ensure border/display updates keep renderer code isolated from layout internals.
- [ ] T033 [US3] Update documentation in `specs/003-basic-html/spec.md` Edge Cases to note border clamping + default display behavior.
- [ ] T034 [US3] Review and refactor border + display-role implementations (layout + renderer) to remove duplication and capture follow-ups in `specs/003-basic-html/research.md`.

**Checkpoint**: User Story 3 complete with deterministic borders, clamping, and display roles.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Final adjustments spanning multiple stories.

- [ ] T035 Update `specs/003-basic-html/research.md` with lessons learned for text, image, and border diagnostics-only validation.
- [ ] T036 Refresh `specs/003-basic-html/contracts/diagnostics-contract.md` examples with actual payload snippets captured from TestConsole.
- [ ] T037 Document release notes in `docs/release-notes.md` summarizing new HTML capabilities and limitations (single-page, disk/data URIs only).
- [ ] T038 Validate `dotnet test Html2x.sln -c Release` and record results in `specs/003-basic-html/quickstart.md` before handing off.
- [ ] T039 Capture performance metrics by running `dotnet run --project src/Tests/Html2x.TestConsole/... basic.html` and record line-box + render runtime plus renderer allocations in `specs/003-basic-html/quickstart.md`, confirming variance remains within ±5% of the stored baseline.
- [ ] T040 If variance exceeds target, document mitigation/rollback steps in `specs/003-basic-html/research.md` and update the plan before closing the feature.

---

## Dependencies & Execution Order

### Phase Dependencies

1. **Setup** (Phase 1) must complete before diagnostics or story work begins.
2. **Foundational** (Phase 2) depends on Setup and blocks all user stories.
3. **User Story 1** (Phase 3) delivers the MVP and must finish before later stories relying on text fragment updates.
4. **User Story 2** (Phase 4) depends on Phase 2 + US1 for shared diagnostics helpers.
5. **User Story 3** (Phase 5) depends on Phase 2 plus any reusable diagnostics utilities from US1/US2.
6. **Polish** runs after all user stories.

### User Story Dependencies

- **US1**: No dependency beyond foundational.
- **US2**: Reuses diagnostics writers from US1; ensure T011 finished before T021.
- **US3**: Requires display-role skeleton (T006) and diagnostics writers (T011/T021).

### Parallel Opportunities

- Tests marked [P] (T009, T018, T027) can run in parallel once prerequisites complete.
- Renderer vs layout tasks (e.g., T010 vs T012 or T031 vs T029) can progress concurrently after failing tests exist.

### Within Each User Story

- Introduce a single failing test, implement the minimal passing change, refactor, then repeat for the next scenario.
- Update shared contracts prior to renderer changes.
- Keep predictability checks and diagnostics tasks visible in the plan.
- Confirm documentation and release notes before closing the story.

### Implementation Strategy

1. Deliver User Story 1 as MVP (text flow + diagnostics) before touching images or borders.
2. Layer User Story 2 (images) once text diagnostics are stable.
3. Add User Story 3 (borders + display roles + clamping) last, then run polish tasks and full regression.







