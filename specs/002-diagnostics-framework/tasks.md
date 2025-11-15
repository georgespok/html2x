# Tasks: Diagnostics Framework

**Input**: Design documents from `/specs/002-diagnostics-framework/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Follow TDD by adding one failing test per story before implementing the fix. Each failing test task below must be completed prior to the paired implementation task.

**Organization**: Tasks are grouped by user story so each increment stays independently testable.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish a clean baseline before adding diagnostics features.

- [X] T001 Run `dotnet restore Html2x.sln` and record dependency notes in `build/diagnostics/setup.md`.
- [X] T002 Execute `dotnet test Html2x.sln -c Release` to capture current pass/fail state in `build/diagnostics/setup.md`.
- [X] T003 [P] Create `build/diagnostics/.gitkeep` and document CLI commands in `specs/002-diagnostics-framework/quickstart.md`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Create the diagnostics project, contracts, and console harness scaffolding required by all stories.

- [X] T004 Scaffold `src/Html2x.Diagnostics/Html2x.Diagnostics.csproj`, add it to `Html2x.sln`, and include the project reference metadata.
- [X] T005 Define diagnostics contracts in `src/Html2x.Abstractions/Diagnostics/IDiagnosticsRuntime.cs` plus related DTOs per `data-model.md`.
- [X] T006 Update `src/Html2x/HtmlConverter.cs` to accept diagnostics decorators without changing default behavior.
- [X] T007 [P] Add `src/Html2x.TestConsole/diagnostics/run-diagnostics-json.ps1` and `run-diagnostics-json.sh` script stubs documented in `quickstart.md`.
- [X] T008 Inventory `ILogger` usage across `src/Html2x.*` projects, document remaining console-only exceptions in `specs/002-diagnostics-framework/research.md`, and confirm Html2x.TestConsole is the sole component allowed to print via `ILogger`.
- [X] T009 Implement diagnostics-based logging adapters in `src/Html2x.Diagnostics/Logging/*` and replace remaining `ILogger` calls in `src/Html2x.*` (except Html2x.TestConsole) so diagnostics drive all telemetry.
- [X] T010 Harden `src/Html2x.Diagnostics/Runtime/DiagnosticsRuntime.cs` and `src/Html2x.Diagnostics/Pipeline/DiagnosticsDispatcher.cs` to keep renders output-identical when diagnostics are enabled, then document any remaining variances in `build/diagnostics/setup.md`.

**Checkpoint**: Diagnostics project, contracts, and console tooling exist; ready for story-specific work.

---

## Phase 3: User Story 1 – Toggleable Diagnostics Session (Priority: P1)  MVP

**Goal**: Allow operators to enable diagnostics per render while keeping the off-state zero cost.  
**Independent Test**: Rendering the same HTML with diagnostics disabled must emit zero events; enabling diagnostics must emit ordered stage events without changing output.

### Tests for User Story 1

- [X] T011 [US1] Add failing regression test `tests/Html2x.Test/Diagnostics/DiagnosticsToggleTests.cs` that asserts diagnostics are off by default and emit no events.
- [X] T012 [P] [US1] Add failing integration test `tests/Html2x.Test/Diagnostics/StageEventOrderTests.cs` ensuring an enabled session emits start/stop events for every pipeline stage.

### Implementation for User Story 1

- [X] T013 [US1] Implement opt-in session creation and disabled fast-path in `src/Html2x.Diagnostics/Runtime/DiagnosticsRuntime.cs` to satisfy T011.
- [X] T014 [US1] Wire stage-level event publishers in `src/Html2x.LayoutEngine/*` and `src/Html2x.Renderers.Pdf/*` via `src/Html2x.Diagnostics/Pipeline/DiagnosticsDispatcher.cs` to satisfy T012.
- [X] T015 [US1] Update `src/Html2x.TestConsole/Program.cs` to expose `--diagnostics` flags and ensure diagnostics stay off unless explicitly enabled.
- [X] T016 [US1] Refresh `specs/002-diagnostics-framework/quickstart.md` with the new `DiagnosticsRuntime.Configure` usage and console instructions.

**Checkpoint**: User Story 1 fully functional with deterministic event streams and updated quickstart guidance.

---

## Phase 4: User Story 2 – Stage Insights and Structured Dumps (Priority: P2)

**Goal**: Capture structured dumps and reasoning contexts for style trees, box trees, and fragment chains.  
**Independent Test**: Enabled diagnostics must output deterministic dumps and context events that explain shrink-to-fit or layout failures without mutating pipeline state.

### Tests for User Story 2

- [X] T017 [US2] Add failing dump serialization test `tests/Html2x.Test/Diagnostics/StructuredDumpTests.cs` that asserts layout dumps include stable node counts and identifiers.
- [ ] T018 [P] [US2] Add failing context emission test `tests/Html2x.Test/Diagnostics/DiagnosticContextTests.cs` verifying disposed contexts raise a `context/detail` event with captured values.

### Implementation for User Story 2

- [ ] T019 [US2] Implement dump builders and serializers in `src/Html2x.Diagnostics/Dumps/StructuredDumpSerializer.cs` and integrate with stage publishers.
- [ ] T020 [US2] Implement `DiagnosticContext` to emit dedicated events in `src/Html2x.Diagnostics/Context/DiagnosticContext.cs` and ensure shrink-to-fit data is recorded.
- [ ] T021 [US2] Extend `specs/002-diagnostics-framework/quickstart.md` and `docs/diagnostics.md` with examples for dumps and context events.

**Checkpoint**: User Story 2 delivers structured dumps and reasoning metadata with deterministic identifiers.

---

## Phase 5: User Story 3 – Pluggable Observers and Sinks (Priority: P3)

**Goal**: Provide JSON and console sinks plus extension points (including optional in-memory sink evaluation) for partner integrations.  
**Independent Test**: JSON sink writes canonical payloads to disk; console sink mirrors the same events; optional in-memory sink enables deterministic assertions without I/O.

### Tests for User Story 3

- [ ] T022 [US3] Add failing JSON sink contract test `tests/Html2x.Test/Diagnostics/SinkContractTests.cs` asserting persisted payloads contain sessions, events, dumps, reasoning, and raw field values with no redaction.
- [ ] T023 [P] [US3] Add failing console sink test `tests/Html2x.Test/Diagnostics/ConsoleSinkTests.cs` ensuring console output mirrors JSON payload ordering.
- [ ] T024 [US3] Add exploratory test `tests/Html2x.Test/Diagnostics/InMemorySinkTests.cs` to determine if an in-memory sink is required for deterministic assertions.
### Implementation for User Story 3

- [ ] T025 [US3] Implement `JsonDiagnosticSink` with deterministic serialization in `src/Html2x.Diagnostics/Sinks/JsonDiagnosticSink.cs` and configuration hooks.
- [ ] T026 [US3] Implement `ConsoleDiagnosticSink` emitting human-readable stage timelines in `src/Html2x.Diagnostics/Sinks/ConsoleDiagnosticSink.cs`.
- [ ] T027 [US3] Prototype `InMemoryDiagnosticSink` (if justified by T024) for unit-test assertions in `src/Html2x.Diagnostics/Sinks/InMemoryDiagnosticSink.cs`.
- [ ] T028 [US3] Wire sink registration via `DiagnosticsOptions` so the existing `--diagnostics` flag in `src/Html2x.TestConsole/diagnostics/run-diagnostics-json.ps1` enables the selected sinks without introducing new toggles.
- [ ] T029 [US3] Update `docs/diagnostics.md` and `quickstart.md` with sink configuration matrices, console diff instructions, and guidance on sanitizing sinks when raw payloads contain sensitive data.

**Checkpoint**: User Story 3 equips Html2x with pluggable sinks plus documentation for partners and console users.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Ensure documentation, release readiness, and regression coverage across all stories.

- [ ] T030 Run `dotnet test Html2x.sln -c Release` and `dotnet run --project src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj -- --input sample.html --diagnostics` capturing outputs in `build/diagnostics/final.md`.
- [ ] T031 [P] Consolidate release notes and migration guidance in `docs/diagnostics.md` and `specs/002-diagnostics-framework/spec.md`.
- [ ] T033 Record follow-up work for SVG visualization and PDF metadata sinks by adding a backlog entry to `specs/002-diagnostics-framework/research.md` and cross-referencing it in `docs/diagnostics.md`.

---

## Dependencies & Execution Order

- **Phase Dependencies**: Setup → Foundational → US1 → US2 → US3 → Polish. Each later phase depends on all earlier ones.
- **User Story Dependencies**: US1 is the MVP and must complete before US2/US3. US2 (dumps/contexts) depends on session infrastructure from US1. US3 (sinks) depends on event payloads from US1/US2.
- **Task Dependencies**: Within each story, failing tests (e.g., T011, T012) must be written before their implementation counterparts (e.g., T013, T014). Documentation tasks (T016, T021, T029, T031, T033) depend on preceding functionality completing.

---

## Parallel Execution Examples

- **Setup**: T001–T003 can run concurrently once repo cloned (marked [P] where applicable).
- **Foundational**: T005, T007, and T009 can proceed in parallel after T004 creates the project skeleton (tests build on artifacts from T008), while T010 runs once diagnostics wiring solidifies.
- **US1**: T012 can be authored in parallel with T011 since they target different test files; once tests exist, T014 and T015 can proceed concurrently (different files) after T013 lands.
- **US2**: T018 (context test) can be developed while T017 focuses on dumps, enabling T019 and T020 to progress in tandem once tests are red.
- **US3**: JSON and console sink implementation tasks (T025, T026) can run simultaneously after their respective tests (T022, T023) exist; T027 depends on the outcome of T024, and documentation/backlog tasks follow once sinks stabilize.

---

## Implementation Strategy

- **MVP Scope**: Complete Phases 1–3 (Setup, Foundational, User Story 1). This delivers opt-in diagnostics sessions with deterministic events and console wiring.
- **Incremental Delivery**:
  1. Finish MVP (US1) and validate zero-cost disabled state.
  2. Layer US2 to capture structured dumps and contexts, ensuring deterministic identifiers.
  3. Deliver US3 sinks plus documentation, enabling partner integrations.
- **Testing Cadence**: For every task marked as a failing test, keep the suite red until the paired implementation task runs. Re-run `dotnet test Html2x.sln -c Release` after each story to ensure determinism.








