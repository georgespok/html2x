# Build, Run, And Test

This page collects durable commands for restoring, building, testing, and
manually rendering Html2x from a local checkout.

Run commands from the repository root.

## Restore, Build, Test

```powershell
dotnet restore src/Html2x.sln
dotnet build src/Html2x.sln -c Release
dotnet test src/Html2x.sln -c Release
```

Use [Testing Strategy](testing-strategy.md) for focused test ownership and
project-specific commands.

## Focused Test Examples

```powershell
dotnet test src\Tests\Html2x.LayoutEngine.Style.Test\Html2x.LayoutEngine.Style.Test.csproj -c Release --no-build
dotnet test src\Tests\Html2x.LayoutEngine.Geometry.Test\Html2x.LayoutEngine.Geometry.Test.csproj -c Release --no-build
dotnet test src\Tests\Html2x.LayoutEngine.Fragments.Test\Html2x.LayoutEngine.Fragments.Test.csproj -c Release --no-build
dotnet test src\Tests\Html2x.LayoutEngine.Pagination.Test\Html2x.LayoutEngine.Pagination.Test.csproj -c Release --no-build
dotnet test src\Tests\Html2x.LayoutEngine.Test\Html2x.LayoutEngine.Test.csproj -c Release --no-build --filter FullyQualifiedName~Architecture
dotnet test src\Html2x.sln -c Release --no-build --filter Category=Integration
```

## Manual Test Console

`Html2x.TestConsole` is the manual harness for rendering sample HTML to PDF and
exporting diagnostics JSON. Use it to inspect visual output, compare
diagnostics JSON, or reproduce a scenario outside unit tests.

It is not a replacement for automated tests.

## Basic Render

Run from the repository root.

```powershell
dotnet run --project src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj -- src/Tests/Html2x.TestConsole/html/all-supported-features.html build/all-supported-features.pdf
```

## Render With Diagnostics

```powershell
dotnet run --project src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj -- src/Tests/Html2x.TestConsole/html/all-supported-features.html build/all-supported-features.pdf --diagnostics --diagnostics-json build/diagnostics/session.json
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

Generated PDFs, diagnostics JSON, and logs belong under `build/`. Keep
generated artifacts out of durable docs unless a test fixture explicitly
requires them.
