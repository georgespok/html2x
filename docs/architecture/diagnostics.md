# Diagnostics Architecture

Diagnostics are developer-facing troubleshooting artifacts. They explain stage lifecycle, unsupported input, layout decisions, rendering decisions, and serializer output without requiring a debugger.

## Session Flow

`HtmlConverter` creates a `DiagnosticsSession` when `HtmlConverterOptions.Diagnostics.EnableDiagnostics` is true.

Typical event flow:

```text
LayoutBuild started
  -> stage/dom, stage/style, stage/box-tree, stage/fragment-tree, stage/pagination lifecycle events
  -> style, geometry, table, pagination, image, or font traces
LayoutBuild succeeded with layout snapshot
PdfRender started
PdfRender succeeded with render summary
```

If layout fails, `PdfRender` is skipped and the diagnostics session is attached to the thrown exception when available.

## Ownership

Diagnostic contracts that cross project boundaries belong in `Html2x.Abstractions`. JSON export belongs in `Html2x.Diagnostics`.

Pipeline stages own the events that describe their decisions:

- Style owns unsupported and ignored CSS declaration diagnostics.
- Layout owns geometry, formatting, table, pagination, image resolution, and unsupported layout mode diagnostics.
- The public facade owns stage lifecycle and converter-level font path failures.
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
