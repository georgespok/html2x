# Feature Specification: Diagnostics Framework

**Feature Branch**: `[002-diagnostics-framework]`  
**Created**: November 13, 2025  
**Status**: Draft  
**Input**: Diagnostics Framework - optional subsystem for structured, extensible, pluggable insight into style computation, box layout, inline measurement, fragmentation, pagination, and PDF rendering. Delivered as `Html2x.Diagnostics` with a clean API, event reporting, observers, structured dumps, and sinks (JSON and console now; SVG visualization and in-PDF metadata later) that stay inert when not enabled.

## Clarifications

### Session 2025-11-13

- Q: How should diagnostics handle sensitive data inside events, dumps, or contexts? → A: Always capture full unredacted content; downstream sinks are responsible for any sanitization.

## User Scenarios & Testing (mandatory)

Constitution alignment: Each story keeps instrumentation layered atop existing style, layout, and rendering stages (Principle I), delivers best-effort identical outputs for equal inputs in the Html2x reference environment (documenting any unavoidable variance per Principle II), and requires deterministic hooks that the harness can assert (Principle IV).

### User Story 1 - Toggleable Diagnostics Session (Priority: P1)

An observability engineer enables diagnostics for a single Html2x render run to see stage-level events alongside the engine’s internal reasoning, calculations, and warnings, while routine runs stay unaffected when diagnostics are off.

**Why this priority**: We cannot ship any instrumentation until we prove zero-cost off-state behavior and scoped activation for sensitive tenants.

**Independent Test**: Extend `Html2x.TestConsole` with a failing test that renders a sample document twice—once without diagnostics, once with a diagnostics session—and asserts the disabled run emits zero diagnostics events while the enabled run captures the ordered stage events.

**Acceptance Scenarios**:

1. **Given** Html2x runs with diagnostics disabled by default, **When** a document renders without creating a diagnostics session, **Then** no diagnostics events, observers, or sinks are created and the render completes exactly as it does today.
2. **Given** a diagnostics session created through the new API, **When** the same HTML renders, **Then** the system emits ordered start and end events for style computation, box layout, inline measurement, fragmentation, pagination, and PDF rendering with monotonic timestamps stored in the session timeline.

---

### User Story 2 - Stage Insights and Structured Dumps (Priority: P2)

A Html2x developer inspects a failed layout by capturing structured snapshots of style trees, box trees, and fragment chains aligned to the exact pipeline stage that issued them.

**Why this priority**: Targeted dumps shorten root-cause analysis for regressions that currently need manual code instrumentation.

**Independent Test**: Author an integration test that renders a known HTML and asserts that the diagnostics session exposes serializable structures for each pipeline stage, including counts of styles, boxes, fragments, pagination steps, and the intermediate reasoning metadata that led to layout decisions.

**Acceptance Scenarios**:

1. **Given** diagnostics are enabled with structured dumps, **When** the layout stage completes, **Then** a deterministic representation of the box tree is captured with stable identifiers that can be diffed between runs.
2. **Given** an inline measurement failure, **When** diagnostics capture is requested for that stage, **Then** the dump highlights offending nodes and includes the measured inputs, calculated widths, reasoning messages, and warnings so investigators understand why the failure occurred without mutating pipeline state.
3. **Given** a shrink-to-fit calculation, **When** the layout engine enters the measurement routine, **Then** a diagnostics context (for example, `session.Context("ShrinkToFit")`) records key-value pairs such as available width and intrinsic width so downstream tools can reconstruct the decision path.

---

### User Story 3 - Pluggable Observers and Sinks (Priority: P3)

A partner team subscribes a custom sink that forwards diagnostics streams to JSON files or console tracing today, with SVG visualizers and PDF metadata sinks tracked for a follow-on milestone.

**Why this priority**: Third parties need to integrate their own tools without Html2x code changes.

