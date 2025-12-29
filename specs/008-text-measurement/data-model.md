# Data Model: Font Accurate Text Measurement

## Entities

### Font Source
- Represents a configured location that provides font files for layout
- Fields: sourceId (stable key), path, availability status
- Validation: must resolve at least one font file when configured

### Font File
- Represents a single font resource used for measurement and rendering
- Fields: family, weight, style, sourceId, filePath
- Relationships: belongs to Font Source
- Validation: file must be readable and valid

### Text Run
- Represents a continuous segment of text with a single font selection
- Fields: text, font key, size in points
- Relationships: references Font File via resolved font key

### Text Metrics
- Represents measured width and vertical metrics for a text run
- Fields: width, ascent, descent, size in points
- Relationships: derived from Text Run and Font File

### Layout Result
- Represents line breaks and baseline placements used for rendering
- Fields: lines, line metrics, baseline offsets
- Relationships: derived from Text Runs and Text Metrics

## Validation Rules
- Missing or invalid font files must halt layout with diagnostics
- Measurements must be in points
- Wrapping must prefer whitespace breaks and fall back to character-level breaks

## State Transitions
- Font Source: configured -> validated -> used
- Text Run: created -> measured -> placed into layout
- Layout Result: computed -> emitted to renderer
