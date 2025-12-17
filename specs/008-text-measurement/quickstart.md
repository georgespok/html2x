# Quickstart: Font Accurate Text Measurement

## Goal
Enable font-accurate layout measurements using a strict font source path.

## Steps
1. Provide a font source path containing the fonts used by your document.
2. Configure Html2x conversion to use that font source path.
3. Run the conversion and verify diagnostics if a font cannot be resolved.

## Expected Result
- Layout uses the provided fonts for measurement.
- Missing or invalid fonts fail early with a clear diagnostic.

## Sample (conceptual)

```text
1) Set FontPath to a directory with your .ttf or .otf files
2) Convert HTML to PDF
3) Review diagnostics if conversion fails
```
