# Data Model: SkiaSharp Renderer

## Fragment (from LayoutEngine)
- id: string/Guid
- type: enum {Text, Image, Shape}
- x, y, width, height: double (absolute, page coordinates)
- zIndex: int (stack order)
- content:
  - Text: string text, fontFamily, fontSize, fontStyle, fontWeight, color, letterSpacing, lineHeight, alignment
  - Image: uri or blob id, intrinsicWidth, intrinsicHeight, mimeType, objectFit
  - Shape: path commands, stroke, fill, opacity, clip
- diagnostics: list of diagnostic items (missing asset, oversized asset, font fallback)
- pageNumber: int (zero-based)

Validation rules:
- width, height >= 0; NaN/Infinity rejected
- coordinates must fit target page bounds; clipping handled by renderer but geometry must be finite
- content must be non-null for given type

## RenderInstruction (renderer input)
- fragmentId: string
- pageNumber: int
- command: enum {DrawText, DrawImage, DrawPath}
- geometry: x, y, width, height, zIndex
- payload:
  - DrawText: font, size, color, text spans
  - DrawImage: bitmap source id, scaling mode
  - DrawPath: path data, stroke/fill paints, opacity
- diagnostics: propagated from fragment

State transitions:
1. LayoutEngine emits Fragment list with absolute geometry.
2. Renderer maps each Fragment to a RenderInstruction without changing geometry.
3. Instructions streamed to `SKCanvas` per page; failures log fragmentId, pageNumber, command.

## Diagnostics Payload
- sourceFragmentId: string
- code: enum {ImageMissing, ImageOversize, FontFallback, RenderFailure}
- message: string
- data: key/value metadata (e.g., requestedSize, availableFonts)
- severity: enum {Info, Warning, Error}

Relationships:
- Fragment -> Diagnostics (one-to-many)
- Fragment -> RenderInstruction (one-to-one per fragment, immutable geometry)
- RenderInstruction -> Diagnostics (contains forwarded diagnostics)
