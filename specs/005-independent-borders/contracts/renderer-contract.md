# Renderer Contract

**Scope**: Internal contract between `Html2x.LayoutEngine` and `Html2x.Renderers.Pdf`.

## Interface Changes

### `BorderEdges` (Existing)
No changes to the type itself, but the **interpretation** by the renderer changes.

**Previous Behavior**:
- Renderer checks `GetUniformBorder()`.
- If distinct sides, returns `null` (no border).

**New Behavior**:
- Renderer MUST inspect `Top`, `Right`, `Bottom`, `Left` individually.
- Renderer MUST draw each side that has `Width > 0` and `Style != None` using `BorderShapeDrawer`.

## Corner Rendering Protocol

The renderer is responsible for the geometry of corner joins.

**Algorithm (Rectangular Overlap)**:
1. **Top**: Rectangle covering full width `(0,0, W, T)`.
2. **Right**: Rectangle covering full height `(W-R, 0, R, H)`.
3. **Bottom**: Rectangle covering full width `(0, H-B, W, B)`.
4. **Left**: Rectangle covering full height `(0, 0, L, H)`.

**Drawing**:
- Each rectangle is drawn independently using `SkiaSharp`.
- Overlap order is Top -> Right -> Bottom -> Left (Left draws on top of Top/Bottom).

**Constraint**:
- This logic MUST reside in `Html2x.Renderers.Pdf.Drawing.BorderShapeDrawer`.
- The Layout Engine only provides the widths/styles via `Box.Borders`.