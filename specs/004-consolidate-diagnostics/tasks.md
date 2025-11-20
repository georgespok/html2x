# Tasks: Diagnostics Session Envelope

**Input**: Design documents from `/specs/004-consolidate-diagnostics/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md

**Tests**: Maintain the single-failing-test loop from Principle III. Each story lists failing test tasks before implementation so the suite goes red → green incrementally.

**Goal-Driven Cadence**: Every story captures State Assessment, Action Decomposition, Path Planning (dependencies), Adaptive Execution checkpoints, and Reflection hooks to satisfy Principle VI.

**Organization**: Tasks are grouped by user story for independent delivery and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]** marks tasks that can proceed in parallel.
- **[Story]** denotes the owning user story (US1, US2, US3). Setup/Foundational/Polish tasks omit the story label.
- Include exact file paths so executors know where to work.

## Path Conventions

- Source: `src/Html2x.*`
- Tests: `tests/Html2x.*`
- Console harness: `src/Tests/Html2x.TestConsole/`
- Docs: `docs/`, `specs/004-consolidate-diagnostics/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm baseline repo health and guardrails before refactoring diagnostics.

- [X] T001 Run `dotnet restore Html2x.sln` and record dependency status in `specs/004-consolidate-diagnostics/plan.md`.
- [X] T002 Execute `dotnet test Html2x.sln -c Release` to capture the pre-change baseline in `specs/004-consolidate-diagnostics/research.md`.
- [X] T003 [P] Review `.editorconfig` and `Directory.Build.props` to ensure analyzer/formatting rules are understood before modifying diagnostics files.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared contracts and serializer hooks used by every story.

- [X] T004 Add the `DiagnosticsSessionEnvelope` DTO to `src/Html2x.Abstractions/Diagnostics/Contracts/DiagnosticsSessionEnvelope.cs`.
- [X] T005 [P] Create `src/Html2x.Diagnostics/Serialization/DiagnosticsSessionEnvelopeSerializer.cs` with System.Text.Json options for deterministic ordering.
- [X] T006 Update `specs/004-consolidate-diagnostics/data-model.md` and `docs/diagnostics-snapshot-plan.md` with the finalized envelope schema and lifecycle diagram.

**Checkpoint**: Contracts + serializer ready for story work.

---

## Phase 3: User Story 1 - Single Session Capture (Priority: P1)  MVP

**Goal**: Emit exactly one diagnostics session envelope per pipeline run with ordered events and shared metadata.  
**Independent Test**: `DiagnosticsSessionEnvelopeTests.RunProducesSingleDocument` fails first, then passes once a single envelope is emitted per run.  
**State Assessment**: Current runtime writes StructuredDump per event, duplicating metadata. Need a session-scoped collector to buffer events, emit lifecycle metrics, and flush once.  
**Action Decomposition**: Add failing unit/integration tests → implement collector + instrumentation → update diagnostics wiring → document schema usage.  
**Path Planning & Risks**: Dependency on foundational DTO/serializer; monitor collector wiring changes via git history for recovery if needed.  
**Adaptive Checkpoints**: After tests go green, run console harness to confirm only one artifact per run; re-evaluate if any stage misses diagnostics.  
**Reflection Hook**: Capture collector patterns and diagnostic insights in `specs/004-consolidate-diagnostics/research.md`.

### Tests for User Story 1

