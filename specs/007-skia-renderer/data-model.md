# Data Model: SkiaSharp Renderer

## Fragment hierarchy (current + planned adjustments)
- **Fragment (abstract)**: `Rect: RectangleF` (absolute page coords), `ZOrder: int`, `Style: VisualStyle`, **add** `FragmentId: int` (global sequence per render), **add** `PageNumber: int`.
- **BlockFragment**: `IList<Fragment> Children`.
- **LineBoxFragment**: `BaselineY`, `LineHeight`, `IReadOnlyList<TextRun> Runs`, `TextAlign`.
- **ImageFragment**: `Src`, `AuthoredWidthPx`, `AuthoredHeightPx`, `IntrinsicWidthPx`, `IntrinsicHeightPx`, `IsMissing`, `IsOversize`.
- **RuleFragment**: horizontal rule/line (no extra fields).
- **TextRun (record)**: `Text`, `FontKey`, `FontSizePt`, `Origin` (baseline absolute), `AdvanceWidth`, `Ascent`, `Descent`, `TextDecorations`, `ColorRgba?`.

Validation rules:
- `Rect.Width/Height >= 0`, not NaN/Infinity; `PageNumber >= 0`.
- `FragmentId` stable, monotonic within a render pass (single global counter).

## RenderInstruction (renderer input / mapping target)
- `FragmentId: int`
- `PageNumber: int`
- `Command: DrawText | DrawImage | DrawRule`
- `Geometry: x, y, width, height, zIndex`
- `Payload`:
  - Text: `TextRun` data, color, paints
  - Image: bitmap source id, intrinsic size, object fit
  - Rule: stroke width/color/length

State transitions:
1. LayoutEngine builds `FragmentTree` (Blocks + All list) assigning `Id` and `PageNumber`.
2. Renderer maps each `Fragment` to `RenderInstruction` without geometry changes.
3. Renderer iterates per page, emits diagnostics/logs on failures with `FragmentId`.

## Diagnostics Payload (renderer-visible, separate stream from fragments)
- `FragmentId: int`
- `SourcePath: string?` (optional DOM path/node id for traceability)
- `Code: ImageMissing | ImageOversize | FontFallback | RenderFailure`
- `Message: string`
- `Data: key/value` (e.g., requestedSize, asset path)
- `Severity: Info | Warning | Error`

Relationships:
- `BlockFragment` aggregates `Fragment` children.
- `LineBoxFragment.Runs` composed of `TextRun`.
- `Fragment` -> `RenderInstruction` one-to-one.
