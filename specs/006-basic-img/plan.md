# Implementation Plan: Basic <img> Support

**Branch**: [006-basic-img] | **Date**: 2025-11-28 | **Spec**: specs/006-basic-img/spec.md
**Input**: Feature specification from specs/006-basic-img/spec.md

## Summary

Add basic <img> support to Html2x: load local and data URI images, honor explicit width/height, preserve aspect ratio, keep inline-block flow, reject oversized assets (over configurable MaxImageSizeMb, default 10 MB) and render a placeholder, show a placeholder with icon on load failure, and resolve relative sources against the input HTML directory. No new performance targets beyond the size caps. No public API surface.

## Technical Context

**Language/Version**: C# / .NET 8  
**Primary Dependencies**: QuestPDF, Html2x.Abstractions, Html2x.LayoutEngine  
**Storage**: None (in-memory only)  
**Testing**: xUnit via `dotnet test Html2x.sln -c Release`  
**Target Platform**: Windows and Linux CLI (PDF renderer)  
**Project Type**: Library + console harness  
**Performance Goals**: No explicit targets; bounded by configurable MaxImageSizeMb with rejection and placeholder  
**Constraints**: No remote HTTP(S) image retrieval; inline-block display; diagnostics required for failures and size checks; file access limited to the input HTML directory and subfolders (plus data URIs); configurable `MaxImageSizeMb` (default 10 MB); per-image diagnostics must record status, rendered size, and warnings.
**Scale/Scope**: Document-scale rendering; typical reports with inline images  
**Public API**: None; in-proc library only. Do not emit OpenAPI/GraphQL/YAML contracts.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] [I. Staged Layout] Abstractions -> Layout -> Renderers flow maintained; no reverse deps.
- [x] [II. Predictability] Size caps and deterministic scaling; failures logged with diagnostics.
- [x] [III. Test-First] Behavior-focused xUnit tests precede implementation.
- [x] [IV. Diagnostics] Emit diagnostics/log warnings for missing images and oversize rejections (placeholder shown); no downscaling.
- [x] [V. Extensibility] Image fragment/contract updates in Abstractions; docs to follow.
- [x] [VI. Goal-Driven] Assumptions recorded (local/data URI only, size caps, path scope); state transitions planned.
- [x] [VII. Accessible Docs] Plain English, code sketches as needed.
- [x] [Delivery] Add new HTML sample in src/Tests/Html2x.TestConsole/html/.

## Project Structure

### Documentation (this feature)

```
specs/006-basic-img/
|-- plan.md          # This file
|-- research.md      # Phase 0 output
|-- data-model.md    # Phase 1 output
|-- quickstart.md    # Phase 1 output
`-- tasks.md         # Phase 2 (/speckit.tasks); no public API contracts
```

### Source Code (repository root)

```
src/
|-- Html2x.Abstractions/
|-- Html2x.Diagnostics/
|-- Html2x.LayoutEngine/
|-- Html2x.Renderers.Pdf/
|-- Html2x/                 # composition layer
`-- Tests/
    |-- Html2x.LayoutEngine.Test/
    |-- Html2x.Renderers.Pdf.Test/
    |-- Html2x.Test/
    `-- Html2x.TestConsole/ (manual harness, html samples, fonts)
```

**Structure Decision**: Use existing modular assemblies (Abstractions -> LayoutEngine -> Renderers -> Pdf) with tests per module and manual harness under src/Tests/Html2x.TestConsole/.

## Rendering Approach (QuestPDF only)

- Pass supported image bytes/streams directly to QuestPDF `Image`; rely on QuestPDF for aspect preservation and inline layout.
- Reject images over the configurable `MaxImageSizeMb` (default 10 MB); emit warning and render placeholder box (no downscaling step).
- Baseline alignment: maintain inline-block layout in the layout stage; renderer uses QuestPDF inline container with baseline offset from layout metadata.
- Failure cases: when load fails or is rejected, use a small built-in placeholder PNG at expected size with missing-image icon and log warning.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|---------------------------------------|
| None | n/a | n/a |
