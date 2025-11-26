# Research: Independent Borders

## Problem Statement
The current `Html2x.Renderers.Pdf` implementation forces all borders to be uniform. Standard CSS requires independent control over Top/Right/Bottom/Left borders (width, color, style).

## Options Analysis

### Option 1: QuestPDF High-Level API (Nested Containers)
Use QuestPDF's fluent API. Requires nesting containers to achieve different colors per side.
*   **Pros**: Native layout engine.
*   **Cons**: Layout artifacts from nesting, potential Z-index issues, complex to manage padding/margins correctly with nested borders.

### Option 2: Custom Canvas Drawing (SkiaSharp) - SELECTED
Use QuestPDF's `.Element(e => ...)` or `.SkiaSharpCanvas(...)` (via helper) to drop down to the `SkiaSharp` drawing context.

*   **Pros**:
    *   **Straightforward Independent Colors**: We can issue 4 separate draw commands with different `SKPaint` colors on the same canvas layer.
    *   **Single Container**: No layout nesting required; draws borders on top of the single block.
    *   **Flexibility**: Easy to switch to diagonal miters later if needed.
*   **Cons**:
    *   **Dependency**: Requires `SkiaSharp`.
    *   **Math**: Simplified to rectangles (trivial).

## Decision
**Selected: Option 2 (Custom Canvas Drawing - Simplified)**

**Rationale**:
We choose Custom Canvas Drawing for its flexibility and cleaner architecture (avoiding deep nesting artifacts of Option 1) but simplify the geometry requirement. We will draw **rectangular strokes** that overlap at corners. This is significantly easier to implement than miters while still supporting the core requirement of independent colors and widths.

## Technical Details for Option 2

### SkiaSharp Integration
1.  **Dependency**: Add `SkiaSharp`.
2.  **API**: Use `.Canvas((canvas, size) => ...)` to access `SKCanvas`.

### Geometry Algorithm (Simplified Rectangles)
For a box `(0,0, W, H)` with borders `T, R, B, L`:

*   **Top**: Draw Rect/Line from `(0, T/2)` to `(W, T/2)` with `StrokeWidth = T`.
    *   *Refinement*: To ensure overlap covers the corner fully, we extend the line or draw a filled rectangle.
    *   **Rectangle Approach**:
        *   **Top**: Rect `(0, 0, W, T)`
        *   **Bottom**: Rect `(0, H-B, W, B)`
        *   **Left**: Rect `(0, T, L, H-T-B)` (Draw between top/bottom to avoid double overlap? Or just overlap?
        *   **Overlap Strategy**: Draw vertical sides full height `(0,0, L, H)`?
        *   **Decision**:
            *   Top: `(0, 0, W, T)`
            *   Bottom: `(0, H-B, W, B)`
            *   Left: `(0, 0, L, H)`
            *   Right: `(W-R, 0, R, H)`
    *   **Z-Order**: The drawing order determines which color is "on top" at the corner.
        *   Order: Top, Bottom, Left, Right (Arbitrary, or mimic CSS default? CSS usually miters, so no Z-order. Browsers often draw Top/Left then Bottom/Right for 3D effects, but for flat colors, last drawn wins).
        *   **Plan**: Draw Top, Right, Bottom, Left.

### Dashed/Dotted Styles
*   **Solid**: `SKPaint.Style = StrokeAndFill`.
*   **Dashed/Dotted**: `SKPaint.Style = Stroke`, `StrokeWidth = T`, `PathEffect = Dash`.
    *   Draw a line down the center of the rectangular region.

## Verification
Visual inspection will show independent colors/widths. Corners will look like overlapping blocks (e.g., a vertical bar crossing a horizontal bar).
