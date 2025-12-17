# Feature Specification: Font Accurate Text Measurement

**Feature Branch**: `008-text-measurement`  
**Created**: December 27, 2025  
**Status**: Draft  
**Input**: User description: "Add font-accurate text measurement and shaping so layout decisions use the exact font files provided at render time, with strict font resolution, early failure on missing fonts, and strong diagnostics."

## Clarifications

### Session 2025-12-27

- Q: Define an explicit out-of-scope boundary for this MVP. -> A: No new renderers or output formats.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Correct measurement from provided fonts (Priority: P1)

As a developer generating documents, I need layout measurements to use the exact fonts I provide so that line breaks, widths, and baselines are correct.

**Why this priority**: Correct layout is the primary goal and affects every output.

**Independent Test**: Can be fully tested by providing a known font set and verifying that measured widths and baselines match expected values for a sample document.

**Acceptance Scenarios**:

1. **Given** a document and a valid font set, **When** layout is computed, **Then** measured widths and baselines match the expected values for that font set.
2. **Given** mixed font styles in a document, **When** layout is computed, **Then** each segment uses the correct font metrics.

---

### User Story 2 - Reliable failure on missing or invalid fonts (Priority: P2)

As a developer, I need clear failures when fonts are missing or invalid so I can fix configuration before layout proceeds.

**Why this priority**: Early failure prevents incorrect output and speeds up troubleshooting.

**Independent Test**: Can be fully tested by referencing missing or corrupted fonts and verifying a diagnostic before any layout output is produced.

**Acceptance Scenarios**:

1. **Given** a document that references a missing font, **When** layout starts, **Then** the system fails with a clear diagnostic and no layout output is produced.
2. **Given** a document that references a corrupted font, **When** layout starts, **Then** the system fails with a clear diagnostic that identifies the invalid font.

---

### User Story 3 - Correct wrapping behavior for long text (Priority: P3)

As a developer, I need wrapping to behave predictably so long text still fits the layout without crashes or unexpected breaks.

**Why this priority**: Wrapping errors distort layout and can break documents.

**Independent Test**: Can be fully tested by using long words and mixed whitespace and verifying the produced line breaks.

**Acceptance Scenarios**:

1. **Given** a paragraph with spaces, **When** it is wrapped to a narrow width, **Then** line breaks occur at spaces when possible.
2. **Given** a long word that cannot fit a line, **When** wrapping occurs, **Then** the word is split at character boundaries.

---

### User Story 4 - Testable layout with deterministic measurement sources (Priority: P4)

As a developer, I need the layout engine to be testable with deterministic measurement sources so I can verify behavior without relying on real fonts.

**Why this priority**: Deterministic tests are needed to validate correctness and prevent regressions.

**Independent Test**: Can be fully tested by injecting a deterministic measurement source and verifying line breaks and baselines.

**Acceptance Scenarios**:

1. **Given** a deterministic measurement source, **When** layout runs, **Then** the output is stable and repeatable for the same inputs.

---

### Edge Cases

- What happens when the configured font source path exists but is empty?
- How does the system handle a corrupted font file?
- What happens when a very long word has no spaces and exceeds the line width?
- How does wrapping behave for text containing mixed scripts or emoji sequences?
- What happens when the font metadata in the document does not match any available font file?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST require an explicit font source path before layout can begin.
- **FR-002**: The system MUST resolve all fonts used by the document exclusively from the configured font source.
- **FR-003**: The system MUST compute text widths and vertical text metrics (height above and below the baseline) using the resolved font data.
- **FR-004**: The system MUST use computed text metrics for all layout decisions, including wrapping and line placement.
- **FR-005**: The system MUST provide a clear diagnostic when any required font cannot be resolved or is invalid.
- **FR-006**: The system MUST apply a space-first wrapping strategy and fall back to character-level wrapping when no valid space break exists.
- **FR-007**: The layout engine MUST depend only on abstract text measurement and font source services provided at composition time.
- **FR-008**: The layout engine MUST be testable with deterministic measurement and font sources that do not require real font files.

### Out of Scope

- No new renderers or output formats.

### Acceptance Criteria

- **AC-001**: When no font source path is provided, layout does not start and a diagnostic is returned.
- **AC-002**: When a document references a font outside the configured source, layout fails with a diagnostic identifying the missing font.
- **AC-003**: For known font files, measured widths and baselines are consistent across repeated runs with identical inputs.
- **AC-004**: Wrapping breaks at spaces when possible and falls back to character boundaries only when no space break fits.
- **AC-005**: When a font file is corrupted or unreadable, layout fails with a diagnostic that identifies the font source and reason.
- **AC-006**: Layout behavior for long unbroken text is consistent and does not crash, using character-level wrapping when needed.

### Key Entities *(include if feature involves data)*

- **Font Source**: A configured location that provides font files used for layout and rendering.
- **Font File**: A single font resource identified by name, style, and weight attributes.
- **Text Run**: A sequence of text with a uniform font selection and styling.
- **Text Metrics**: Measurements derived from the font file that describe width and vertical placement relative to the baseline.
- **Layout Result**: The computed line breaks and positioning information used for rendering.

### Assumptions

- Font files are accessible on disk at the time layout begins.
- Document inputs provide enough font metadata to resolve the intended font files.
- If multiple font files match a request, the system uses a deterministic selection order.
- Test harnesses can supply deterministic measurement data without real font files.

### Dependencies

- Valid font files are available at the configured font source path.
- Input documents provide font references that can be resolved to files.

## Success Criteria *(mandatory)*

- **SC-001**: No regressions in existing layout tests and new tests cover critical paths.
