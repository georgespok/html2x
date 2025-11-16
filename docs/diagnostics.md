# Diagnostics Framework Guide

## Overview
Html2x.Diagnostics is an opt-in subsystem that instruments Html2x renders without changing output when disabled. Sessions are created through `DiagnosticsRuntime.Configure(...)` and compose into the `HtmlConverter` facade so all downstream stages (style, layout, fragments, pagination, rendering) can publish structured events, dumps, and reasoning breadcrumbs.

- **Disabled state**: No diagnostics objects are allocated, no sinks are instantiated, and renders behave exactly like the current baseline.
- **Enabled state**: Each render runs inside a diagnostics session that streams synchronous events to the configured sinks (JSON, console, in-memory test sinks, or custom implementations).

## Enabling Diagnostics
1. Construct `DiagnosticsOptions` at the Html2x facade boundary and call `BuildRuntime()` when a caller explicitly opts in:
   ```csharp
   var diagnostics = new DiagnosticsOptions
   {
       EnableConsoleSink = true,
       JsonOutputPath = "build/diagnostics/session.json",
       EnableInMemorySink = Debugger.IsAttached
   }.BuildRuntime();
   ```
   `DiagnosticsOptions` is the supported surface for enabling any builtin or custom sink; it replaces ad-hoc `ILogger` factories across the solution.
2. Decorate the `HtmlConverter` and start an explicit session:
   ```csharp
   var converter = diagnostics.Decorate(new HtmlConverter());
   using var session = diagnostics.StartSession("sample-run");
   var pdf = await converter.ToPdfAsync(html, options);
   ```
3. Html2x.TestConsole wires its `--diagnostics` / `--diagnostics-json` flags directly to the same options builder, making the CLI the lone component that still uses `ILogger` (only to print status updates). All other assemblies publish diagnostics solely through sinks.
4. Advanced scenarios can still call `DiagnosticsRuntime.Configure(opts => { ... })` if they need to register custom sinks dynamically; under the hood this is what `DiagnosticsOptions.BuildRuntime()` uses.

## Structured Dumps
- Layout now emits a `dump/layout` event after the box tree finishes building. The event contains a `StructuredDumpMetadata` payload with:
  - `format`: always `json` for built-in dumps.
  - `nodeCount`: deterministic count of nodes captured.
  - `body`: serialized JSON carrying `nodes` (each node has a stable identifier such as `layout.0.1`), attributes, and summaries.
- Use the JSON sink output or capture the metadata body from in-memory sinks to diff node identifiers between runs. The StructuredDumpTests assert deterministic node counts; treat the test as a baseline when adding new node types.
- Future dump types (styles, fragments, pagination) should reuse `LayoutDiagnosticsDumper` patterns: assemble a `StructuredDumpDocument`, pass it to `DiagnosticSession.Publish`, and let the runtime serialize it for you.

## Diagnostic Contexts
- `session.Context("Scenario")` now records key/value pairs and emits a `context/detail` event automatically when disposed.
- Payload shape:
  - `contextId`: GUID correlating values to logs.
  - `name`: friendly label such as `ShrinkToFit`.
  - `openedAt` / `closedAt`: UTC timestamps for latency tracking.
  - `values`: dictionary containing any reasoning metrics set through `ctx.Set`.
- Use contexts to capture shrink-to-fit metrics (available width, intrinsic width), pagination heuristics, or fragment selection reasons. Keep values serializable (strings, numbers, bools, arrays) to avoid sink-specific conversions.

## Built-in Sinks
`DiagnosticsOptions` (in `Html2x`) is the canonical way to enable sinks. The table below summarizes the built-ins and how the CLI toggles map to each option.

