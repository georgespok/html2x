# Research

## Decisions

### Relative image base
- Decision: Resolve relative src against the input HTML file directory; allow absolute paths.
- Rationale: Matches browser semantics and avoids working-directory surprises.
- Alternatives considered: Use process working directory; require absolute paths only.

### Oversized image handling
- Decision: Downscale images when larger than 10 MP or 10 MB; log a warning.
- Rationale: Bounded memory/CPU cost while still rendering the asset.
- Alternatives considered: Reject over-threshold images; no limits.

### Missing image behavior
- Decision: Render an inline placeholder box at expected size with a missing-image icon and log a warning.
- Rationale: Keeps layout stable and signals the issue to users and diagnostics.
- Alternatives considered: Silent empty box; collapse layout.

### Performance targets
- Decision: No explicit performance targets; rely on size caps and downscaling.
- Rationale: Caps are sufficient for this scope; avoids premature guarantees.
- Alternatives considered: Per-image or per-document timing targets.

### Rendering pipeline (QuestPDF only)
- Decision: Pass supported image bytes/streams directly to QuestPDF `Image` element; reject images over 10 MP or 10 MB instead of downscaling; generate a small built-in placeholder PNG for failures.
- Rationale: Keeps dependencies minimal; QuestPDF supports required formats (JPEG/PNG/GIF/SVG) and preserves aspect ratio; rejecting oversize files avoids extra tooling and complexity.
- Alternatives considered: Add SkiaSharp for pre-downscale/format normalization (not needed given rejection policy); always downscale (more work with little benefit under current caps).
