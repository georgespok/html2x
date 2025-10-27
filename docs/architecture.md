# Architecture Overview

Html2x follows a modular architecture with clear separation between parsing and rendering.

## Architecture Layers

### Html2x.Core
Core layout models and fragments representing the rendered output structure.

- `Fragment` - Base class for layout fragments
- `BlockFragment` - Block-level layout elements
- `LineBoxFragment` - Text line containers
- `TextRun` - Individual text segments
- `ImageFragment` - Image elements
- `LayoutPage` - Page layout container

### Html2x.Layout
Layout engine that transforms HTML/CSS into a fragment tree.

- `LayoutBuilder` - Main orchestrator for layout construction
- Converts HTML/CSS into fragment hierarchy

### Html2x.Pdf
PDF renderer that consumes fragments and produces PDF output.

- `PdfRenderer` - Renders fragments to PDF
- `PdfOptions` - Configuration for PDF generation

## Data Flow

```
HTML/CSS
  ↓
DOM + CSSOM (AngleSharp)
  ↓
Style Tree (computed styles)
  ↓
Box Tree (layout model)
  ↓
Fragment Tree (lines/pages)
  ↓
Renderer (PDF via QuestPDF)
```

This design allows adding new renderers (SVG, Canvas) without modifying the layout engine.

## Future Renderers

The architecture supports:
- `Html2x.Svg` - SVG output
- `Html2x.Canvas` - Canvas rendering
- Any other output format

All renderers consume the same fragment tree produced by the layout engine.
