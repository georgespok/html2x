# Extending Diagnostics

Add diagnostics when a behavior needs to be observable, testable, or explainable without stepping through a debugger.

## Add A Payload

Payload contracts that cross projects belong in `Html2x.Abstractions` and should implement `IDiagnosticsPayload`.

Payloads should include:

- `Kind`.
- Stable event-specific fields.
- Context needed to find the affected input or output.
- Raw user input only when diagnostics are enabled and reproduction requires it.

## Emit The Event

Emit events from the stage that owns the decision:

- Style diagnostics from style computation.
- Geometry, pagination, table, image, and unsupported mode diagnostics from layout.
- Stage lifecycle diagnostics from the converter facade.
- Render summaries and renderer failures from the renderer.

## Update Serialization

Add mapping support in `Html2x.Diagnostics.DiagnosticsSessionSerializer` for known payload fields. Unknown payloads should still preserve `kind`.

## Test

Add tests for:

- Event name.
- Severity.
- Payload kind.
- Known serialized fields.
- Forward-compatible unknown payload behavior when relevant.
- Stable ordering when ordering is part of the contract.
