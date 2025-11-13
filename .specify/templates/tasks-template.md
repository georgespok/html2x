---

description: "Task list template for feature implementation"
---

# Tasks: [FEATURE NAME]

**Input**: Design documents from `/specs/[###-feature-name]/`  
**Prerequisites**: plan.md (required), spec.md (user stories), research.md, data-model.md, contracts/

**Tests**: Introduce exactly one failing automated test at a time and list it explicitly for each user story. Pair every failing-test task with the minimal implementation/refactor tasks required to turn the suite green before adding the next test.

**Organization**: Tasks are grouped by user story to keep delivery incremental and independently testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- Source: `src/Html2x.*`
- Tests: `tests/Html2x.*` (`html2x.IntegrationTest` for end-to-end)
- Console harness: `src/Html2x.TestConsole/`
- Samples and fonts: `src/Html2x.TestConsole/html/`, `src/Html2x.TestConsole/fonts/`

<!--
  ============================================================================
  IMPORTANT: The tasks below are SAMPLE TASKS for illustration purposes only.

  The /speckit.tasks command MUST replace these with actual tasks based on:
  - User stories from spec.md (with their priorities P1, P2, P3...)
  - Feature requirements from plan.md
  - Entities from data-model.md
  - Contracts from contracts/

  Tasks MUST be organized by user story so each story can be:
  - Implemented independently
  - Tested independently
  - Delivered as an MVP increment

  DO NOT keep these sample tasks in the generated tasks.md file.
  ============================================================================
-->

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm baseline project health before feature work begins.

- [ ] T001 Run `dotnet restore Html2x.sln` and capture dependency notes.
- [ ] T002 Verify analyzer and formatting configuration (`.editorconfig`, Directory.Build.props).
- [ ] T003 [P] Update plan.md and checklist with constitution gates.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before any user story starts.

- [ ] T004 Establish stage boundaries or shared contracts required by all stories (e.g., new fragment types in `src/Html2x.Core/`).
- [ ] T005 [P] Add failing integration test covering deterministic output for the new scenario in `tests/html2x.IntegrationTest/`.
- [ ] T006 [P] Extend logging helpers (`src/Html2x.Pdf/Logging/`) if new diagnostics are needed.
- [ ] T007 Document operational changes in `docs/` and update quickstart notes if tooling changes.

**Checkpoint**: Foundation ready - user story implementation can now begin.

---

## Phase 3: User Story 1 - [Title] (Priority: P1)  MVP

**Goal**: [Brief description of what this story delivers]  
**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 1

- [ ] T010 [P] [US1] Write failing unit test in `tests/Html2x.Layout.Test/` or relevant project.
- [ ] T011 [P] [US1] Extend integration scenario in `tests/html2x.IntegrationTest/` to assert deterministic fragments or PDF parity.

### Implementation for User Story 1

- [ ] T012 [US1] Implement feature code in `src/Html2x.Layout/...` or appropriate project respecting stage contracts.
- [ ] T013 [US1] Wire renderer changes in `src/Html2x.Pdf/...` if required.
- [ ] T014 [US1] Add structured logging or diagnostics for the new path.
- [ ] T015 [US1] Update docs and release notes to describe behavior and migration guidance.

**Checkpoint**: User Story 1 fully functional, independently testable, and observable.

---

## Phase 4: User Story 2 - [Title] (Priority: P2)

**Goal**: [Brief description of what this story delivers]  
**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 2

- [ ] T020 [P] [US2] Add failing tests for new behavior (unit or integration as appropriate).
- [ ] T021 [P] [US2] Capture observability assertions (log events, metrics) in tests.

### Implementation for User Story 2

- [ ] T022 [US2] Implement layout or renderer updates with stage isolation.
- [ ] T023 [US2] Extend shared contracts if needed and document the change.
- [ ] T024 [US2] Ensure deterministic outputs remain validated by updating existing baselines.

**Checkpoint**: User Stories 1 and 2 remain independently deliverable.

---

## Phase 5: User Story 3 - [Title] (Priority: P3)

**Goal**: [Brief description of what this story delivers]  
**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 3

- [ ] T025 [P] [US3] Add failing tests capturing the new scenario.
- [ ] T026 [P] [US3] Validate logging coverage or diagnostics for this story.

### Implementation for User Story 3

- [ ] T027 [US3] Implement code changes respecting pipeline contracts.
- [ ] T028 [US3] Update renderer or layout integrations as required.
- [ ] T029 [US3] Refresh docs or samples illustrating the capability.

**Checkpoint**: All user stories function independently with passing tests and observability hooks.

---

[Add more user story phases as needed, following the same pattern]

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories.

- [ ] T030 [P] Documentation updates in `docs/` and release notes.
- [ ] T031 Harden performance or memory hotspots while preserving determinism.
- [ ] T032 [P] Expand regression coverage or golden files if justified.
- [ ] T033 Validate `dotnet test Html2x.sln -c Release` and console smoke test.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion - blocks all user stories.
- **User Stories (Phase 3+)**: Depend on Foundational completion; can proceed in parallel once their tests exist.
- **Polish (Final Phase)**: Depends on chosen user stories being complete.

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational; no dependency on other stories.
- **User Story 2 (P2)**: Can start after Foundational; may reuse US1 artifacts but must stay independently testable.
- **User Story 3 (P3)**: Can start after Foundational; coordinate with earlier stories only through shared contracts.

### Within Each User Story

- Introduce a single failing test, implement the minimal passing change, refactor, then repeat for the next scenario.
- Update shared contracts prior to renderer changes.
- Keep determinism checks and logging tasks visible in the plan.
- Confirm documentation and release notes before closing the story.

### Parallel Opportunities

- Setup and Foundational tasks marked [P] can run in parallel.
- Once Foundational completes, individual user stories can progress concurrently.
- Tests marked [P] can be developed in parallel provided they touch separate files.
- Documentation and release note tasks can run alongside polish work.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (blocks all stories).
3. Complete Phase 3: User Story 1 with passing tests and observability in place.
4. Stop and validate deterministic outputs before continuing.

### Incremental Delivery

1. Finish Setup and Foundational.
2. Deliver User Story 1 (MVP) with smoke tests.
3. Layer additional stories sequentially or in parallel, each with failing-tests-first workflow.
4. Merge only after updating docs, logging, and release notes.

### Parallel Team Strategy

1. Team completes Setup and Foundational together.
2. Assign stories to different contributors once shared contracts and failing tests exist.
3. Coordinate through documented interfaces and deterministic baselines.
4. Re-run full test suite and console smoke test before merge.

---

## Notes

- Mark tasks complete when code, tests, and docs update together.
- Include references to relevant principles when exceptions are required.
- Capture follow-up actions or debt in the plan if a principle is temporarily violated.
