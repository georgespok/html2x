# Quickstart: Diagnostics Session Envelope

1. **Enable diagnostics collector**  
   - Instantiate `DiagnosticsSessionCollector` at the pipeline boundary (HtmlConverter entry point) and pass it through stage scopes.  
   - Capture session metadata once (IDs, environment markers, start timestamp) before any stage executes.

2. **Publish stage events**  
   - Replace direct StructuredDump writes with `collector.Publish(DiagnosticEvent)` calls inside layout, rendering, and renderer-specific stages.  
   - Include stage identifiers and payloads verbatim to preserve debugging fidelity.

3. **Flush on completion/failure**  
   - Call `collector.Complete()` when the pipeline succeeds; call `collector.Fail(exception)` inside the fatal handler.  
   - Collector serializes a single `DiagnosticsSessionEnvelope` JSON payload and hands it to registered sinks.

4. **Verify via tests and harness**  
   - Run `dotnet test Html2x.sln -c Release` to execute new unit/integration coverage.  
   - Use `dotnet run --project src/Tests/Html2x.TestConsole/... -- --input sample.html --output build/sample.pdf` and inspect the emitted envelope file/log.

5. **Consume downstream**  
   - Point tooling at the existing diagnostics folder/telemetry topic; each run now produces exactly one document that contains lifecycle markers plus ordered events.  
