# ITextMeasurer Contract

## Purpose
Measure text widths and vertical metrics using the exact font files provided at runtime.

## Responsibilities
- Measure width of a text run in points
- Provide ascent and descent in points for a font and size

## Inputs
- Font key (family, weight, style)
- Font size in points
- Text string

## Outputs
- Width in points
- Ascent and descent in points

## Error Handling
- If a font cannot be resolved, the caller must fail with a diagnostic
