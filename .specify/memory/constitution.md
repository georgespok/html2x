<!--
Sync Impact Report
Version change: 2.1.0 -> 2.2.0
Modified principles:
- Delivery Workflow (added HTML sample requirement)
Added sections:
- VII. Accessible Documentation & Specification
Removed sections:
- none
Templates requiring updates:
- ✅ .specify/templates/plan-template.md
- ✅ .specify/templates/spec-template.md
- ✅ .specify/templates/tasks-template.md
Follow-up TODOs:
- none
-->
# Html2x Constitution

## Core Principles

### I. Staged Layout Discipline
- DOM, style, box, and fragment stages may share internal data models inside `Html2x.LayoutEngine`, but any cross-project communication MUST use contracts in `Html2x.Abstractions`.
- Layout and rendering code MUST NOT reach back to DOM or style types once a stage hands off control outside the layout assembly.
- Cross-assembly dependencies MUST flow in one direction: `Html2x.Abstractions` -> `Html2x.LayoutEngine` -> `Html2x.Renderers.*`.

Rationale: Preserving isolation at assembly boundaries keeps the system modular, testable, and enables new renderers without regressions.

### II. Rendering Predictability & Diagnostics
- Equivalent HTML, CSS, options, and fonts SHOULD converge to the same fragment semantics; small platform-specific differences are acceptable when captured through diagnostics.
- Sources of nondeterminism (clocks, randomness, environment probes) MUST be isolated behind configuration seams or `Html2x.Diagnostics` instrumentation so behavior can be measured, even if full elimination is deferred.
- Automated tests MUST assert fragment contracts or diagnostics traces; PDF binaries remain a black box and MUST NOT be parsed for assertions.

Rationale: Diagnostics-first predictability surfaces regressions without over-investing in byte-for-byte parity.

### III. Test-First Delivery
- Tests are first-class and MUST focus on observable behavior, not implementation details.
- Plans and task lists MUST enforce the incremental TDD loop: introduce exactly one failing test, implement the minimal passing change, refactor, then document the next test. Trivial scaffolding (constructors, simple properties, passive DTOs) is exempt.
- Tests MUST exercise outcomes such as rendered output, pagination results, diagnostics captures, or consumer-visible public classes and MUST NOT rely on reflection-only contract checks.
- Reflection namespaces (e.g., `System.Reflection` activation helpers such as `Activator.CreateInstance`, `Type.GetType`, `MethodInfo.Invoke`) are prohibited in test code.
- Prioritize tests for business logic and complex flows, use parameterized tests for multi-scenario logic, and keep tests independent and readable.
- Each layer/module MUST be testable in isolation.
- Unit, integration, and scenario tests MUST reside with the affected module and run via `dotnet test Html2x.sln -c Release`.
- No feature merges without green tests and documented coverage of the exercised path.

Rationale: Incremental TDD with behavior-focused tests keeps the pipeline verifiable, prevents silent regressions, and maintains test independence across layers. Aligning plans with the single-failing-test loop protects this discipline.

### IV. Diagnostic Observability
- Layout and rendering stages MUST emit `Html2x.Diagnostics` traces, payloads, and timelines rich enough to explain their behavior.
- Diagnostic toggles (recorders, inspectors, sampling controls) MUST remain available in the test console for every new capability.
- Production faults MUST be triaged via `Html2x.Diagnostics` artifacts without attaching a debugger.

Rationale: Diagnostics provide auditability, accelerate triage, and keep the system supportable without bespoke logging stacks.

### V. Intentional Extensibility
- New CSS properties, fragment types, or render targets MUST extend the shared contracts before reaching feature code.
- Extension points MUST include documentation updates in `docs/` covering intent, usage, and failure modes.
- Breaking changes MUST provide migration guidance and version notes before release.

Rationale: Deliberate extensibility avoids ad hoc growth and protects downstream consumers.

### VI. Goal-Driven Delivery Cadence
- Plans, specs, and tasks MUST document the Goal-Driven Problem Solving loop: State Assessment, Action Decomposition, Path Planning, Adaptive Execution, and Reflection Loop.
- Every delivery artifact MUST capture explicit state transitions (e.g., current coverage -> failing test -> passing refactor) and enumerate assumptions/dependencies before irreversible work begins.
- Teams MUST log reflection notes and reusable patterns at the end of each increment so subsequent work starts with updated context.

Rationale: Making the delivery loop explicit keeps reasoning transparent, surfaces risk early, and accelerates adaptation when assumptions fail.

### VII. Accessible Documentation & Specification
- Specifications, plans, and task descriptions MUST be written in plain, simple English with sufficient detail and examples to be understood by a junior developer without prior context.
- Key architectural or logic points MUST be illustrated with short code sketches or diagrams.
- Documentation MUST avoid jargon where simple explanations suffice.

Rationale: Clear communication reduces ambiguity and ensures that implementation details are accessible to all contributors regardless of seniority.

## Operational Guardrails

- Target framework MUST remain `net8.0`; deviations require architecture review and migration plan.
- Third-party dependencies MUST be managed through NuGet packages approved for pure .NET usage.
- Fonts, HTML samples, and generated artifacts MUST live under tracked directories (`fonts/`, `html/`, `build/`) with provenance documented.
- CLI smoke tests (project `src/Html2x.TestConsole/Html2x.TestConsole.csproj`) MUST be runnable on Windows and Linux without extra tooling.

## Delivery Workflow

- Feature work MUST start with a plan derived from `/speckit.plan` and align with constitution gates before research begins.
- Plans, specs, and tasks MUST make the Goal-Driven loop explicit with ordered state transitions, dependency/assumption tracking, and scheduled reflection checkpoints (Principle VI).
- Specifications MUST enumerate independent user stories, predictability-focused acceptance criteria, and `Html2x.Diagnostics` requirements.
- Task breakdowns MUST keep user stories independently deliverable, call out test-first steps, and include diagnostics instrumentation tasks where applicable.
- Feature completion MUST include documentation updates, release notes, verification that console smoke tests and `dotnet test` succeed, AND a new HTML file in `src/Tests/Html2x.TestConsole/html/` demonstrating the feature.

## Governance

- Amendments require documented rationale, impact assessment, and approval from the Html2x maintainers.
- Constitution versions follow semantic versioning: MAJOR for breaking governance changes, MINOR for new sections or expanded mandates, PATCH for clarifications.
- Compliance reviews MUST accompany feature PRs, referencing the relevant principles in the plan checklist.
- Maintain a TODO register inside this constitution for unresolved data (e.g., ratification date) and track closure in subsequent amendments.

**Version**: 2.2.0 | **Ratified**: 2025-11-06 | **Last Amended**: 2025-11-25