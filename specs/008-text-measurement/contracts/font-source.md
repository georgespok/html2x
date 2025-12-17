# IFontSource Contract

## Purpose
Resolve fonts strictly from a configured font source path without renderer-specific types.

## Responsibilities
- Resolve a requested font key to a stable font descriptor

## Inputs
- Requested font key (family, weight, style)

## Outputs
- Resolved font descriptor (family, weight, style, sourceId, filePath if available)

## Error Handling
- If a font cannot be resolved or is invalid, return a failure to the caller for diagnostics
