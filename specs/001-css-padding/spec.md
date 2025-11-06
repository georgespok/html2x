# Feature Specification: CSS Padding Support

**Feature Branch**: `001-css-padding`  
**Created**: 2025-11-06  
**Status**: Draft  
**Input**: User description: "add support for css "padding" attribute. support for shorthand as well as individual for each side. ignore any backward compatibility if any issue arise"

## Clarifications

### Session 2025-11-06

- Q: Should padding support apply to all element types (block, inline, table cells), or only to block-level elements? → A: Support all applicable element types currently implemented (block and inline). Design should remain extensible for future table and table cell support.

## User Scenarios & Testing (mandatory)

Constitution alignment: Padding support follows the staged layout discipline (Principle I) by extending style computation, propagating through the box model, and surfacing on fragments without bypassing pipeline contracts. Deterministic rendering (Principle II) is preserved through test-driven development with explicit assertions on fragment geometry. Observability (Principle IV) requires structured logging when padding values are parsed, applied, or when invalid values are encountered.

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.

  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - Individual Padding Properties (Priority: P1)

A developer wants to apply padding to individual sides of an HTML element using CSS properties `padding-top`, `padding-right`, `padding-bottom`, and `padding-left` with pixel values (e.g., `10px`, `20px`). The padding should affect the content area of the element, reducing the available space for child content while maintaining the element's overall dimensions.

**Element Scope**: Padding applies to all currently implemented element types (block and inline elements). The design must remain extensible for future table and table cell support, but table elements are out of scope for this iteration.

**Note**: Only `px` (pixel) units are supported in this iteration. Other units (pt, em, rem, %, in, cm, mm) may be added in future iterations, but percentage and relative units (em, rem) are not planned due to complexity.

**Why this priority**: Individual side control is the foundation for padding support. It enables precise layout control and is required before shorthand can be meaningful. This provides immediate value for common use cases like asymmetric padding needs.

**Independent Test**: Add a failing test in `CssStyleComputerTests` that parses HTML with `style="padding-top: 20px; padding-right: 15px; padding-bottom: 10px; padding-left: 5px"` and asserts that `ComputedStyle` contains the correct point values for each side. Verify the values are converted from CSS units to points deterministically.

**Acceptance Scenarios**:

1. **Given** an HTML element with `style="padding-top: 20px"`, **When** the layout is computed, **Then** the element's content area is reduced by 15 points from the top (20px * 0.75 = 15pt), and the computed style contains `PaddingTopPt = 15`.
2. **Given** an HTML element with `style="padding-right: 15px"`, **When** the layout is computed, **Then** the padding-right is 11.25 points (15px * 0.75 = 11.25pt), and the content area is reduced accordingly.
3. **Given** an HTML element with `style="padding-bottom: 10px"`, **When** the layout is computed, **Then** the padding-bottom is 7.5 points (10px * 0.75 = 7.5pt), and the content area reflects this reduction.
4. **Given** an HTML element with `style="padding-left: 5px"`, **When** the layout is computed, **Then** the padding-left is 3.75 points (5px * 0.75 = 3.75pt), and child content is positioned 3.75 points from the left edge of the element's content box.

---

### User Story 2 - Padding Shorthand (Priority: P2)

A developer wants to use the CSS `padding` shorthand property to set padding values for one, two, three, or four sides following CSS specification rules (1 value = all sides, 2 values = vertical/horizontal, 3 values = top/horizontal/bottom, 4 values = top/right/bottom/left). The shorthand should be parsed and expanded into individual side values, with individual side properties taking precedence when both are specified.

**Why this priority**: Shorthand support is essential for developer ergonomics and follows standard CSS conventions. It reduces verbosity and aligns with how developers typically write CSS. However, it depends on individual property support (P1) being in place first.

**Independent Test**: Add a failing test in `CssStyleComputerTests` that exercises all shorthand forms: `padding: 10px` (all sides), `padding: 10px 20px` (vertical/horizontal), `padding: 10px 20px 15px` (top/horizontal/bottom), and `padding: 10px 20px 15px 5px` (top/right/bottom/left). Assert that the computed style contains correct individual side values. Test precedence: when both `padding: 10px` and `padding-top: 20px` are specified, `padding-top` should win.

**Acceptance Scenarios**:

