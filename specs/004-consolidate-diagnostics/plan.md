# Implementation Plan: Diagnostics Session Envelope

**Branch**: `004-consolidate-diagnostics` | **Date**: November 20, 2025 | **Spec**: `specs/004-consolidate-diagnostics/spec.md`  
**Input**: Feature specification from `/specs/004-consolidate-diagnostics/spec.md`

## Summary

Create a single `DiagnosticsSessionEnvelope` per Html2x pipeline run that aggregates shared session metadata plus ordered `DiagnosticEvent` entries, replaces the legacy StructuredDump artifacts, and ensures storage/telemetry sinks emit only the hierarchical schema with full payload contents. The design introduces a session-scoped collector, updated serializers, and documentation/tests that validate run-level uniqueness, failure flushes, and downstream availability.

## Technical Context

**Language/Version**: .NET 8 (entire solution already targets net8.0).  
**Primary Dependencies**: Html2x.Diagnostics runtime & abstractions, Html2x pipeline orchestration, QuestPDF renderer pipeline, System.Text.Json for envelope serialization.  
**Storage**: Persist envelopes via existing diagnostics sinks (file, console, telemetry) that already support structured payloads; no new persistence tier introduced.  
**Testing**: xUnit via `dotnet test Html2x.sln -c Release`, covering diagnostics runtimes, sinks, and integration harness.  
**Target Platform**: Cross-platform (Windows, Linux, CI agents) with identical diagnostics semantics.  
**Project Type**: Modular library (`src/Html2x.*`) with Html2x.TestConsole harness for manual verification.  
**Performance Goals**: n/a (not tracked for this diagnostics-only feature).  
**Constraints**: Maintain managed-code boundary, respect staged layout contracts, and avoid touching renderer internals once diagnostics are emitted.  
**Scale/Scope**: Impacts `Html2x.Diagnostics`, `Html2x.Abstractions`, `Html2x`, `Html2x.LayoutEngine`, `Html2x.Renderers.Pdf`, and associated test projects; sessions typically include 50-500 events but must safely handle spikes to thousands.  
**Dependency Status (T001)**: `dotnet restore src/Html2x.sln` on 2025-11-21 completed successfully; all projects reported up-to-date.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] Stage isolation maintained at assembly boundaries (Principle I: Staged Layout Discipline) — diagnostics collector lives in `Html2x.Diagnostics` and surfaces DTOs through `Html2x.Abstractions`.
- [x] Rendering predictability risks documented with `Html2x.Diagnostics` coverage instead of PDF parsing (Principle II) — envelope provides deterministic ordering plus lifecycle timestamps.
- [x] TDD approach defined, explicitly sequencing one failing test at a time (introduce a single failing test, implement minimal pass, then refactor) per Principle III — plan enumerates failing tests for single-envelope emission, failure flush, and telemetry persistence.
- [x] `Html2x.Diagnostics` instrumentation scoped for new behavior (Principle IV) — instrumentation tasks cover collector lifecycle events and sink serialization.
- [x] Extension points documented with migration guidance (Principle V) — documentation/quickstart will explain schema and highlight removal of StructuredDump.
- [x] Goal-Driven Problem Solving loop captured (Principle VI: state assessment, action decomposition, path planning, adaptive execution, and reflection) — summary plus subsequent sections record current state, ordered steps, and reflection checkpoints.

**Post-Design Recheck (Phase 1 Complete)**: All gates remain satisfied; envelope schema, contracts, and data models uphold staged isolation, diagnostics coverage, and documentation/migration expectations.

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
    Html2x.LayoutEngine/
    Html2x.Renderers.Pdf/
    Html2x/
    Html2x.Diagnostics/
tests/
    Html2x.LayoutEngine.Test/
    Html2x.Renderers.Pdf.Test/
    html2x.Test/
src/Html2x.TestConsole/
```

**Structure Decision**: Update diagnostics abstractions (`src/Html2x.Abstractions/Diagnostics`), runtime collector (`src/Html2x.Diagnostics`), pipeline orchestration (`src/Html2x`), layout/renderers (`src/Html2x.LayoutEngine`, `src/Html2x.Renderers.Pdf`) to emit the new envelope while keeping existing sinks under `src/Html2x.Diagnostics/Sinks`. Documentation and spec artifacts live under `specs/004-consolidate-diagnostics/` with supporting research, plan, data model, quickstart, and tasks subfolders per template guidance.

## Complexity Tracking

> Fill ONLY if Constitution Check has violations that must be justified.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|---------------------------------------|
| | | |
