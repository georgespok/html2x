# Extending Diagnostics

Add diagnostics when a behavior needs to be observable, testable, or
explainable without stepping through a debugger.

## Define The Record

Diagnostics emitted across project boundaries use
`Html2x.Diagnostics.Contracts.DiagnosticRecord`.

Records should include:

- Stable `Stage` and `Name` values.
- `DiagnosticSeverity`.
- A short message when the record describes a warning or error.
- Context needed to find the affected input or output.
- Stable event-specific fields.
- Raw user input only when diagnostics are enabled and reproduction requires it.

Use `DiagnosticFields` for event-specific data. Field values must be strings,
numbers, booleans, enum names as strings, nulls, diagnostic arrays, or nested
diagnostic objects.

## Emit The Record

Emit records from the module that owns the decision:

- Style diagnostics from style computation.
- Geometry, pagination, table, image, and unsupported mode diagnostics from layout.
- Conversion lifecycle diagnostics from the converter facade.
- Render summaries and renderer failures from the renderer.

Producer modules should use small local helper methods when a record needs to
flatten local domain models. `Html2x.Diagnostics` must not reference those
domain models.

## Serialization

`Html2x.Diagnostics.DiagnosticsReportSerializer` serializes
`DiagnosticsReport` generically. New diagnostic families should not require
serializer-specific mapping code.

## Test

Add tests for:

- Record stage.
- Event name.
- Severity.
- Important field values.
- Nested diagnostic objects or arrays when used.
- Stable ordering when ordering is part of the contract.
