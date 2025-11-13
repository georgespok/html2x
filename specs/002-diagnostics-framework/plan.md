# Implementation Plan: Diagnostics Framework

**Branch**: `002-diagnostics-framework` | **Date**: November 13, 2025 | **Spec**: `specs/002-diagnostics-framework/spec.md`  
**Input**: Feature specification from `/specs/002-diagnostics-framework/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Introduce `Html2x.Diagnostics`, an opt-in instrumentation layer that captures stage events, structured dumps, reasoning contexts, and sink outputs without modifying production behavior when disabled. Core projects remain unaware of diagnostics; the Html2x facade injects the subsystem, exposes synchronous observer hooks, and provides JSON/console sinks plus extension points for partners.

## Technical Context

**Language/Version**: .NET 8 (override if feature targets a different framework)  
**Primary Dependencies**: AngleSharp, QuestPDF, Html2x shared libraries  
**Storage**: In-memory unless the feature introduces persistence (document rationale)  
**Testing**: xUnit via `dotnet test Html2x.sln -c Release`  
**Target Platform**: Windows and Linux  
**Project Type**: Modular library (`src/Html2x.*`) with test console harness  
**Performance Goals**: Diagnostics disabled state executes zero instrumentation; enabled sessions must stream events synchronously without exceeding existing render timeouts.  
**Constraints**: Keep implementation pure managed code; no platform-specific APIs without maintainer approval; core assemblies must not depend on diagnostics at compile time.  
**Scale/Scope**: Touches `src/Html2x` facade, `Html2x.Abstractions` contracts, new `Html2x.Diagnostics` project, and `Html2x.TestConsole`. Expected renders span 10–200 pages; diagnostics buffering must sustain at least 5 concurrent sessions in-process.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] Stage isolation maintained (Principle I: diagnostics hooks live behind facade contracts, no cross-stage leaks).
- [x] Deterministic rendering risks addressed with best-effort tests or instrumentation inside the Html2x reference environment (Principle II: diagnostics on/off stage-order checks, fragment counts, and documented variances when byte-level parity is impractical).
- [x] TDD approach defined, explicitly sequencing one failing test at a time (each stage adds a failing diagnostics test before implementation, then refactors).
- [x] Logging and diagnostics updates planned (Principle IV: JSON/console sinks plus TestConsole wiring ensure observability).
- [x] Extension points documented with migration guidance (Principle V: new contracts under `Html2x.Abstractions` and docs/diagnostics.md updates).

## Project Structure

### Documentation (this feature)

```
specs/[###-feature]/
    plan.md
    research.md
    data-model.md
    quickstart.md
    tasks.md
```

### Source Code (repository root)

```
src/
    Html2x.Core/
    Html2x.Layout/
    Html2x.Pdf/
    Html2x/
tests/
    Html2x.Layout.Test/
    Html2x.Pdf.Test/
    html2x.IntegrationTest/
src/Html2x.TestConsole/
```

**Structure Decision**: Add `src/Html2x.Diagnostics/` for instrumentation, new abstractions in `src/Html2x.Abstractions/Diagnostics`, and sample wiring plus scripts under `src/Html2x.TestConsole/diagnostics`. No existing project references diagnostics directly; the facade composes it at runtime.

## Complexity Tracking

> Fill ONLY if Constitution Check has violations that must be justified.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|---------------------------------------|
| | | |

## Phase 0 – Outline & Research

1. Catalog diagnostics scope gaps (session lifecycle injection, synchronous sink isolation, InMemory sink needs) and answer them in `research.md`.
2. Validate opt-in wiring patterns for .NET libraries that must keep core assemblies unaware of diagnostics.
3. Capture best practices for structured observability payloads and sensitive-data handling (decision: always capture raw, sinks sanitize).

**Deliverable**: `specs/002-diagnostics-framework/research.md`

## Phase 1 – Design & Contracts

1. Derive entity definitions (DiagnosticSession/Event/Context/Sink/StructuredDump) in `data-model.md`, including lifecycle diagrams for session start/stop and context scoping.
2. Capture the facade/builder APIs directly in `quickstart.md`, covering session creation, context logging, sink registration, and console scripts.
3. Draft `quickstart.md` instructing consumers how to enable diagnostics, register sinks, and run console harness assertions.
4. Run `.specify/scripts/powershell/update-agent-context.ps1 -AgentType codex` to capture plan-specific tech notes.
5. Re-check Constitution gates post-design; all remain satisfied since design keeps stage isolation, deterministic flows, and observability coverage.

**Deliverables**: `data-model.md`, updated `quickstart.md`, updated agent context.

## Phase 2 – Implementation Sequencing

1. Define step-by-step coding phases for `Html2x.Diagnostics`: session manager, context API, synchronous observer pipeline, built-in JSON/console sinks, optional in-memory sink for tests.
2. Record integration points for Html2x facade injection and TestConsole wiring.
3. Enumerate test strategy aligning with TDD mandate (per-stage failing tests, sink contract tests, console harness verification).

**Exit Criteria**: Plan ready for `/speckit.tasks`; no unresolved clarifications or constitution violations.



