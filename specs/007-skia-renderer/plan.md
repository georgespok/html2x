# Implementation Plan: SkiaSharp Renderer Migration

**Branch**: `007-skia-renderer` | **Date**: 2025-12-06 | **Spec**: specs/007-skia-renderer/spec.md
**Input**: Feature specification from `/specs/007-skia-renderer/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Replace the QuestPdf-based PDF renderer with a SkiaSharp pipeline that draws directly from layout fragments using absolute coordinates. Renderer becomes stateless and deterministic, defers all geometry to `Html2x.LayoutEngine`, and removes all QuestPdf dependencies while keeping existing HTML inputs and diagnostics flows.

## Technical Context

**Language/Version**: C# 12 on .NET 8
**Primary Dependencies**: SkiaSharp (latest stable), Html2x.LayoutEngine, Html2x.Abstractions
**Storage**: N/A
**Testing**: xUnit via `dotnet test Html2x.sln -c Release`
**Target Platform**: Cross-platform (Windows/Linux) .NET 8; PDF output only
**Project Type**: Library + console harness
**Performance Goals**: Rendering latency comparable to QuestPdf path on current samples; deterministic geometry logs across runs
**Constraints**: No layout corrections in renderer; remove QuestPdf packages and namespaces; deterministic output; renderer stateless
**Scale/Scope**: Existing Html2x coverage: single-page PDFs, Latin fonts, text, images, shapes; no multi-page or RTL in scope

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **[I. Staged Layout]** Plan keeps dependencies flowing Abstractions -> LayoutEngine -> Renderers.Skia
- [x] **[II. Predictability]** Renderer is deterministic; no layout corrections or environment probes
- [x] **[III. Test-First]** Tests planned around observable rendering and diagnostics; QuestPdf removal validated by build/tests
- [x] **[IV. Diagnostics]** Renderer will surface diagnostics from layout; failures log context and rethrow
- [x] **[V. Extensibility]** Extension points documented in `docs/`
- [x] **[VI. Goal-Driven]** Plan follows staged research/design/tasks with explicit assumptions
- [x] **[VII. Accessible Docs]** Plan, research, and quickstart will be plain English with sketches where needed
- [x] **[Delivery]** Plan includes new HTML sample under `src/Tests/Html2x.TestConsole/html/`

## Project Structure

### Documentation (this feature)

```
specs/007-skia-renderer/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- tasks.md (Phase 2 via /speckit.tasks)
```

### Source Code (repository root)

```
src/
|-- Html2x.Abstractions/
|-- Html2x.Diagnostics/
|-- Html2x.LayoutEngine/
|-- Html2x.Renderers.Pdf/  (QuestPdf removal, SkiaSharp renderer)
|-- Html2x/                (composition, public APIs)

src/Tests/
|-- Html2x.LayoutEngine.Test/
|-- Html2x.Renderers.Pdf.Test/
|-- Html2x.Test/           (scenario/integration)
|-- Html2x.TestConsole/    (manual harness, fonts, html samples)
```

**Structure Decision**: Use existing multi-assembly layout with new SkiaSharp renderer housed in `Html2x.Renderers.Pdf`; diagnostics and contracts stay in `Html2x.Abstractions`; samples live in `Html2x.TestConsole`.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|---------------------------------------|
| N/A | N/A | N/A |
