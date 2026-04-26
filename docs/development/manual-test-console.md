# Manual Test Console

`Html2x.TestConsole` is the manual harness for rendering sample HTML to PDF and exporting diagnostics JSON.

## Purpose

Use the console when you need to inspect visual output, compare diagnostics JSON, or reproduce a scenario outside unit tests.

It is not a replacement for automated tests. If the console reveals a bug, add a focused unit or integration test before fixing it.

## Basic Render

Run from the repository root.

```powershell
dotnet run --project src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj -- src/Tests/Html2x.TestConsole/html/example.html build/example.pdf
```

## Render With Diagnostics

```powershell
dotnet run --project src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj -- src/Tests/Html2x.TestConsole/html/example.html build/example.pdf --diagnostics --diagnostics-json build/diagnostics/session.json
```

## Sample Inputs

Sample HTML files live under:

```text
src/Tests/Html2x.TestConsole/html/
```

Fonts live under:

```text
src/Tests/Html2x.TestConsole/fonts/
```

## Output Policy

Generated PDFs, diagnostics JSON, and logs belong under `build/`. Do not commit generated artifacts unless a test fixture explicitly requires them.