- [ ] T007 [US1] Add failing `RunProducesSingleDocument` test in `tests/Html2x.Test/Diagnostics/DiagnosticsSessionEnvelopeTests.cs` asserting a single persisted envelope per pipeline run.
- [ ] T008 [P] [US1] Extend `src/Tests/Html2x.TestConsole/Scenarios/DiagnosticsSessionEnvelopeScenario.cs` to fail when multiple JSON files are produced for one execution.
- [ ] T009 [US1] Extend `tests/Html2x.Test/Diagnostics/DiagnosticsSessionEnvelopeTests.cs` to assert lifecycle metrics/logs for collector initialization, event acceptance, and flush outcomes.
- [ ] T010 [US1] Add failing timestamp completeness assertions in `tests/Html2x.Test/Diagnostics/DiagnosticsSessionEnvelopeTests.cs` to enforce start, last-event, and end timestamps on every envelope.
- [ ] T011 [US1] Add failing zero-event scenario test in `tests/Html2x.Test/Diagnostics/DiagnosticsSessionEnvelopeTests.cs` proving envelopes emit even when no stage logs events.
- [ ] T012 [US1] Add failing partial-metadata scenario in `tests/Html2x.Test/Diagnostics/DiagnosticsSessionEnvelopeTests.cs` ensuring missing environment fields trigger warning events.
- [ ] T013 [P] [US1] Add long-running session stress scenario in `src/Tests/Html2x.TestConsole/Scenarios/DiagnosticsLongRunScenario.cs` to verify ordering is preserved for thousands of events.
- [ ] T014 [US1] Add clock-drift simulation test in `tests/Html2x.Test/Diagnostics/DiagnosticsSessionEnvelopeTests.cs` validating monotonic ordering independent of system time jumps.

### Implementation for User Story 1

- [ ] T015 [US1] Emit lifecycle metrics in `src/Html2x.Diagnostics/Runtime/DiagnosticsSessionCollector.cs` and related runtime files to satisfy the new test coverage.
- [ ] T016 [US1] Implement the buffering logic in `src/Html2x.Diagnostics/Runtime/DiagnosticsSessionCollector.cs` to capture metadata, enforce ordering, and hydrate envelope events.
- [ ] T017 [US1] Wire the collector through `src/Html2x/HtmlConverter.cs` and `src/Html2x.LayoutEngine/LayoutBuilder.cs` so every stage publishes events via the collector instead of StructuredDump.
- [ ] T018 [US1] Emit lifecycle diagnostics (start, stage transition, completion) from `src/Html2x.Diagnostics/Runtime/DiagnosticSession.cs` to seed envelope metadata.
- [ ] T019 [P] [US1] Document the new envelope workflow, lifecycle metrics, edge-case expectations, and sample JSON in `docs/diagnostics-snapshot-plan.md` and `specs/004-consolidate-diagnostics/quickstart.md`.

**Checkpoint**: US1 delivers a single envelope artifact with deterministic ordering, lifecycle metrics, and updated docs.

---

## Phase 4: User Story 2 - Failure Flush Discipline (Priority: P2)

**Goal**: Ensure the collector flushes the envelope (with final timestamps and failure events) even when a stage throws.  
**Independent Test**: `DiagnosticsSessionEnvelopeTests.FailureFlushesEnvelope` fails first, then passes when failure paths persist the envelope.  
**State Assessment**: Failures currently leave partial dumps; we need failure hooks plus retry-aware persistence.  
**Action Decomposition**: Author failing tests → implement `collector.Fail` code paths → propagate failure events from pipeline/renderer → document failure-handling guidance.  
**Path Planning & Risks**: Depends on US1 collector; risk of double-flush mitigated via state machine guard.  
**Adaptive Checkpoints**: After failure tests pass, run console harness with forced exception; if envelope missing failure event, pause to inspect wiring.  
**Reflection Hook**: Log failure-mode learnings in `specs/004-consolidate-diagnostics/research.md`.

### Tests for User Story 2

- [ ] T020 [US2] Add failing `FailureFlushesEnvelope` test to `tests/Html2x.Test/Diagnostics/DiagnosticsSessionEnvelopeTests.cs` asserting error payload + final timestamp.
- [ ] T021 [P] [US2] Create `src/Tests/Html2x.TestConsole/Scenarios/DiagnosticsFailureScenario.cs` that forces a renderer exception and asserts the envelope file contains failure metadata.
- [ ] T022 [US2] Add failing serialization-retry test in `tests/Html2x.Test/Diagnostics/DiagnosticsSessionEnvelopeTests.cs` that simulates sink write failures and expects retries plus terminal error logs.

### Implementation for User Story 2

