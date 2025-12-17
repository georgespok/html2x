# Phase 0 Research: Font Accurate Text Measurement

## Decision 1: Measurer placement

- Decision: Implement the concrete measurer in the composition layer and inject it into layout via abstractions.
- Rationale: Keeps Html2x.LayoutEngine isolated and ensures the measurer can be replaced without touching layout.
- Alternatives considered: Put the measurer inside the renderer or layout engine (rejected due to dependency direction and testability).

## Decision 2: Diagnostic payloads

- Decision: Emit diagnostics for font resolution failures, invalid font files, and measurement inputs used for layout decisions.
- Rationale: Supports auditability and quick triage without inspecting rendered PDFs.
- Alternatives considered: Minimal or no diagnostics (rejected because it hides layout failures).

## Decision 3: Wrapping behavior

- Decision: Space-first wrapping with character-level fallback when a token cannot fit an empty line.
- Rationale: Matches expected text layout behavior and keeps long words from breaking layout.
- Alternatives considered: Always character-level wrapping (rejected due to poor readability), hyphenation (deferred).