| Sink | How to Enable | CLI Flag | Best For |
|------|---------------|----------|----------|
| Console | `EnableConsoleSink = true` (default) | `--diagnostics` | Live stage timelines, verifying ordering against JSON output. |
| JSON | `JsonOutputPath = "build/diagnostics/session.json"` | `--diagnostics-json <path>` | Persisting events/dumps for diffing or attaching to CI runs. |
| In-memory | `EnableInMemorySink = true`, optionally tune `InMemoryCapacity` | n/a (tests only) | Deterministic assertions without writing to disk/console. |

Example:
```csharp
var diagnostics = new DiagnosticsOptions
{
    EnableConsoleSink = !Environment.GetEnvironmentVariable("CI").Equals("true", StringComparison.OrdinalIgnoreCase),
    JsonOutputPath = "build/diagnostics/session.json",
    EnableInMemorySink = true,
    InMemoryCapacity = 2048
}.BuildRuntime();
```

## Console Output Diff
When investigating regressions, it’s often useful to diff the console timeline against a baseline:
```powershell
dotnet run --project src/Tests/Html2x.TestConsole/... --diagnostics `
    --diagnostics-json build/diagnostics/current.json |
    Tee-Object build/diagnostics/current.console

git diff --no-index build/diagnostics/baseline.console build/diagnostics/current.console
```
Because console output mirrors JSON ordering, mismatches usually indicate a missing event or stage.

## Sanitizing JSON Dumps
- JSON dumps contain raw HTML-derived data (node identifiers, text runs). If this data is sensitive, emit to a secured path or run the sink output through a sanitizer before sharing.
- Use short-lived folders (e.g., `build/diagnostics/$CI_JOB_ID`) and clean them after upload.
- Downstream tools can redact or hash fields (e.g., text contents) before persisting; the in-memory sink is a good fit when you only need counts/ordering.

## Validation Checklist
1. **Unit tests**: Run `dotnet test src/Html2x.sln -c Release --filter StructuredDump` and `--filter DiagnosticContext` to ensure dumps and context events stay deterministic.
2. **Console smoke**: `dotnet run --project src/Tests/Html2x.TestConsole/... --diagnostics --diagnostics-json build/diagnostics/session.json` then inspect the generated JSON for stage events, dumps, and context entries.
3. **Diffing**: Compare structured dump JSON outputs (or sink files) between commits to surface regressions in node counts, stage order, or context values.

## Failure Modes & Recovery
- **Sink failure**: By default sink exceptions propagate to fail the render. Set `builder.SetSinkExceptionPropagation(false)` during configuration if you prefer diagnostics to swallow sink faults (the dispatcher will continue invoking remaining sinks).
- **Large dumps**: Extensive documents can produce sizable JSON payloads. Capture only the stages you need; consider trimming attributes before creating the `StructuredDumpDocument` if memory pressure surfaces.
- **Missing contexts**: If no `context/detail` events appear, confirm the code path disposes the context (prefer `using var ctx = ...`). A leaked context never flushes values.

## Extending
- Add new sinks by implementing `IDiagnosticSink` and registering them through `DiagnosticsRuntime.Configure`.
- Introduce additional dump builders alongside `LayoutDiagnosticsDumper` for styles or fragments; plug them into each stage via `session.Publish`.

## Release Notes & Backlog
- Locked in `DiagnosticsOptions` as the single configuration surface; console, JSON, and in-memory sinks are selectable toggles that the CLI now mirrors automatically.
- Html2x, LayoutEngine, and Renderers.Pdf removed `ILogger` entirely. Html2x.TestConsole retains `ILogger` strictly for human-readable status, and diagnostics sinks handle all structured output.
- Validation artifacts from `dotnet test src/Html2x.sln -c Release` plus the TestConsole smoke run live in `build/diagnostics/final.md` along with links to generated PDFs/JSON.
- Follow-up work covering SVG visualization sinks (timeline diff SVGs via `--diagnostics-svg`) and PDF metadata sinks (`EnablePdfMetadataSink`). Treat that section as the canonical backlog reference until we schedule those stories.
