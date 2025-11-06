# Research: CSS Padding Support

**Date**: 2025-11-06  
**Feature**: CSS Padding Support  
**Status**: Complete

## Research Objectives

1. Confirm CSS padding shorthand parsing rules (1, 2, 3, 4 value forms)
2. Verify padding inheritance behavior (non-inheritable property)
3. Understand padding interaction with borders and content area
4. Confirm px→pt conversion ratio matches margin implementation
5. Validate inline element padding behavior differences from block elements

## Findings

### CSS Padding Shorthand Parsing

**Decision**: Support all four shorthand forms following CSS Box Model specification:
- 1 value: `padding: 10px` → all sides = 10px
- 2 values: `padding: 10px 20px` → top/bottom = 10px, left/right = 20px
- 3 values: `padding: 10px 20px 15px` → top = 10px, left/right = 20px, bottom = 15px
- 4 values: `padding: 10px 20px 15px 5px` → top = 10px, right = 20px, bottom = 15px, left = 5px

**Rationale**: Standard CSS specification behavior. Matches margin shorthand parsing already implemented.

**Alternatives considered**: None - CSS spec is definitive.

### Padding Inheritance

**Decision**: Padding does NOT inherit from parent elements. Default value is 0 for all sides.

**Rationale**: CSS specification explicitly states padding is not an inheritable property. Child elements with no padding specified will have 0 padding, regardless of parent padding values.

**Alternatives considered**: None - CSS spec is definitive.

### Padding and Box Model

**Decision**: Padding is applied inside the border (if present) and reduces the content area. Element's total dimensions (width/height) relative to container remain unchanged (content-box model).

**Rationale**: Standard CSS box model behavior. Padding affects internal spacing, not external dimensions. Matches margin implementation pattern.

**Alternatives considered**: None - content-box model is standard and matches existing margin behavior.

### Unit Conversion

**Decision**: Use existing `CssValueConverter.TryGetLengthPt()` method which converts px to pt using ratio 72/96 = 0.75 (1px = 0.75pt).

**Rationale**: Consistent with margin implementation. Ensures deterministic conversion matching existing codebase.

**Alternatives considered**: None - reusing existing conversion logic maintains consistency.

### Inline Element Padding

**Decision**: Padding applies to inline elements, affecting horizontal spacing and potentially line-breaking behavior. Vertical padding on inline elements does not affect line height but may cause overlap.

**Rationale**: CSS specification allows padding on inline elements. For this iteration, we support padding on inline elements with the understanding that vertical padding behavior may differ from block elements. Design remains extensible.

**Alternatives considered**: 
- Defer inline padding support: Rejected - spec requires support for currently implemented element types (block + inline)
- Full CSS inline padding semantics: Deferred - complex line-height interactions can be refined in future iterations

### Shorthand Precedence

**Decision**: Individual padding properties (`padding-top`, etc.) take precedence over shorthand `padding` when both are specified on the same element.

**Rationale**: Standard CSS cascade behavior - more specific properties override shorthand. Matches margin implementation pattern.

**Alternatives considered**: None - CSS spec is definitive.

## Implementation Pattern

**Decision**: Follow the exact pattern used for margin implementation:
1. Add constants to `HtmlCssConstants.CssProperties`
2. Extend `ComputedStyle` with padding properties (default 0)
3. Parse in `CssStyleComputer.MapStyle()` (individual properties + shorthand expansion)
4. Propagate to box model via `BoxTreeBuilder`
5. Apply in layout engine (`BlockLayoutEngine`) to reduce content area

**Rationale**: Margin implementation is proven, tested, and follows constitution principles. Reusing the pattern ensures consistency and reduces risk.

**Alternatives considered**:
- New abstraction layer: Rejected - adds unnecessary complexity for a property that behaves identically to margin
- Different storage structure: Rejected - separate properties per side matches margin pattern and is clear

## Unresolved / Future Work

- Table cell padding: Design must remain extensible, but implementation deferred per spec
- Percentage and relative units (em, rem, %): Explicitly out of scope per spec
- Other absolute units (pt, in, cm, mm): May be added in future iterations

## References

- CSS Box Model Specification: https://www.w3.org/TR/CSS2/box.html
- Existing margin implementation: `src/Html2x.Layout/Style/CssStyleComputer.cs`
- CSS extension guide: `docs/extending-css.md`

