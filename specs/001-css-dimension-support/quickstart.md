# Quickstart: CSS Height and Width Support

1. **Restore & Build**
   ```powershell
   dotnet restore Html2x.sln
   dotnet build Html2x.sln -c Release
   ```
2. **Run Layout Tests**
   ```powershell
   dotnet test Html2x.sln -c Release --filter "FullyQualifiedName~Html2x.Layout.Test && Category=Dimensions"
   ```
   - Covers style parsing, unit normalization, and conflicting constraint resolution.
3. **Run Renderer Regression**
   ```powershell
   dotnet test Html2x.sln -c Release --filter "Html2x.Pdf.Test&&Category=BorderedBlocks"
   ```
   - Confirms fragment sizes survive pagination and renderer consumption.
4. **Smoke-Test via Console**
   ```powershell
   dotnet run --project src/Html2x.TestConsole/Html2x.TestConsole.csproj -- `
     --input src/Html2x.TestConsole/html/width-height/grid.html `
     --output build/width-height/grid.pdf `
     --log-dimensions true
   ```
   - Inspect structured diagnostics for `requestedWidth`, `resolvedWidth`, and `fallbackReason`.
5. **Review Logs**
   - Check `build/logs/width-height/*.json` for deterministic dimension payloads.
6. **Document Updates**
   - Append summary + regression evidence to `docs/testing-guidelines.md` and release notes before merging.

