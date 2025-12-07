# Feature Specification: SkiaSharp Renderer Migration

**Feature Branch**: 007-skia-renderer
**Created**: 2025-12-03
**Status**: Draft
**Input**: User description: "Deprecate QuestPdf to move rendering to SkiaSharp and shift all calculation for fragments to absolute positioning in LayoutBuilder. Use the latest version of SkiSharp, install SkiSharp extensions if necessary. Don't need to support compatibility with QuestPdf. Can break public interfaces if needed. Can disable unit and integration tests at first phase then review and fix them at later feature development phases. The rationale behind the change:
1. Renderer.Pdf must be stateless and deterministic. Only do one thing: Fragment -> Draw commands
2. Renderer must not correct layout. Otherwise two layers (Renderer.Pdf and LayoutEngine) define geometry -> hard to debug -> future errors.
3. LayoutEngine remains the single source of truth. All responsibilities clearly separated.
4. Rendering becomes stable. No accidental alignment changes introduced by the renderer.
5. Stop trying to bend QuestPDF into an absolute-position renderer. It's a great layout engine, but not the right tool for Html2x rendering."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Deterministic Skia rendering (Priority: P1)
As a developer generating PDFs, I want Html2x to render via SkiaSharp using fragment absolute coordinates so every run produces the same visual output.

**Why this priority**: Deterministic output is the core goal and unlocks further refactors.

**Independent Test**: Render a fixed HTML sample twice in the same environment and verify visual parity and matching geometry annotations.

**Acceptance Scenarios**:

1. **Given** an HTML input with text and images, **When** the layout is converted to fragments, **Then** the renderer draws the PDF using only fragment geometry with no layout corrections.
2. **Given** the same HTML rendered twice, **When** outputs are compared, **Then** the PDFs are visually identical and fragment geometry matches.

---

### User Story 2 - Remove QuestPdf dependency (Priority: P2)
As a platform maintainer, I want Html2x to drop QuestPdf so the build contains only SkiaSharp for PDF output.

**Why this priority**: Eliminates dual layout sources and reduces dependency surface.

**Independent Test**: Build succeeds with SkiaSharp, and package references contain no QuestPdf assemblies; renderer APIs compile against Skia-only path.

**Acceptance Scenarios**:

1. **Given** the solution is built, **When** dependencies are inspected, **Then** no QuestPdf packages are present and renderer classes no longer import QuestPdf namespaces.

---

### User Story 3 - Controlled migration posture (Priority: P3)
As a QA lead, I want the migration to allow temporary disabling of failing renderer tests while keeping the plan to restore coverage after the Skia path stabilizes.

**Why this priority**: Enables incremental delivery without blocking other work.

**Independent Test**: Test suite tags or toggles allow Skia migration work to run while clearly reporting skipped renderer tests.

**Acceptance Scenarios**:

1. **Given** renderer tests marked for migration, **When** the suite runs, **Then** the skipped tests are reported with clear reasons and can be re-enabled via a switch.

### Edge Cases

- Very large documents or page counts should not overflow coordinate ranges or blow memory.
- Missing or oversized images must be marked by layout before rendering and rendered as placeholders without renderer-side layout fixes.
- Fonts unavailable on host should fall back predictably while keeping coordinates stable.
- Zero-size or clipped fragments should not crash drawing routines.
- High-DPI or non-A4 page sizes should preserve absolute positioning accuracy.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Layout engine MUST emit fragments with absolute x, y, width, height, and stacking order sufficient for direct drawing.
- **FR-002**: PDF renderer MUST translate fragments directly into SkiaSharp drawing commands without reflow, resizing, or overlap correction.
- **FR-003**: Solution MUST remove QuestPdf entirely (no dual-mode); runtime and build dependencies target the SkiaSharp-only pipeline.
- **FR-004**: Rendering MUST be deterministic for identical inputs and environment, producing repeatable geometry logs and visually consistent output across runs.
- **FR-005**: Migration MUST support temporarily skipped or tagged renderer tests, with documented criteria to re-enable after Skia parity is met.
- **FR-006**: Diagnostics MUST surface layout-time image issues (missing, oversize) and carry them through rendering without renderer-side validation.
- **FR-007**: Build and CLI flows MUST continue to accept existing HTML inputs, even if public APIs change, with clear migration notes in docs.
- **FR-008**: Skia renderer MUST perform no positioning or sizing calculations; it simply iterates fragments and draws, and if drawing fails it logs context and rethrows, leaving consistency to the LayoutEngine.

### Success Criteria

- Successive renders of the same HTML render visually identical output in controlled environments for the existing Html2x feature set (single page, Latin fonts, text, images, shapes).
- No QuestPdf assemblies or namespaces remain in source, binaries, or package outputs.
- Migration skips are tracked with reasons and are cleared before declaring the feature complete.

### Key Entities

- **Fragment**: Layout atom containing absolute position, size, z-order, and content type (text, image, shape).
- **RenderInstruction**: Derived drawing command sequence built from fragments for SkiaSharp output.
- **Diagnostics Payload**: Metadata passed from layout to renderer describing asset availability and size decisions.

### Assumptions

- Latest stable SkiaSharp is sufficient for current Html2x scope: single-page PDFs, Latin fonts, text drawing, images (PNG/JPEG with transparency), fills/strokes/clipping/opacity, absolute positioning.
- No multi-page, RTL, color-profile, or other unused features are required for this migration.
- Deterministic rendering is evaluated in controlled environments with fixed fonts and locale.
- Temporary test skips are limited to renderer-specific suites and will be tracked in tasks for closure.