**Independent Test**: Build contract tests that register mock sinks through the new observer API, trigger renders, and assert the JSON and console sinks receive canonical event payloads covering stage events, structured data, internal reasoning, warnings, and intermediate values. Determine whether a lightweight `InMemorySink` is required to support deterministic unit-test assertions before committing to that scope, and record backlog items for SVG and PDF sinks.

**Acceptance Scenarios**:

1. **Given** a JSON sink registered through the diagnostics API, **When** a render completes, **Then** the sink writes a structured log containing session id, stage names, timing data, and captured dumps to a provided stream.
2. **Given** a console sink enabled, **When** a render runs with diagnostics activated, **Then** the sink emits stage lifecycle entries to stdout with identifiers that match the JSON payloads so manual operators can follow the same timeline without post-processing.
3. **Given** the Html2x.TestConsole harness configured as a diagnostics consumer, **When** a render runs with diagnostics enabled, **Then** the harness routes the emitted events through a `LoggerSink` (or equivalent) so operators can observe stage timelines directly in the console output without holding any `ILogger` dependency inside the Html2x pipeline.

---

### Edge Cases

- Missing fonts, locale differences, or timestamp sources should still emit diagnostics that explain the fallback path without altering rendering output.
- Observers run synchronously; if a sink or observer throws, the exception propagates to the caller and the diagnostics output records that failure for later analysis.
- Concurrent renders with overlapping diagnostics sessions need unique identifiers and thread-safe buffers to keep data isolated.
- Unit tests may need an `InMemorySink` (or similar) to capture diagnostics without relying on console output; if implemented, it must stay internal to the test harness and avoid impacting production configurations.

## Requirements (mandatory)

### Functional Requirements

- **FR-001**: Diagnostics remain opt-in with zero instrumentation executed when disabled; default configuration leaves diagnostics off for all renders until explicitly enabled by the caller.
- **FR-002**: Creating a diagnostics session requires an explicit API call that scopes capture to a single render, assigns a deterministic session id, and enforces disposal after completion. Additional documentation is optional when diagnostics are enabled.
- **FR-003**: Each pipeline stage (style computation, box layout, inline measurement, fragmentation, pagination, PDF rendering) publishes start, stop, and error events with timestamps, correlation ids, and payload sizes.
- **FR-004**: Structured dumps expose snapshot models for styles, boxes, fragments, and pagination artifacts with stable identifiers so successive runs can be diffed by external tools.
- **FR-005**: Observer registration supports chaining multiple sinks; sinks run synchronously within the diagnostics pipeline, and sink exceptions may propagate to the caller. Diagnostics treat sinks as best-effort helpers and do not guarantee isolation beyond surfacing errors when practical.
- **FR-006**: Built-in sinks for this delivery cover JSON serialization and console output, with SVG visualization and PDF metadata embedding captured as follow-up roadmap items; each delivered sink can be toggled independently via configuration, and the team will evaluate whether an internal-only `InMemorySink` is necessary for deterministic unit-test assertions before extending scope.
- **FR-007**: The diagnostics subsystem exposes extension points so partners can register custom sinks or observers without referencing internal Html2x assemblies, using contracts published under `Html2x.Abstractions`.
- **FR-008**: Html2x.Diagnostics ships as a separate assembly that core rendering projects do not reference directly; the Html2x facade injects diagnostics dependencies into the pipeline when requested, and documentation explains how integrators wire it up without leaking diagnostics types across module boundaries.
- **FR-009**: Automated tests assert best-effort deterministic outputs with diagnostics on and off inside the Html2x reference environment by verifying stage ordering through diagnostics events and documenting any observed variances (e.g., PDF metadata). Byte-level comparisons are optional and not required.
- **FR-010**: Diagnostics instrumentation replaces direct `ILogger` dependencies within Html2x core assemblies; Html2x.TestConsole may keep its existing `ILogger` usage solely to print diagnostics to stdout, and integrating diagnostics sinks there is recommended but optional.
- **FR-011**: Diagnostics data models must capture not only the structures flowing through the pipeline but also the engine’s reasoning, decisions, calculations, warnings, and intermediate values, exposing a rich diagnostics model that observers alone cannot provide.
- **FR-012**: Diagnostics sessions expose scoped `DiagnosticContext` handles (e.g., `session.Context("ShrinkToFit")`) that allow stages to set key-value pairs like available width or intrinsic width, ensuring reasoning breadcrumbs flow alongside events and dumps without retaining disposable context objects.
- **FR-013**: Diagnostics capture full, unredacted payloads (including sensitive inputs) by default; sanitization or redaction responsibility lies with downstream sinks or consumers that choose to emit or persist the data.

