# Diagnostics Architecture

Diagnostics are developer-facing troubleshooting artifacts. They explain
conversion lifecycle, unsupported input, layout decisions, rendering decisions,
and serializer output without requiring a debugger.

## Report Flow

`HtmlConverter` creates a `DiagnosticsCollector` when `HtmlConverterOptions.Diagnostics.EnableDiagnostics` is true. The collector is passed as an `IDiagnosticsSink` through style, geometry, layout, pagination, font, and renderer boundaries. The completed `DiagnosticsReport` is exposed on `Html2PdfResult.DiagnosticsReport`.

Typical record flow:

```text
LayoutBuild stage/started
  -> dom, style, box-tree, fragment-tree, and pagination lifecycle records
  -> style, geometry, table, pagination, image, or font records
LayoutBuild stage/succeeded with diagnostic snapshot fields
PdfRender stage/started
PdfRender stage/succeeded with render fields
```

If layout fails, `PdfRender` is skipped and the diagnostics report is attached to the thrown exception as `DiagnosticsReport` when available.

## Ownership

Generic diagnostics contracts that cross project boundaries belong in
`Html2x.Diagnostics.Contracts`. JSON export belongs in `Html2x.Diagnostics`.
`Html2x.Abstractions` owns no diagnostics contracts, collections, report
models, snapshot DTOs, or serializers.

`Html2x.Diagnostics` owns `DiagnosticsCollector`, `DiagnosticsReport`, and
`DiagnosticsReportSerializer`. The report serializer is generic over
`DiagnosticRecord` and `DiagnosticFields`; it must not special-case layout,
geometry, table, renderer, image, or font models.

Pipeline stages own the events that describe their decisions:

- Style owns unsupported and ignored CSS declaration diagnostics.
- Layout owns geometry, formatting, table, pagination, image resolution, and unsupported layout mode diagnostics.
- The public facade owns conversion lifecycle and converter-level font path failures.
- The PDF renderer owns renderer summaries and renderer-local failures.

## Severity

- `Info`: expected trace detail or successful decision.
- `Warning`: recoverable issue that can affect visual output.
- `Error`: conversion-blocking or stage-failing issue.

## Context

Emitters should include useful context when available:

- Selector or selector-like source.
- Element identity such as tag, id, class, or role.
- Raw style declaration or value.
- Structural path through DOM, style tree, box tree, fragments, table, or pagination.
- Raw input when diagnostics are explicitly enabled and the value is needed for reproduction.

Missing context should not make the event unreadable.
