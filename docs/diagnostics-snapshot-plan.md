# Diagnostics Snapshot Plan

## Purpose

Diagnostics envelopes exist purely for short-lived troubleshooting. Each Html2x pipeline run emits exactly one JSON document that contains shared session metadata plus ordered `DiagnosticEvent` entries. Operators capture the artifact immediately after the run to inspect failures or regressions, then discard it; we do not preserve long-term history or maintain schema/pipeline version identifiers.

## Envelope Schema

Field | Description
----- | -----------
`sessionId` | Guid generated per pipeline run.
`correlationId` | Guid/string that ties related runs together if needed; defaults to a generated Guid when omitted.
`pipelineName` | Human-friendly name of the pipeline entry point (e.g., HtmlConverter, TestConsole).
`environmentMarkers` | Optional name/value pairs for environment or host metadata.
`startTimestamp` | UTC timestamp captured when the collector initializes.
`endTimestamp` | UTC timestamp captured when the collector flushes (success or failure).
`status` | `Succeeded`, `Failed`, or `Canceled` (see `DiagnosticsSessionStatus`).
`events` | Ordered array of `DiagnosticEvent` instances; each entry retains its original timestamp/severity/category/payload.

## Lifecycle

1. **Initialization** – HtmlConverter instantiates a `DiagnosticsSessionCollector`, captures `sessionId`/`correlationId`, and records `startTimestamp`.
2. **Event Capture** – Layout, rendering, and renderer-specific components publish `DiagnosticEvent` objects through `IDiagnosticSession` scopes; events accrue in order.
3. **Completion Path** – On success, the collector stamps `endTimestamp`, sets status to `Succeeded`, and serializes the envelope via `DiagnosticsSessionEnvelopeSerializer`.
4. **Failure Path** – On exception, the collector appends the failure event (category `session`, kind `session/fail`), captures the error payload, marks status `Failed`, records the final timestamp, and serializes.
5. **Serialization Failure** – If a sink write fails, the collector retries per sink policy and emits a final diagnostic describing the failure so operators know the envelope could not be persisted.
6. **Retention** – Envelopes are inspected immediately after generation and may be deleted once troubleshooting completes; no archival guarantees exist.

## References

- `specs/004-consolidate-diagnostics/data-model.md` – canonical data model.
- `src/Html2x.Abstractions/Diagnostics/Contracts/DiagnosticsSessionEnvelope.cs` – DTO definition.
- `src/Html2x.Diagnostics/Serialization/DiagnosticsSessionEnvelopeSerializer.cs` – JSON serializer.
