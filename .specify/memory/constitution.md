<!--
Sync Impact Report
Version change: 1.0.0 -> 1.1.0
Modified principles: III. Test-First Delivery (expanded with incremental TDD, behavior-focused testing, reflection prohibition, isolation requirements)
Added sections: none
Removed sections: none
Templates requiring updates:
- updated: .specify/templates/plan-template.md
- updated: .specify/templates/spec-template.md
- updated: .specify/templates/tasks-template.md
Follow-up TODOs:
- (resolved) Ratification date documented: 2025-11-06
-->
# Html2x Constitution

## Core Principles

### I. Staged Layout Discipline
- Pipeline stages (DOM, style, box, fragment, renderer) MUST communicate only through contracts defined in `Html2x.Core`.
- Layout and rendering code MUST NOT reach back to DOM or style types once a stage hands off control.
- Cross-assembly dependencies MUST flow in one direction: Core -> Layout -> Renderers.
Rationale: Preserving stage isolation keeps the system modular, testable, and enables new renderers without regressions.

### II. Deterministic Rendering Outputs
- Identical HTML, CSS, options, and fonts MUST produce equivalent outputs across platforms.
- All randomness, system clock access, and environment-specific dependencies MUST be eliminated or sealed behind deterministic abstractions.
- Test suites MUST assert fragment equivalence or semantic PDF parity for every new feature.
Rationale: Deterministic rendering underpins regression safety and cross-platform parity.

### III. Test-First Delivery
- Tests are first-class and MUST focus on observable behavior, not implementation details.
- Code MUST follow incremental TDD: introduce one failing test, implement the minimal passing code, then refactor before the next test. Trivial scaffolding (constructors, simple properties, passive DTOs) is exempt.
- Tests MUST exercise outcomes such as rendered output, pagination results, logging, or API responses and MUST NOT rely on reflection-based contract checks.
- Reflection APIs (e.g., `Activator.CreateInstance`, `Type.GetType`, `MethodInfo.Invoke`) are prohibited in test code.
- Prioritize tests for business logic and complex flows, use parameterized tests for multi-scenario logic, and keep tests independent and readable.
- Each layer/module MUST be testable in isolation.
- Unit, integration, and scenario tests MUST reside with the affected module and run via `dotnet test Html2x.sln -c Release`.
- No feature merges without green tests and documented coverage of the exercised path.
Rationale: Incremental TDD with behavior-focused tests keeps the pipeline verifiable, prevents silent regressions, and maintains test independence across layers.

### IV. Instrumented Observability
- Layout and rendering stages MUST emit structured logs using the shared logging helpers with context-rich metadata.
- Diagnostic toggles (recorders, PDF inspectors) MUST remain available in test consoles for every new capability.
- Production faults MUST be traceable via correlatable log events without attaching a debugger.
Rationale: Instrumentation provides auditability, accelerates triage, and keeps the system supportable.

### V. Intentional Extensibility
- New CSS properties, fragment types, or render targets MUST extend the shared contracts before reaching feature code.
- Extension points MUST include documentation updates in `docs/` covering intent, usage, and failure modes.
- Breaking changes MUST provide migration guidance and version notes before release.
Rationale: Deliberate extensibility avoids ad hoc growth and protects downstream consumers.

## Operational Guardrails

- Target framework MUST remain `net8.0`; deviations require architecture review and migration plan.
- Third-party dependencies MUST be managed through NuGet packages approved for pure .NET usage.
- Fonts, HTML samples, and generated artifacts MUST live under tracked directories (`fonts/`, `html/`, `build/`) with provenance documented.
- CLI smoke tests (`Html2x.Pdf.TestConsole`) MUST be runnable on Windows and Linux without extra tooling.

## Delivery Workflow

- Feature work MUST start with a plan derived from `/speckit.plan` and align with constitution gates before research begins.
- Specifications MUST enumerate independent user stories, deterministic acceptance criteria, and observability requirements.
- Task breakdowns MUST keep user stories independently deliverable, call out test-first steps, and include logging tasks where applicable.
- Feature completion MUST include documentation updates, release notes, and verification that console smoke tests and `dotnet test` succeed.

## Governance

- Amendments require documented rationale, impact assessment, and approval from the Html2x maintainers.
- Constitution versions follow semantic versioning: MAJOR for breaking governance changes, MINOR for new sections or expanded mandates, PATCH for clarifications.
- Compliance reviews MUST accompany feature PRs, referencing the relevant principles in the plan checklist.
- Maintain a TODO register inside this constitution for unresolved data (e.g., ratification date) and track closure in subsequent amendments.

**Version**: 1.1.0 | **Ratified**: 2025-11-06 | **Last Amended**: 2025-11-06
