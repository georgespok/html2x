# Quickstart - Html2x Diagnostics Framework

## CLI Setup Commands
1. `dotnet restore src/Html2x.sln` - restores all Html2x projects (solution file lives under `src/`).
2. `dotnet test src/Html2x.sln -c Release` - captures the current test baseline before enabling diagnostics.
3. `dotnet run --project src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj -- <input.html> <output.pdf> --diagnostics` - smoke tests the console harness once diagnostics are wired.
4. `pwsh src/Tests/Html2x.TestConsole/diagnostics/run-diagnostics-json.ps1` - sample automation for JSON sink capture (PowerShell).
5. `bash src/Tests/Html2x.TestConsole/diagnostics/run-diagnostics-json.sh` - same automation for macOS/Linux shells.

## 1. Enable Diagnostics in Html2x
1. Reference `Html2x.Diagnostics` only from the top-level `Html2x` facade project.
2. Add an opt-in helper (e.g., `DiagnosticsRuntime.Configure`) that wires sinks and exposes decorators for layout builders, renderers, or `HtmlConverter` instances.
3. Keep diagnostics disabled by default; callers must explicitly enable via builder or runtime flag.

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
When disposed, the context emits its own diagnostics event (category `context`, kind `detail`).

## 4. Register Sinks
- **JSON Sink**: `options.AddJsonSink(pathOrStream)` captures structured payloads for later diffing.
- **Console Sink**: `options.AddConsoleSink()` emits timeline entries to stdout (used by `Html2x.TestConsole`).
- **InMemory Sink (optional, test harness)**: Evaluate during implementation; intended for assertions without touching disk or console.

## 5. Run Validation
```powershell
dotnet test Html2x.sln -c Release --filter Diagnostics
dotnet run --project src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj -- sample.html build/diagnostics/sample.pdf --diagnostics --diagnostics-json build/diagnostics/session.json
```
- Ensure diagnostics-off renders emit zero events.
- When enabled, confirm JSON artifacts exist and console output lists each stage start/stop with timestamps.

## 6. Extend
- Add custom sinks by implementing `IDiagnosticSink` from `Html2x.Abstractions.Diagnostics` and registering via the diagnostics helper.
- Update `docs/diagnostics.md` with new sink usage patterns and troubleshooting notes.
