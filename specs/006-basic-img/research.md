# Research

## Decisions

### Relative image base
- Decision: Resolve relative src against the input HTML file directory; allow absolute paths.
- Rationale: Matches browser semantics and avoids working-directory surprises.
- Alternatives considered: Use process working directory; require absolute paths only.

### Oversized image handling
- Decision: Flag oversize (>10 MB) in the image provider during layout and render a placeholder; no downscaling.
- Rationale: Keeps layout IO centralized and deterministic; avoids renderer-specific byte checks.
- Alternatives considered: Downscale in renderer; defer decision to renderer IO.

### Missing image behavior
- Decision: Image provider marks missing during layout (bad path/data URI), renderer draws placeholder at expected size and logs a warning.
- Rationale: Stabilizes layout early and keeps renderer IO-free.
- Alternatives considered: Detect missing only at render time; silent empty box.

### Performance targets
- Decision: No explicit performance targets; rely on size caps and downscaling.
- Rationale: Caps are sufficient for this scope; avoids premature guarantees.
- Alternatives considered: Per-image or per-document timing targets.

### Rendering pipeline (QuestPDF only)
- Decision: Renderer consumes layout-marked image status and draws image or placeholder; no renderer IO or size checks.
- Rationale: IO centralized in provider; renderer stays QuestPDF-only paint logic.
- Alternatives considered: Renderer loads bytes directly; mixed concerns.
