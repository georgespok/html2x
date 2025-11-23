# Quickstart: Basic HTML-to-PDF Essentials

1. **Restore & build**
   ```powershell
   dotnet restore Html2x.sln
   dotnet build Html2x.sln -c Release
   ```

2. **Run layout regression for text+images**
   ```powershell
   dotnet test Html2x.sln -c Release --filter BasicHtmlDiagnostics
   ```
   - Tests assert diagnostics for `<br>` handling, image sizing, and border payloads.

3. **Manual smoke with TestConsole**
   ```powershell
   dotnet run --project src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj -- \
     --input specs/003-basic-html/samples/basic.html \
     --output build/basic-html.pdf \
     --emit-diagnostics build/basic-html.json
   ```
   - Inspect `build/basic-html.json` to confirm fragments (text color, line height, borders, image dimensions).

4. **Validate diagnostics contract**
   - Compare the emitted JSON with `contracts/diagnostics-contract.md`.
   - Ensure text runs include `LineIndex`, images carry `SourceType` + dimensions, and border entries exist for styled elements.

5. **Ready for planning**
   - Ensure `research.md`, `data-model.md`, and contracts are reviewed.
   - Proceed to `/speckit.plan` follow-up steps (tasks generation) once tests are green.

- 2025-11-17: Ran dotnet restore src/Html2x.sln; no package changes or warnings.
- 2025-11-17: Built dotnet build src/Html2x.sln -c Release; warning CS8618 remains for Fragment.Style (pre-existing).
