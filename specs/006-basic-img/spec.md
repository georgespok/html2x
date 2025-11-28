# Feature Specification: Basic `<img>` Support

**Feature Branch**: `[006-basic-img]`  
**Created**: 2025-11-28  
**Status**: Draft  
**Input**: User description: "Basic <img> support (Load image, Explicit width/height, Keep aspect ratio, Default HTML display model inline-block). No need for full display: yet"

<!--
  CONSTITUTION NOTE (Principle VII):
  - Write in plain, simple English suitable for a junior developer.
  - Illustrate key architectural or logic points with short code sketches.
  - Avoid jargon where simple explanations suffice.
-->

## User Scenarios & Testing *(mandatory)*

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

### User Story 1 - Inline image renders with explicit size (Priority: P1)

Content authors include `<img>` tags with width or height attributes and expect the generated output to show the images at the specified size while preserving their proportions.

**Why this priority**: Precise sizing is essential for layout fidelity in documents and is the core value of adding image support.

**Independent Test**: Render a sample HTML containing images with explicit width or height; verify the output shows images at expected dimensions without distortion.

**Acceptance Scenarios**:

1. **Given** an HTML document with `<img src>` and `width="120"` set, **When** the document is rendered, **Then** the image appears at 120px width with proportional height.
2. **Given** an HTML document with `height="80"` only, **When** it is rendered, **Then** the image height is 80px and width scales to maintain aspect ratio.

---

### User Story 2 - Images align with surrounding text (Priority: P2)

Writers expect images to behave like inline-block elements so text flows naturally before and after each image.

**Why this priority**: Inline alignment keeps reading flow intact for reports and rich text documents.

**Independent Test**: Render a paragraph with an inline image; confirm text continues on the same line until wrapping, with baseline alignment consistent with inline-block behavior.

**Acceptance Scenarios**:

1. **Given** a paragraph with an inline `<img>` mid-sentence, **When** rendered, **Then** text appears on the same line until natural wrap and the image sits on the baseline like other inline replaced elements.

---

### User Story 3 - Graceful handling when images fail (Priority: P3)

Editors want documents to remain readable even if an image source is missing or unreachable.

**Why this priority**: Prevents broken layouts and preserves document meaning when assets are unavailable.

**Independent Test**: Render HTML with an invalid image URL; confirm the renderer signals the missing asset and preserves surrounding layout without crashes.

**Acceptance Scenarios**:

1. **Given** an `<img>` whose source cannot be retrieved, **When** rendered, **Then** a placeholder or empty inline-box of expected size appears and the rest of the text remains correctly positioned.

---

### Edge Cases
- Missing or empty `src` attribute.
- Image resource exceeds page or container bounds; must scale down while keeping aspect ratio.
- Unsupported or corrupted image format.
- Local file path not found or inaccessible.
- Conflicting width and height attributes that do not match the intrinsic aspect ratio.
- High-DPI images that require density-aware sizing to avoid oversized rendering.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST load images referenced by `<img src>` when the resource is a local file path or embedded data URI in a supported format (JPEG, PNG, GIF, SVG); remote network retrieval is out of scope for this feature.
- **FR-002**: System MUST apply explicit `width` and/or `height` attributes from HTML to the rendered image box, interpreting numeric values as CSS pixels.
- **FR-003**: System MUST preserve image aspect ratio when only one dimension is provided; the unspecified dimension is derived from the intrinsic ratio.
- **FR-004**: System MUST behave as inline-block for `<img>` elements by default, allowing text to flow on the same line until wrapping, with baseline alignment consistent with inline replaced elements.
- **FR-005**: System MUST scale images down to fit within page or container bounds while keeping aspect ratio; scaling up beyond intrinsic size is optional per rendering quality settings.
- **FR-006**: System MUST expose a visible or logged indication when an image fails to load while keeping the surrounding layout intact.
- **FR-007**: System MUST handle images with density metadata (e.g., DPI) so that rendered size reflects the declared CSS pixel intent rather than raw pixel count.
- **FR-008**: System MUST, when both `width` and `height` are provided and conflict with intrinsic aspect ratio, honor the authored `width` and adjust `height` to preserve aspect ratio (no distortion).

### Key Entities *(include if feature involves data)*

- **Image Source**: Reference to an external or embedded resource identified by the `src` attribute, with metadata such as format, intrinsic dimensions, and retrieval status.
- **Rendered Image Box**: The inline visual box produced after layout, with computed width, height, baseline alignment, and scaling factors relative to intrinsic size.

### Assumptions

- Allowed image schemes: data URIs and local file paths only; HTTP(S) or other network schemes are not supported in this feature.
- Supported formats include JPEG, PNG, GIF (static), and SVG; others may fail gracefully without blocking rendering.
- When no `width` or `height` is specified, the intrinsic dimensions map directly to CSS pixels unless constrained by container bounds.

