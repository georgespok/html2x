# Dimension Resolution Schema

## Purpose
Provides a shared contract for tooling and tests to describe how block-level containers request and verify CSS `width`/`height` handling without implying a runtime web service.

## Request Payload: `BlockDimensionQuery`
- `elementId` (string, required): Deterministic identifier from DOM/style tree.
- `requestedWidth` (float, nullable): Raw numeric value.
- `requestedHeight` (float, nullable).
- `unit` (enum, required): `px`, `pt`, or `percent`.
- `containerWidthPt` (float, optional): Parent width used when resolving percentages.
- `containerHeightPt` (float, optional).
- `borderThicknessPt` (float, optional): Needed for bordered placeholder validation.

### Validation Rules
1. Values must be ≥ 0; invalid entries fall back to auto sizing with warnings.
2. Percentages require a container dimension; if missing after one retry, record `fallbackReason`.

## Response Payload: `BlockDimensionResult`
- `elementId` (string, required).
- `resolvedWidthPt` (float, required).
- `resolvedHeightPt` (float, required).
- `passCount` (int, 1-2): Indicates whether a re-measurement was required.
- `fallbackReason` (string, nullable): Populated when unsupported units or conflicts occur.
- `warnings` (array<string>): Surface constraint conflicts or clipped borders.

## Diagnostics Event: `BlockDimensionDiagnostics`
- `elementId` (string, required).
- `requestedWidth`, `requestedHeight` (float, nullable).
- `unit` (enum: `px`, `pt`, `percent`).
- `resolvedWidthPt`, `resolvedHeightPt` (float, required).
- `overflowBehavior` (enum: `Clip`, `Paginate`).
- `borderThicknessPt` (float, optional).
- `fallbackReason` (string, nullable).

## Usage Notes
- These payloads live only inside tests, recorders, or console tooling; they document what data must flow through Html2x.Abstractions contracts.
- Emit diagnostics for every bordered block processed so deterministic comparisons can assert dimension lineage per Principle IV.
- When future units (e.g., `em`, `cm`) are supported, extend the `unit` enum and add migration guidance in `docs/`.
