# Implementation Plan: Independent Borders

**Branch**: `005-independent-borders` | **Date**: 2025-11-25 | **Spec**: [specs/005-independent-borders/spec.md](../spec.md)
**Input**: Feature specification from `/specs/005-independent-borders/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Support independent border styling (width, style, color) for each side of a block element using **custom SkiaSharp canvas drawing** with a simplified **rectangular overlap** geometry. This avoids the complexity of miter math and container nesting while ensuring independent side rendering.

## Technical Context

**Language/Version**: C# (.NET 8.0)
**Primary Dependencies**: 
- QuestPDF (PDF Generation)
- AngleSharp (CSS Parsing)
- **SkiaSharp** (New Dependency for custom drawing)
**Testing**: xUnit, Html2x.TestConsole (Visual Inspection)
**Target Platform**: Cross-platform (Windows/Linux/macOS)
**Project Type**: Library (Html2x) + Console App (Test)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **[I. Staged Layout]** Data model updates in `Abstractions`, rendering in `Renderers.Pdf`.
- [x] **[II. Predictability]** Deterministic rendering via explicit geometry logic.
- [x] **[III. Test-First]** Visual inspection strategy defined in Spec.
- [x] **[IV. Diagnostics]** No new diagnostics needed, visual output is key.
- [x] **[V. Extensibility]** Extends existing `BorderEdges` usage.
- [x] **[VI. Goal-Driven]** Plan tracks state transitions.
- [x] **[VII. Accessible Docs]** Plan written in plain English.
- [x] **[Delivery]** Includes task for `independent-borders.html` sample.

## Project Structure

### Documentation (this feature)

```text
specs/005-independent-borders/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (N/A - internal library)
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)

```text
src/
├── Html2x.Abstractions/
│   └── Layout/Styles/     # Border data models (existing)
├── Html2x.Renderers.Pdf/
│   ├── Rendering/         # QuestPdfFragmentRenderer (logic change)
│   └── Drawing/           # New folder for canvas logic
│       └── BorderShapeDrawer.cs # New class
└── Tests/
    └── Html2x.TestConsole/html/ # New test sample
```

**Structure Decision**: Extend existing project structure. No new projects required.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| SkiaSharp Dependency | Required for independent side drawing without nesting. | Nesting containers causes layout artifacts; Miters are too complex. |