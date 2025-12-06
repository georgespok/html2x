# Research: SkiaSharp Renderer Migration

## Dependency selection
- Decision: Target SkiaSharp 3.119.1 (latest stable as of 2025-12-06); avoid preview 3.119.2.
- Rationale: 3.119.1 is the newest non-preview release (23 Sep 2025) with high download volume; previews risk API churn near release.
- Alternatives considered: 3.119.2-preview.1 (newer but prerelease); 2.88.x LTS (older, lacks current fixes).

## PDF backend
- Decision: Use Skia PDF backend via `SKDocument`/`SKCanvas` for writing pages; one document per render, one canvas per page.
- Rationale: Skia PDF backend is the supported path for PDF output; supports annotations and direct drawing commands without layout logic.
- Alternatives considered: Raster-to-PDF by snapshotting `SKSurface` then embedding; rejected due to extra memory and potential nondeterminism.

## Rendering model
- Decision: Map fragments directly to `SKCanvas` commands (text, images, paths, fills) using absolute coordinates; avoid layout corrections or transforms beyond page origin translation.
- Rationale: Aligns with requirement that renderer is a pure drawer and deterministic; keeps LayoutEngine as sole geometry authority.
- Alternatives considered: Recompute positioning in renderer or add fit/overflow logic; rejected as it breaks deterministic separation.

## Resource management
- Decision: Dispose `SKPaint`, `SKBitmap`, `SKImage`, `SKSurface`, `SKDocument` with `using`/`Dispose` to avoid native leaks; avoid retaining large bitmaps after draw.
- Rationale: SkiaSharp wraps native resources that must be freed promptly for server use; documented in SkiaSharp guidance.
- Alternatives considered: Rely on GC finalizers; rejected due to nondeterministic cleanup and higher memory footprint.

## Determinism practices
- Decision: Set explicit color profiles, text antialiasing, and subpixel options; avoid time/locale sources; use fixed font resolution and DPI; log fragment geometry instead of PDF bytes for comparisons.
- Rationale: Controls sources of nondeterminism and matches diagnostics-first approach; byte-for-byte PDF parity not required.
- Alternatives considered: PDF byte diffing; rejected because PDF metadata ordering can vary.

## Diagnostics
- Decision: Forward layout diagnostics (missing/oversize images, font fallbacks) into renderer logs and, when possible, PDF annotations via `DrawAnnotation` for traceability.
- Rationale: Keeps observability through pipeline without renderer validation.
- Alternatives considered: Renderer-side validation; rejected as it would reintroduce layout responsibilities.

## Testing posture
- Decision: Temporarily skip failing QuestPdf-derived renderer tests using category/trait; add new Skia renderer tests asserting fragment-to-canvas mapping and deterministic geometry logs.
- Rationale: Supports incremental migration while keeping visibility of pending coverage.
- Alternatives considered: Disabling entire suite; rejected due to loss of migration signal.
