# Data Model: Diagnostics Session Envelope

## DiagnosticsSessionEnvelope
- **Keys**: `sessionId` (Guid), `correlationId` (Guid/string) - each run emits exactly one envelope.
- **Attributes**: `pipelineName`, `environment` (name/value pairs), `startTimestamp`, `endTimestamp`, `status` (Succeeded | Failed | Canceled), `events` (ordered array).
- **Relationships**: Owns many `DiagnosticEvent` entries; references `SessionCollectorLifecycle` state transitions for tracing.
- **Validation Rules**:
  - `startTimestamp` <= `endTimestamp`.
  - `events` must be stored in emission order and cannot be null (empty list allowed).
  - `status` is derived from collector terminal state (success/failure).

## DiagnosticEvent
- **Keys**: composite of `sessionId` + `eventSequence` (monotonic integer).
- **Attributes**: `timestamp`, `severity`, `category`, `stage`, `payload` (object/string), `overridesSessionDefaults` (bool), optional `exception`.
- **Relationships**: Belongs to a single `DiagnosticsSessionEnvelope`.
- **Validation Rules**:
  - `eventSequence` increments by 1 per accepted event.
  - `timestamp` must be >= previous event timestamp (monotonic clock).
  - Payload stored verbatim; serialization errors bubble to collector.

## SessionCollectorLifecycle
- **States**: `Initialized` -> `Active` -> (`Flushing` -> `Completed` | `Failed`) with a possible `Aborted` path if startup fails.
- **Transitions**:
  - `Initialized` when pipeline run begins and metadata is captured.
  - `Active` while events stream in.
  - `Flushing` when completion or fatal error triggers serialization.
  - `Completed` when envelope persisted; `Failed` if sinks throw (with retry metadata).
- **Constraints**: Lifecycle state is append-only; moving backward requires creating a new session.

## EventSinkTarget (logical)
- **Purpose**: Defines where envelopes land (Console, File, Telemetry).
- **Fields**: `name`, `supportsStructuredPayloads` (bool), `maxPayloadBytes`, `retryPolicy`.
- **Usage**: Collector consults sink to decide chunking/backoff; ensures storage requirement alignment noted in spec.
