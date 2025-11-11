# Data Model: CSS Height and Width Support

## Entity: RequestedDimension
- **Purpose**: Captures the raw CSS inputs emitted by style resolution before any normalization.
- **Fields**:
  - `ElementId` (string, required) – deterministic identifier from DOM/style tree.
  - `WidthValue` (float, nullable) – raw numeric value from CSS.
  - `HeightValue` (float, nullable).
  - `Unit` (enum: Px, Pt, Percent) – reflects original CSS unit; absence implies auto.
  - `SourceDeclaration` (string) – CSS selector/declaration reference for diagnostics.
- **Relationships**: One-to-one with `ResolvedDimension`.
- **Validation Rules**:
  - Values must be ≥ 0; invalid inputs force `Unit = Auto` and trigger warnings.
  - Percentages require parent dimension context; missing context sets a retry flag.

## Entity: ResolvedDimension
- **Purpose**: Stores normalized width/height values (points) and fallback reasoning for downstream consumers.
- **Fields**:
  - `ElementId` (string, required) – matches RequestedDimension.
  - `WidthPt` (float, required).
  - `HeightPt` (float, required).
  - `IsPercentageWidth` (bool).
  - `IsPercentageHeight` (bool).
  - `PassCount` (int, 1-2) – indicates whether a re-measurement occurred.
  - `FallbackReason` (string, nullable) – describes unsupported units, conflicts, or missing parents.
- **Relationships**: Feeds directly into `FragmentDimension`.
- **Validation Rules**:
  - `WidthPt`/`HeightPt` must stay within ±1pt of the measured layout pass.
  - `PassCount > 2` is invalid and must fail the test harness.

## Entity: FragmentDimension
- **Purpose**: Carries render-ready rectangle metrics plus diagnostics metadata into the fragment tree and renderer.
- **Fields**:
  - `ElementId` (string, required).
  - `WidthPt` (float, required).
  - `HeightPt` (float, required).
  - `BorderThicknessPt` (float, optional) – needed for bordered placeholders.
  - `OverflowBehavior` (enum: Clip, Paginate).
  - `Diagnostics` (object) – mirrors data from Requested/Resolved dimensions for logging.
- **Relationships**: Consumes ResolvedDimension; referenced by Html2x.Renderers.Pdf when laying out pages.
- **State Transitions**:
  1. **Initialized** – receives Requested/Resolved payloads.
  2. **Measured** – accommodates content measurement passes.
  3. **Finalized** – emitted to renderer with immutable metrics and diagnostics snapshot.
- **Validation Rules**:
  - Finalized state requires `WidthPt`/`HeightPt` to equal ResolvedDimension (±1pt tolerance).
  - `BorderThicknessPt` must not exceed half the smallest dimension; otherwise clip/log.

## Supporting Concept: PercentageResolutionContext
- **Fields**:
  - `ParentWidthPt` (float?, optional).
  - `ParentHeightPt` (float?, optional).
  - `PassCount` (int) – guards against repeated passes; max 2 (initial + retry).
- **Rules**:
  - If parent dimension remains null after retry, fallback to auto and populate `FallbackReason`.
  - `PassCount` increments each re-measurement; exceeding 2 fails deterministic tests.