1. **Given** an HTML element with `style="padding: 15px"`, **When** the layout is computed, **Then** all four sides (top, right, bottom, left) have padding of 11.25 points (15px * 0.75 = 11.25pt).
2. **Given** an HTML element with `style="padding: 10px 20px"`, **When** the layout is computed, **Then** top and bottom padding are 7.5 points (10px * 0.75), and right and left padding are 15 points (20px * 0.75).
3. **Given** an HTML element with `style="padding: 10px 20px 15px"`, **When** the layout is computed, **Then** top padding is 7.5 points (10px * 0.75), right and left padding are 15 points (20px * 0.75), and bottom padding is 11.25 points (15px * 0.75).
4. **Given** an HTML element with `style="padding: 10px 20px 15px 5px"`, **When** the layout is computed, **Then** padding values are top=7.5pt (10px * 0.75), right=15pt (20px * 0.75), bottom=11.25pt (15px * 0.75), left=3.75pt (5px * 0.75).
5. **Given** an HTML element with `style="padding: 10px; padding-top: 25px"`, **When** the layout is computed, **Then** padding-top is 18.75 points (25px * 0.75, individual property overrides shorthand), and other sides remain 7.5 points (10px * 0.75).

---

### User Story 3 - Padding in Box Layout (Priority: P3)

A developer expects padding to affect the layout of block and inline elements, reducing the available content area where child elements are positioned. Padding should be applied inside the border (if present) and outside the content, affecting the positioning of child content but not the element's overall dimensions relative to its container. For inline elements, padding affects horizontal spacing and may influence line-breaking behavior.

**Why this priority**: Parsing padding values is necessary but insufficient if padding doesn't affect layout. This story ensures padding values flow through the pipeline and influence box geometry. It depends on P1 and P2 being implemented, as it uses the computed padding values.

**Independent Test**: Add a failing test in `BoxTreeBuilderTests` or `LayoutIntegrationTests` that creates a block element with `style="width: 200px; padding: 20px"` containing child content. Assert that the child content's available width is 120 points (200px * 0.75 = 150pt total, minus 30pt horizontal padding) and that the child's X position accounts for the left padding (15pt). Verify that the block's total width remains 150 points (converted from 200px), but the content area is reduced.

**Acceptance Scenarios**:

1. **Given** a block element with `style="width: 200px; padding: 20px"` containing text, **When** the layout is computed, **Then** the text content area has an effective width of 120 points (200px * 0.75 = 150pt total width, minus 30pt total horizontal padding = 120pt), and text starts 15 points (20px * 0.75) from the left edge of the element.
2. **Given** a block element with `style="padding-top: 30px; padding-bottom: 10px"` containing multiple child blocks, **When** the layout is computed, **Then** the first child block is positioned 22.5 points below the top border (30px * 0.75), and spacing accounts for bottom padding (10px * 0.75 = 7.5pt).
3. **Given** a block element with asymmetric padding `style="padding: 10px 20px 15px 5px"`, **When** the layout is computed, **Then** child content is positioned with correct offsets: 7.5pt from top (10px * 0.75), 15pt from right (20px * 0.75), 11.25pt from bottom (15px * 0.75), 3.75pt from left (5px * 0.75).

---

### Edge Cases

- **Invalid values**: When padding values are invalid (e.g., negative lengths, non-numeric strings), the system should log a warning and fall back to 0 padding for that side. Structured logs should include the element context and the invalid value for debugging.
- **Unsupported units**: Only `px` (pixel) units are supported. If other units are encountered (e.g., `padding: 10pt`, `padding: 1em`, `padding: 10%`, `padding: 1rem`), the system should log a warning that the unit is not supported and treat it as 0. Percentage and relative units (em, rem) are not planned for future support due to complexity. Other absolute units (pt, in, cm, mm) may be added in future iterations.
- **Unit conversion edge cases**: Very large pixel values (e.g., `padding: 10000px`) should be handled gracefully without overflow. Conversion from pixels to points should be deterministic and consistent across platforms, using the same conversion logic as margin properties.
- **Inheritance**: Padding does not inherit in CSS. If a parent has `padding: 20px` and a child has no padding specified, the child should have 0 padding, not inherit the parent's padding. Tests should verify this behavior.
- **Zero and auto values**: `padding: 0` should result in 0 padding on all sides. `padding: auto` is invalid for padding (unlike margin) and should be treated as 0 with a warning logged.