### Key Entities

- **DiagnosticSession**: Represents a scoped capture tied to one render; tracks session id, activation flags, and lifecycle (created, capturing, completed, disposed).
- **DiagnosticEvent**: Immutable payload describing stage, timestamp, severity, correlation id, metrics, and optional dump references.
- **DiagnosticObserver**: Contract for components that listen to events and dumps, responsible for routing data toward sinks and enforcing isolation policies.
- **DiagnosticContext**: Lightweight scope created from a diagnostics session (e.g., `session.Context("ShrinkToFit")`) that records key-value pairs (`ctx.Set("availableWidth", 140)`, `ctx.Set("intrinsicWidth", 210)`) representing intermediate reasoning; disposal flushes the captured metadata into events or dumps.
- **DiagnosticsModel**: Defines the richer capture schema that combines data structures, reasoning narratives, calculation inputs/outputs, warnings, and intermediate values so downstream tools can reconstruct decision context without relying solely on observer callbacks.
- **DiagnosticSink**: Pluggable destination (JSON and console sinks delivered now, SVG/PDF metadata/custom sinks supported by contracts later) that receives normalized payloads along with sink-specific configuration such as output streams or serialization hints.
- **StructuredDump**: Value object containing serialized representations of style trees, box trees, inline fragments, and pagination steps with stable identifiers for diffing.

### Assumptions

- Diagnostics defaults to off in all production environments; enabling it requires explicit configuration during troubleshooting.
- Session scoped captures are sufficient; long-lived global captures would introduce unacceptable overhead and are out of scope now.
- Existing Html2x.TestConsole harness remains the primary vehicle for manual and automated validation, so new samples and fonts will be added there when needed.
- Replacing `ILogger` usage inside Html2x is feasible without regressions because console logging continues through the diagnostics sink pattern demonstrated in TestConsole, and Html2x.TestConsole may retain `ILogger` solely to print diagnostics results to standard output.
- SVG visualization and PDF metadata sinks remain backlog items; this iteration only delivers JSON and console sinks with documented follow-up requirements.
- An optional `InMemorySink` may be introduced to strengthen unit-test assertions; decision deferred until we evaluate JSON/console sink coverage gaps.
- Diagnostics emit raw values; operators must configure sinks responsibly when sensitive data must be hidden or transformed.
- The Html2x reference environment for deterministic testing is the Windows Server CI image with the pinned font set documented in `docs/diagnostics.md`; other environments are best-effort and any differences will be recorded in release notes.

## Success Criteria (mandatory)

### Measurable Outcomes

- **SC-001**: Automated tests demonstrate that renders executed without enabling diagnostics produce no additional diagnostics events, dumps, or sink invocations compared to the current main branch.
- **SC-002**: Enabling diagnostics for a targeted run yields 100% coverage of the six named pipeline stages with ordered start and end events in the reference environment, and any untested environments or known variances are documented.
- **SC-003**: JSON and console sinks pass contract tests that confirm events are persisted or emitted and can be decoded downstream, and backlog records exist for SVG and PDF metadata sink requirements.
- **SC-004**: Structured dumps capture style tree, box tree, and fragment snapshots whose node counts and identifiers match baseline expectations for the sample documents.
- **SC-005**: Documentation in `docs/diagnostics.md` plus release notes explain activation steps, safety considerations, and rollback guidance before the feature is flagged ready for release.
