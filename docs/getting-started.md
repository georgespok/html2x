# Getting Started

This page gets a developer from a clean checkout to a verified local build.

## Prerequisites

- .NET 8 SDK.
- A shell that can run `dotnet` commands. PowerShell is the default local shell for this repository.
- Local fonts for manual PDF rendering. The test console includes fonts under `src/Tests/Html2x.TestConsole/fonts`.

## Restore, Build, Test

Run commands from the repository root.

```powershell
dotnet restore src/Html2x.sln
dotnet build src/Html2x.sln -c Release
dotnet test src/Html2x.sln -c Release
```

Use focused test runs while developing a specific subsystem.

```powershell
dotnet test src/Tests/Html2x.LayoutEngine.Test/Html2x.LayoutEngine.Test.csproj -c Release
dotnet test src/Tests/Html2x.Renderers.Pdf.Test/Html2x.Renderers.Pdf.Test.csproj -c Release
dotnet test src/Tests/Html2x.Test/Html2x.Test.csproj -c Release
```

## Manual Rendering Smoke Test

The test console renders sample HTML to PDF and can export diagnostics JSON.

```powershell
dotnet run --project src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj -- src/Tests/Html2x.TestConsole/html/example.html build/example.pdf
```

With diagnostics:

```powershell
dotnet run --project src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj -- src/Tests/Html2x.TestConsole/html/example.html build/example.pdf --diagnostics --diagnostics-json build/diagnostics/session.json
```

## Expected State

After setup:

- The solution restores without missing packages.
- The release build passes.
- The test suite passes.
- The test console can write a PDF under `build/`.

If any step fails, start with [Troubleshooting](development/troubleshooting.md).