## Requirements (mandatory)

### Functional Requirements

- **FR-001**: Implementation MUST respect pipeline contracts; padding values MUST flow through style computation → box model → fragments without bypassing stages (Principle I).
- **FR-002**: Padding values MUST produce deterministic layout outputs for identical HTML/CSS inputs. Development and testing occur on Windows; cross-platform compatibility is assumed through .NET Core's platform-agnostic runtime (Principle II).
- **FR-003**: Automated tests MUST be authored first following incremental TDD: one failing test per user story before implementation begins (Principle III).
- **FR-004**: Structured logging MUST emit warnings when invalid padding values are encountered, including element context and the invalid value (Principle IV).
- **FR-005**: Padding support MUST be documented in `docs/extending-css.md` or relevant architecture docs, and release notes MUST mention the new CSS property support (Principle V).
- **FR-006**: CSS padding shorthand parsing MUST support all four forms: single value (all sides), two values (vertical/horizontal), three values (top/horizontal/bottom), and four values (top/right/bottom/left).
- **FR-007**: Individual padding properties (`padding-top`, `padding-right`, `padding-bottom`, `padding-left`) MUST take precedence over shorthand `padding` when both are specified on the same element.
- **FR-008**: Padding values MUST support only `px` (pixel) units. Values in `px` MUST be converted to points deterministically using the same conversion logic as margin properties. Other units (pt, em, rem, %, in, cm, mm) MUST be rejected with a warning and treated as 0. Future iterations may add support for other absolute units (pt, in, cm, mm), but percentage and relative units (em, rem) are not planned.
- **FR-009**: Padding MUST affect the content area of block and inline elements (currently implemented element types), reducing available space for child content while maintaining the element's total dimensions (width/height) relative to its container. The implementation MUST be designed to allow future extension for table and table cell elements without architectural changes.
- **FR-010**: Padding MUST NOT inherit from parent elements (padding is not an inheritable property in CSS).
- **FR-011**: Invalid padding values (negative lengths, non-numeric strings, `auto`, unsupported units) MUST be treated as 0 with a warning logged, ensuring the layout pipeline continues without errors.
- **FR-012**: Padding values MUST be stored in `ComputedStyle` as separate properties (`PaddingTopPt`, `PaddingRightPt`, `PaddingBottomPt`, `PaddingLeftPt`) following the same pattern as margin properties.

### Key Entities (include if feature involves data)

- **ComputedStyle**: Immutable record representing computed CSS styles for an element. Must be extended with padding properties (`PaddingTopPt`, `PaddingRightPt`, `PaddingBottomPt`, `PaddingLeftPt`) following the margin property pattern.
- **BlockBox**: Box model representation of block-level elements. Must account for padding when calculating content area dimensions and child positioning.
- **InlineBox**: Box model representation of inline elements. Must account for padding when calculating content area and line-breaking behavior.
- **Spacing**: Existing class used for margin values. May be reused or extended for padding if the box model requires it, or padding may be stored separately if layout logic differs significantly. Design must support future extension to table cell elements.

## Success Criteria (mandatory)

### Measurable Outcomes

- **SC-001**: `dotnet test Html2x.sln -c Release` passes on Windows for the updated solution, with all new padding-related tests included.
- **SC-002**: Integration tests confirm deterministic fragment geometry for HTML elements with padding. Identical HTML/CSS inputs produce identical fragment positions and dimensions.
- **SC-003**: Observability signals (structured logs) expose padding parsing events, invalid value warnings, and unsupported unit warnings. Logs are traceable in the test harness (`Html2x.Pdf.TestConsole`) and include element context.
- **SC-004**: Documentation in `docs/extending-css.md` or equivalent is updated to reflect padding support, including examples of shorthand parsing and precedence rules. Release notes document the new CSS property support.
- **SC-005**: Padding values correctly reduce content area: a block element with `width: 200px; padding: 20px` has child content positioned within a 120-point wide area (200px * 0.75 = 150pt total width, minus 30pt total horizontal padding = 120pt).
- **SC-006**: All four shorthand forms are supported and tested: single value (all sides), two values (vertical/horizontal), three values (top/horizontal/bottom), four values (top/right/bottom/left).
- **SC-007**: Individual padding properties override shorthand when both are present on the same element, verified through explicit test assertions.
