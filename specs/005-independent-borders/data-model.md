# Data Model: Independent Borders

## Existing Structures (Reused)

The project already possesses the necessary data structures in `Html2x.Abstractions`.

### `BorderEdges`
Represents the four sides of a border.
- `BorderSide Top`
- `BorderSide Right`
- `BorderSide Bottom`
- `BorderSide Left`

### `BorderSide`
Represents a single border's attributes.
- `double Width` (in points)
- `BorderLineStyle Style` (Solid, Dashed, Dotted, None)
- `Color Color`

## Rendering Logic Model

### `BorderShapeDrawer` (New Service)
Encapsulates the SkiaSharp drawing logic.

- **Input**: `SKCanvas`, `Size`, `BorderEdges`
- **Logic**:
    1.  Draw Top Rect: `(0, 0, W, T)`
    2.  Draw Right Rect: `(W-R, 0, R, H)`
    3.  Draw Bottom Rect: `(0, H-B, W, B)`
    4.  Draw Left Rect: `(0, 0, L, H)`
    - **Styles**: If dashed/dotted, draw a stroke line centered in the rect region.

## Data Flow
1.  **Parser**: `AngleSharp` -> `CssStyleComputer` populates `ComputedStyle.Borders`.
2.  **Layout**: `LayoutBuilder` uses `BorderEdges` to calculate `ContentBox` dimensions (subtracting borders).
3.  **Rendering**: `QuestPdfFragmentRenderer` reads `BorderEdges`.
    - If all sides `Width == 0` -> No OP.
    - Else -> Invoke `BorderShapeDrawer.Draw(canvas, size, borders)`.