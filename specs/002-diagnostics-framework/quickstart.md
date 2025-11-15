# Quickstart - Html2x Diagnostics Framework

## CLI Setup Commands
1. `dotnet restore src/Html2x.sln` - restores all Html2x projects (solution file lives under `src/`).
2. `dotnet test src/Html2x.sln -c Release` - captures the current test baseline before enabling diagnostics.
3. `dotnet run --project src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj -- <input.html> <output.pdf> --diagnostics` - smoke tests the console harness once diagnostics are wired.
4. `pwsh src/Tests/Html2x.TestConsole/diagnostics/run-diagnostics-json.ps1` - sample automation for JSON sink capture (PowerShell).
5. `bash src/Tests/Html2x.TestConsole/diagnostics/run-diagnostics-json.sh` - same automation for macOS/Linux shells.

## 1. Configure Diagnostics via `DiagnosticsOptions`
1. Reference `Html2x.Diagnostics` only from the top-level `Html2x` facade project.
2. Use `DiagnosticsOptions` to describe the sinks you want enabled and call `BuildRuntime()` when diagnostics are explicitly requested (e.g., CLI `--diagnostics` flag, integration tests).
3. Keep diagnostics disabled by default; only construct the runtime when the caller opts in.

```csharp
var diagnostics = new DiagnosticsOptions
{
    EnableConsoleSink = true,
    JsonOutputPath = "build/diagnostics/session.json",
    EnableInMemorySink = Debugger.IsAttached
}.BuildRuntime();

var converter = diagnostics.Decorate(new HtmlConverter());
```
The TestConsole CLI uses this exact options class so `--diagnostics` and `--diagnostics-json <path>` automatically toggle the same sinks without custom wiring.

## 2. Create a Diagnostics Session
```csharp
var diagnostics = DiagnosticsRuntime.Configure(opts =>
{
    opts.AddJsonSink("build/diagnostics/session.json");
    opts.AddConsoleSink();
});

var converter = diagnostics.Decorate(new HtmlConverter());

using var session = diagnostics.StartSession("Sample Render");
var pdf = await converter.ToPdfAsync(html, options);
```

## 3. Record Contextual Values
```csharp
using var ctx = session.Context("ShrinkToFit");
ctx.Set("availableWidth", 140);
ctx.Set("intrinsicWidth", 210);
```
When disposed, the context emits its own diagnostics event (category `context/detail`) whose payload carries the captured values plus open/close timestamps so diagnostics logs can reconstruct the reasoning path.

## 4. Capture Structured Dumps
```csharp
using var session = diagnostics.StartSession("sample-run");
using var ctx = session.Context("ShrinkToFit");

ctx.Set("availableWidth", 140);
ctx.Set("intrinsicWidth", 210);

await converter.ToPdfAsync(html, options);
```

- The layout stage automatically publishes a `dump/layout` event whose `StructuredDumpMetadata` includes node counts, deterministic identifiers, and a JSON body.
- Expect matching node counts across repeated runs; StructuredDumpTests assert this determinism by diffing the `nodes` array within the JSON payload.
- Additional dump types (styles, fragments, pagination) will reuse the same serialization pipeline once those stages opt in.

## 5. Register Sinks
`DiagnosticsOptions` exposes toggles for all built-in sinks:

| Sink | Toggle | Typical Use |
|------|--------|-------------|
| JSON | `JsonOutputPath = "build/diagnostics/session.json"` | Persist events/dumps for diffing or CI artifacts. |
| Console | `EnableConsoleSink = true` | Live stage/timeline view (default for TestConsole). |
| In-memory | `EnableInMemorySink = true`, `InMemoryCapacity = 1024` | Deterministic assertions inside tests without touching disk. |

Custom sinks can still be registered by calling `DiagnosticsRuntime.Configure(opts => opts.AddSink(...))` if your scenario extends beyond the built-ins.

## 6. Run Validation
```powershell
dotnet test Html2x.sln -c Release --filter Diagnostics
dotnet run --project src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj -- sample.html build/diagnostics/sample.pdf --diagnostics --diagnostics-json build/diagnostics/session.json
```
- Ensure diagnostics-off renders emit zero events.
- When enabled, confirm JSON artifacts exist and console output lists each stage start/stop with timestamps.
- Use `--diagnostics-json` outputs (or the structured dump JSON body) to diff node identifiers and ensure shrink-to-fit context values flow through the `context/detail` events.

## 7. Extend
- Add custom sinks by implementing `IDiagnosticSink` from `Html2x.Abstractions.Diagnostics` and registering via the diagnostics helper.
- Update `docs/diagnostics.md` with new sink usage patterns and troubleshooting notes.
