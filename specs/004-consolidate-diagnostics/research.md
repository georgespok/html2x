# Research: Diagnostics Session Envelope

## Decision 1: Session-Scoped Collector Lifecycle
- **Decision**: Implement a dedicated `DiagnosticsSessionCollector` that captures events in-memory, enforces stage ordering, and flushes once per pipeline completion or fatal failure.
- **Rationale**: Keeps stage isolation intact, guarantees a single envelope per run, and allows the collector to be toggled on/off without extra scaffolding. Matches Principle I (staged layout) and IV (observability) while minimizing integration touchpoints.
- **Alternatives considered**:
  - *Reuse existing StructuredDump per-event writes*: rejected because it perpetuates duplicate metadata and requires downstream stitching.
  - *Stream events directly to sinks without buffering*: rejected due to ordering guarantees, difficulty including final session metadata, and inability to attach lifecycle timestamps consistently.

## Decision 2: Envelope Serialization
- **Decision**: Serialize envelopes as JSON using the existing `Html2x.Diagnostics` serialization helpers without embedding schema or pipeline version metadata.
- **Rationale**: Envelopes are short-lived troubleshooting artifacts; keeping the payload lightweight avoids unnecessary duplication while still aligning with existing sinks (console, file, telemetry). System.Text.Json already in use, so no additional dependency risk.
- **Alternatives considered**:
  - *Binary serialization*: unnecessary for current payload sizes and would reduce debuggability.
  - *Dual-writing legacy StructuredDump + new envelope*: rejected to avoid double I/O and because no downstream consumers depend on the old shape.

## Decision 3: Payload Retention Strategy
- **Decision**: Store `DiagnosticEvent.Payload` values verbatim (no redaction) inside the envelope; sensitive data concerns are mitigated via controlled diagnostics environments and limited access to diagnostics artifacts.
- **Rationale**: Full payloads are mandatory for reliable triage since Html2x relies on diagnostics to trace layout/rendering flows. Redaction would remove context (e.g., CSS selectors, layout node IDs) and slow root-cause analysis.
- **Alternatives considered**:
  - *Always redact sensitive fields*: too lossy for debugging.
  - *Configurable redaction per session*: adds complexity without current compliance requirements; can be layered later if needed.

## Baseline Verification (T002)
- **Command**: `dotnet test src/Html2x.sln -c Release` (run on 2025-11-21).  
- **Result**: Html2x.LayoutEngine.Test (39 total, 36 passed, 3 skipped), Html2x.Renderers.Pdf.Test (10 passed), Html2x.Test (10 passed); no failures. Confirms current diagnostics pipeline is green before feature work begins.

## Coding Standards Review (T003)
- Examined `src/.editorconfig` to reconfirm indentation (4-space C#/HTML, 2-space XML) and naming/severity settings that new diagnostics code must honor.  
- No `Directory.Build.props` exists in the repo; default SDK props remain in effect, so build configuration stays simple unless future work introduces a repo-wide props file.