- [ ] T023 [US2] Implement `Fail` handling in `src/Html2x.Diagnostics/Runtime/DiagnosticsSessionCollector.cs` to append failure events, mark status, and flush once.
- [ ] T024 [US2] Route exceptions through the collector in `src/Html2x/HtmlConverter.cs` and `src/Html2x.Renderers.Pdf/Pipeline/PdfRenderer.cs` so failures trigger `collector.Fail`.
- [ ] T025 [US2] Add serialization retry/backoff and final error emission in `src/Html2x.Diagnostics/Sinks/FileDiagnosticsSink.cs` (and related sinks) to satisfy the new tests.
- [ ] T026 [P] [US2] Capture failure/retry procedures and diagnostics expectations in `docs/diagnostics-snapshot-plan.md`.

**Checkpoint**: US2 guarantees envelopes exist for both success and failure paths.

---

## Phase 5: User Story 3 - Downstream Availability and Audit (Priority: P3)

**Goal**: Storage and telemetry sinks emit only the new hierarchical schema so auditors consume one document per run.  
**Independent Test**: Sink contract tests fail until only `DiagnosticsSessionEnvelope` JSON is written/broadcast.  
**State Assessment**: Existing sinks stream StructuredDump entries; consumers would need to stitch data. Need sink updates + contract docs.  
**Action Decomposition**: Write failing sink tests → update file/console/telemetry sinks → refresh quickstart + downstream docs.  
**Path Planning & Risks**: Depends on US1+US2 to provide reliable envelope objects; risk of breaking legacy tools mitigated by zero external consumers.  
**Adaptive Checkpoints**: After sink tests pass, point telemetry harness to verify only new schema messages flow; re-evaluate if subscribers observe mixed formats.  
**Reflection Hook**: Capture telemetry consumer feedback in `specs/004-consolidate-diagnostics/research.md`.

### Tests for User Story 3

- [ ] T027 [US3] Add failing sink contract test in `tests/Html2x.Test/Diagnostics/SinkContractTests.cs` asserting only `DiagnosticsSessionEnvelope` payloads persist.
- [ ] T028 [P] [US3] Update `tests/Html2x.Test/Diagnostics/ConsoleSinkTests.cs` to fail when any StructuredDump format is emitted.

### Implementation for User Story 3

- [ ] T029 [US3] Update `src/Html2x.Diagnostics/Sinks/FileDiagnosticsSink.cs` to persist one envelope JSON per run and delete StructuredDump writers.
- [ ] T030 [US3] Update `src/Html2x.Diagnostics/Sinks/ConsoleDiagnosticsSink.cs` and telemetry sinks to broadcast only the hierarchical envelope payload.

**Checkpoint**: US3 ensures all sinks and docs present the new schema exclusively.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T031 Run `dotnet test Html2x.sln -c Release` and attach logs to `specs/004-consolidate-diagnostics/plan.md` as final verification.
- [ ] T032 [P] Update release notes in `docs/diagnostics-snapshot-plan.md` and add migration summary to `specs/004-consolidate-diagnostics/spec.md`.
- [ ] T033 Capture reflection notes and follow-ups in `specs/004-consolidate-diagnostics/research.md`.

---

## Dependencies & Story Order

| Order | Story | Depends On |
|-------|-------|------------|
| 1 | US1 Single Session Capture | Setup + Foundational |
| 2 | US2 Failure Flush Discipline | US1 |
| 3 | US3 Downstream Availability and Audit | US1 + US2 |

---

## Parallel Execution Examples

- **US1**: After T007 lands, T008–T014 can run in parallel with T015/T016 because they touch TestConsole/tests/docs while core collector work proceeds.  
- **US2**: T021 and T022 can execute while T023/T024 progress since the former are harness/unit tests and the latter touch runtime code.  
- **US3**: T027 can run alongside T028 because it exercises test code while sink implementations (T029/T030) change separately.

---

## Implementation Strategy

1. Ship MVP with US1 completing the single-envelope collector, lifecycle metrics, and documentation.  
2. Layer US2 failure handling to guarantee envelopes exist for outages.  
3. Finish with US3 sink + telemetry updates and polish to prepare downstream consumers.  
4. Maintain TDD cadence: each failing test task precedes the implementation task that turns it green.  
5. Use `dotnet test Html2x.sln -c Release` and Html2x.TestConsole harness checkpoints after every story.
