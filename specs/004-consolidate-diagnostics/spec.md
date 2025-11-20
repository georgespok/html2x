# Feature Specification: Diagnostics Session Envelope

**Feature Branch**: `004-consolidate-diagnostics`  
**Created**: November 20, 2025  
**Status**: Draft  
**Input**: User description: "We need a new diagnostics session envelope that replaces the old list of sessions with a single serialized document per pipeline run. This document must own all session-level identifiers, pipeline metadata, environment markers, and life cycle timestamps so shared context is declared once. Inside that envelope we keep an ordered list of diagnostic events where each event still supplies its own timestamp, severity, category, and payload to preserve overrides. The pipeline publishes events through a session-scoped collector that flushes the full document whenever the run completes, whether successfully or due to failure. Storage and telemetry layers emit only the new hierarchical schema, so all downstream tooling sees the consolidated format. Tests and documentation must reflect this specification"

## User Scenarios & Testing (mandatory)

### User Story 1 - Single Session Capture (Priority: P1)

As a pipeline owner I want each Html2x run to emit a single diagnostics envelope that records state transitions of every stage so I can inspect one authoritative artifact per execution. This preserves staged layout discipline (Principle I) because the collector only records when a stage begins or ends, and it protects rendering predictability (Principle II) by logging diagnostics instead of inspecting rendered assets. The plan follows Goal-Driven Delivery by declaring the current flat-session gap, the ordered action of building a per-run collector, and adaptive checkpoints at each stage boundary.

**Why this priority**: Without the envelope we continue shipping redundant payloads that mask ordering bugs; fixing that unlocks every downstream consumer.

**Independent Test**: Add `Html2x.Diagnostics.Tests.DiagnosticsSessionEnvelopeTests.RunProducesSingleDocument` that fails until the collector produces exactly one envelope with the requested metadata.

**Acceptance Scenarios**:

1. **Given** a pipeline run starts and a session collector is initialized, **When** the run completes successfully, **Then** the system persists one envelope containing shared session metadata and an ordered list of all emitted events.  
2. **Given** each composition stage emits diagnostics through the collector, **When** events are serialized, **Then** their relative order matches the sequence they were raised so analysts can replay the run chronologically.

---

### User Story 2 - Failure Flush Discipline (Priority: P2)

As a developer investigating failures I need the collector to flush the full document even when a stage throws so I never lose the terminal diagnostics needed for reflection (Principles IV and VI).

**Why this priority**: Failure telemetry is currently fractured across multiple dumps, turning every outage into a forensic exercise.

**Independent Test**: Add `Html2x.Diagnostics.Tests.DiagnosticsSessionEnvelopeTests.FailureFlushesEnvelope` that forces a stage exception and asserts the envelope contains the failure event and session end markers.

**Acceptance Scenarios**:

1. **Given** a stage throws before completing, **When** the pipeline transitions to failure handling, **Then** the collector records the last successful stage, the failure event, the error payload, and the final session timestamp before persisting the envelope.

---

### User Story 3 - Downstream Availability and Audit (Priority: P3)

As a diagnostics consumer I need storage and telemetry sinks to expose only the new hierarchical schema so I can query one document per run, link audits by session identifiers, and capture reflection notes without syncing multiple resources (Principles II and VI).

**Why this priority**: Unified output lets tooling flag regressions and monitor adaptive checkpoints automatically without stitching multiple resources.

**Independent Test**: Add an integration test in `Html2x.TestConsole` that runs the harness, captures the saved envelope, and verifies storage contains a single document with environment markers and audit trail.

**Acceptance Scenarios**:

1. **Given** a harness run completes, **When** storage writes diagnostics, **Then** downstream tooling retrieves one session document whose schema matches the documented contract, including environment markers and lifecycle timestamps.  
2. **Given** telemetry subscribers listen for diagnostics exports, **When** the new format ships, **Then** only the hierarchical payload is broadcast so there are no legacy StructuredDump emissions to confuse consumers.

---

### Edge Cases

- No events: if a pipeline produces no events, the collector still emits an envelope with metadata and an empty event list so predictable structure is maintained.  
- Partial metadata: if some environment markers are missing, the collector records nulls plus a diagnostics warning event for traceability.  
- Long running sessions: collectors must flush in chunks to memory but persist one final document without truncating order.  
- Clock drift: per-event timestamps use a monotonic source so chronological order is accurate even when system clocks shift.  
- Serialization failure: if persistence fails, the collector retries with backoff and logs a final out-of-band error so operators can re-run without data loss.

## Requirements (mandatory)

### Functional Requirements

- **FR-001**: Each pipeline execution MUST create exactly one `DiagnosticsSessionEnvelope` containing session-level identifiers, pipeline metadata, environment markers, and lifecycle timestamps, and the envelope MUST show start, stage transition, and completion states.  
- **FR-002**: The envelope MUST include an ordered `DiagnosticEvent` collection where each event carries its own timestamp, severity, category, payload (stored in full without redaction), and optional overrides without duplicating session metadata.  
- **FR-003**: A session-scoped collector MUST manage event capture across stages, enforce stage ordering, and flush on completion, failure, or timeout so no event batch is lost.  
- **FR-004**: Storage writers and telemetry emitters MUST persist and broadcast only the new hierarchical schema; all StructuredDump DTOs, serializers, and writers are deleted or redirected to the new shape.  
- **FR-005**: Automated tests across unit, integration, and harness layers MUST assert run-level uniqueness, ordered events, failure flush discipline, and schema invariants before implementation merges.  
- **FR-006**: Developer documentation MUST describe the envelope schema, provide sample JSON, and explain collector state transitions for the new lifecycle.  
- **FR-007**: Instrumentation MUST capture observability metrics for collector lifecycle events (initialized, event accepted, flush success, flush failure) so Principle IV coverage remains auditable.

### Key Entities

- **DiagnosticsSessionEnvelope**: Represents a single pipeline run diagnostic artifact with fields for sessionId, correlationId, pipeline name, version, environment markers, startTimestamp, endTimestamp, status, and `Events`.  
- **DiagnosticEvent Entry**: Reuses the existing `DiagnosticEvent` contract to represent individual events with timestamp, severity, category, payload, optional stage identifier, and flags for session-default overrides.  
- **SessionCollectorLifecycle**: Conceptual controller that tracks state transitions (initialized, active, flushing, completed, failed) and enforces one-way progression.

## Assumptions

- All Html2x pipeline stages already expose diagnostics hooks that can be routed through the new collector without redesigning stage contracts.  
- Telemetry consumers are not yet in production, so breaking schema changes carry no external migration risk.  
- Storage limits allow one aggregated document per run without exceeding existing retention quotas, so no sharding is required initially.
- Diagnostics envelopes are short-lived troubleshooting artifacts; they are captured for immediate analysis and are not preserved or versioned for long-term auditing.

## Success Criteria (mandatory)

### Success Signals

- **SC-001**: Test and harness runs consistently emit a single diagnostics envelope per pipeline execution, showing the new collector is active end-to-end.  
- **SC-002**: Lifecycle timestamps (start, last event, end) remain present in emitted envelopes so analysts can reconstruct execution flow without inspecting PDFs.  
- **SC-003**: Documentation and release notes describe the schema change well enough that contributors can read envelopes without additional guidance.

## Clarifications

### Session 2025-11-20

- Q: Should DiagnosticEvent payloads be redacted before storing them in the envelope? → A: Always retain full payload contents.
