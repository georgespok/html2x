# Getting Started

This page gets a developer from a clean checkout to a verified local build.

## Prerequisites

- .NET 8 SDK.
- A shell that can run `dotnet` commands.
- Local fonts for manual PDF rendering.

## Restore, Build, Test

Run commands from the repository root.

```powershell
dotnet restore src/Html2x.sln
dotnet build src/Html2x.sln -c Release
dotnet test src/Html2x.sln -c Release
```

Use [Testing](development/testing.md) for focused test runs while developing a
specific subsystem.

## Manual Rendering Smoke Test

Use the [Manual Test Console](development/manual-test-console.md) when a change
needs local PDF output or diagnostics JSON verification.

## Expected State

After setup:

- The solution restores without missing packages.
- The release build passes.
- The test suite passes.
- Optional manual rendering writes outputs under `build/`.

If any step fails, start with [Troubleshooting](development/troubleshooting.md).
