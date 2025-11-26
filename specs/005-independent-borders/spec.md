# Feature Specification: Independent Borders

**Feature Branch**: `005-independent-borders`  
**Created**: 2025-11-25  
**Status**: Draft  
**Input**: User description: "The current implementation simplifies all borders to a single uniform style (width, colour, line style) applied to all four sides. This limitation prevents accurate rendering of common CSS patterns like specific underlines (`border-bottom`), side accents (`border-left`), or different styles per side. This feature aims to support standard CSS box model behaviour where each side can be independently styled."

<!--
  CONSTITUTION NOTE (Principle VII):
  - Write in plain, simple English suitable for a junior developer.
  - Illustrate key architectural or logic points with short code sketches.
  - Avoid jargon where simple explanations suffice.
-->

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Distinct Styles Per Side (Priority: P1)

A developer wants to apply different border styles to each side of a generic container (div) so that they can create complex visual frames or indicators (e.g., a left-side accent color).

**Why this priority**: This is the core functionality requested—breaking the limitation of uniform borders.

**Independent Test**: Can be fully tested by rendering an HTML file with a `div` having distinct `border-top`, `border-right`, `border-bottom`, and `border-left` styles and verifying the PDF output shows four distinct sides meeting at the corners.

**Acceptance Scenarios**:

1. **Given** a div with `border-left: 5px solid red` and `border-right: 1px solid blue`, **When** rendered to PDF, **Then** the left border is thick red, the right border is thin blue, and top/bottom have no border (or default).
2. **Given** a div with `border-style: solid`, `border-color: red green blue yellow` (clockwise), **When** rendered, **Then** each side appears in its respective color.
3. **Given** a div with different widths `border-width: 10px 1px 5px 20px`, **When** rendered, **Then** the content box is inset by the correct amount on each side.

---

### User Story 2 - Single Side Borders (Priority: P2)

A developer wants to add a simple underline to a heading or a bottom border to a table row without boxing the entire element.

**Why this priority**: Common use case (underlines, separators) that relies on the ability to have *zero* width borders on some sides.

**Independent Test**: Create an HTML sample with `border-bottom: 1px solid black` and verify only the bottom line is drawn.

**Acceptance Scenarios**:

1. **Given** an `h1` element with `border-bottom: 2px solid black`, **When** rendered, **Then** a line appears only under the element.
2. **Given** a `span` with `border-bottom: 1px dotted grey`, **When** rendered, **Then** the dotted line appears under the text run.

### Edge Cases

- **Corner Joins**: Corners will be rendered using simple rectangular overlap. Diagonal miters are NOT supported in this version.
- **Zero Width**: Explicitly setting `border-left-width: 0` should prevent drawing even if `border-style` is solid.
- **Transparent Colors**: `border-color: transparent` should result in no visible stroke but still occupy space.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST support parsing and storing independent values for `border-top-width`, `border-right-width`, `border-bottom-width`, `border-left-width`.
- **FR-002**: System MUST support parsing and storing independent values for `border-*-style` (solid, dashed, dotted, none, etc.).
- **FR-003**: System MUST support parsing and storing independent values for `border-*-color`.
- **FR-004**: Layout engine MUST calculate the content box position based on the CSS `content-box` model, subtracting the specific border width of each side from the total available width/height.
- **FR-005**: Renderer MUST draw each border side individually with its specific style and color.
- **FR-006**: Renderer MUST handle corners where borders meet using simple rectangular overlap, utilizing custom SkiaSharp canvas drawing. Diagonal miters are out of scope.

### Key Entities

- **Box Model**: Currently likely assumes a single `Border` object. Needs to evolve to hold `Top`, `Right`, `Bottom`, `Left` border definitions.
- **ComputedStyle**: Needs to expose the 12 individual properties (width, style, color * 4 sides) or 4 composite properties.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Border shorthand properties, supporting `None`, `Solid`, `Dashed`, `Dotted` styles and `Px` units only, render correctly when expanded to individual sides.
- **SC-002**: Visual inspection of existing uniform borders confirms no regressions in rendering (backward compatibility).
- **SC-003**: A new test sample `independent-borders.html` renders borders that visually match the simplified rectangular overlap model.

## Clarifications
### Session 2025-11-25
- Q: Corner rendering strategy → A: Rectangular Overlap via Custom SkiaSharp Drawing. **UPDATE**: Miter requirement dropped to simplify initial canvas implementation.
- Q: Should `border-radius` interaction be supported in this feature? → A: No (Out of scope for this feature)
- Q: What CSS box sizing model should the layout engine assume for width/height calculations? → A: `content-box` (Standard CSS default)
