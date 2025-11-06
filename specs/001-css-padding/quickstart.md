# Quick Start: Implementing CSS Padding Support

**Date**: 2025-11-06  
**Feature**: CSS Padding Support  
**Target Audience**: Developers implementing this feature

## Overview

This guide provides a step-by-step implementation path for adding CSS padding support, following the established margin pattern. The implementation follows incremental TDD: write failing test → implement minimal code → refactor → repeat.

## Implementation Order

Follow this sequence to maintain testability and incremental progress:

### Step 1: Add CSS Constants

**File**: `src/Html2x.Layout/HtmlCssConstants.cs`

Add padding property constants to `CssProperties` class:

```csharp
public static class CssProperties
{
    // ... existing properties ...
    public const string Padding = "padding";
    public const string PaddingTop = "padding-top";
    public const string PaddingRight = "padding-right";
    public const string PaddingBottom = "padding-bottom";
    public const string PaddingLeft = "padding-left";
}
```

**Test**: No test needed (constants are compile-time).

### Step 2: Extend ComputedStyle

**File**: `src/Html2x.Layout/Style/StyleModels.cs`

Add padding properties to `ComputedStyle` class:

```csharp
public sealed class ComputedStyle
{
    // ... existing properties ...
    public float PaddingTopPt { get; set; }
    public float PaddingRightPt { get; set; }
    public float PaddingBottomPt { get; set; }
    public float PaddingLeftPt { get; set; }
}
```

**Test**: Write failing test in `Html2x.Layout.Test/CssStyleComputerTests.cs`:
- Test individual padding properties parse correctly
- Test default value is 0
- Test px→pt conversion

### Step 3: Parse Individual Padding Properties

**File**: `src/Html2x.Layout/Style/CssStyleComputer.cs`

Extend `MapStyle()` method to parse individual padding properties:

```csharp
private ComputedStyle MapStyle(IElement element, ComputedStyle? parentStyle)
{
    var css = element.ComputeCurrentStyle();
    
    var style = new ComputedStyle
    {
        // ... existing properties ...
        PaddingTopPt = _converter.GetLengthPt(css, HtmlCssConstants.CssProperties.PaddingTop, 0),
        PaddingRightPt = _converter.GetLengthPt(css, HtmlCssConstants.CssProperties.PaddingRight, 0),
        PaddingBottomPt = _converter.GetLengthPt(css, HtmlCssConstants.CssProperties.PaddingBottom, 0),
        PaddingLeftPt = _converter.GetLengthPt(css, HtmlCssConstants.CssProperties.PaddingLeft, 0)
    };
    
    // ... rest of method ...
}
```

**Test**: Update existing test from Step 2 to verify parsing works.

### Step 4: Parse Padding Shorthand

**File**: `src/Html2x.Layout/Style/CssStyleComputer.cs`

Add method to parse and expand shorthand (similar to `ApplyPageMargins` pattern):

```csharp
private void ApplyPaddingShorthand(ICssStyleDeclaration css, ComputedStyle style)
{
    var shorthandDefined = _converter.TryGetLengthPt(
        css.GetPropertyValue(HtmlCssConstants.CssProperties.Padding), 
        out var shorthand);
    
    if (!shorthandDefined) return;
    
    // Parse shorthand: 1, 2, 3, or 4 values
    var paddingValue = css.GetPropertyValue(HtmlCssConstants.CssProperties.Padding);
    var values = ParseShorthandValues(paddingValue);
    
    // Apply based on value count (1=all, 2=vertical/horizontal, 3=top/horizontal/bottom, 4=all)
    // Individual properties take precedence (already set in MapStyle)
    if (style.PaddingTopPt == 0 && values.Count > 0)
        style.PaddingTopPt = values[0];
    // ... similar for other sides based on value count
}
```

**Test**: Add failing test for all four shorthand forms:
- `padding: 10px` → all sides = 10px
- `padding: 10px 20px` → top/bottom=10px, left/right=20px
- `padding: 10px 20px 15px` → top=10px, left/right=20px, bottom=15px
- `padding: 10px 20px 15px 5px` → all four sides
- Precedence: `padding: 10px; padding-top: 25px` → top=25px, others=10px

### Step 5: Extend Box Model

**File**: `src/Html2x.Layout/Box/BoxModels.cs`

Add `Padding` property to `BlockBox`:

```csharp
public sealed class BlockBox : DisplayNode
{
    // ... existing properties ...
    public Spacing Padding { get; set; } = new();
}
```

**Test**: Add failing test in `BoxTreeBuilderTests`:
- Verify padding values copied from `ComputedStyle` to `BlockBox`
- Verify default padding is 0

### Step 6: Propagate Padding to Boxes

**File**: `src/Html2x.Layout/Box/BoxTreeBuilder.cs`

Extend box tree building to copy padding from `ComputedStyle`:

```csharp
// In method that creates BlockBox from ComputedStyle
block.Padding = new Spacing
{
    Top = Safe(styles.PaddingTopPt),
    Right = Safe(styles.PaddingRightPt),
    Bottom = Safe(styles.PaddingBottomPt),
    Left = Safe(styles.PaddingLeftPt)
};
```

**Test**: Update test from Step 5 to verify propagation.

### Step 7: Apply Padding in Layout Engine

**File**: `src/Html2x.Layout/Box/BlockLayoutEngine.cs`

Modify layout calculations to account for padding:

```csharp
// In method that calculates content area
var contentWidth = Math.Max(0, 
    totalWidth - block.Padding.Left - block.Padding.Right - block.Margin.Left - block.Margin.Right);

var contentX = parentX + block.Margin.Left + block.Padding.Left;
var contentY = parentY + block.Margin.Top + block.Padding.Top;
```

**Test**: Add failing integration test:
- Block with `width: 200px; padding: 20px`
- Verify child content width = 160px (200 - 40)
- Verify child X position accounts for left padding

### Step 8: Add Logging

**File**: `src/Html2x.Layout/Style/CssStyleComputer.cs`

Add structured logging for invalid/unsupported values:

```csharp
if (unsupportedUnit)
{
    _logger.LogWarning("Unsupported padding unit '{Unit}' on element {TagName}. Treating as 0.", 
        unit, element.TagName);
}
```

**Test**: Verify warnings logged for unsupported units, invalid values.

### Step 9: Update Documentation

**File**: `docs/extending-css.md`

Add padding as example of CSS property extension, referencing margin pattern.

## Testing Strategy

### Unit Tests (`Html2x.Layout.Test`)

1. **CssStyleComputerTests**:
   - Individual property parsing (px values)
   - Shorthand parsing (all 4 forms)
   - Precedence (individual over shorthand)
   - Invalid values (negative, non-numeric, unsupported units)
   - Default values (0 when not specified)
   - Inheritance (does NOT inherit)

2. **BoxTreeBuilderTests**:
   - Padding propagation from `ComputedStyle` to `BlockBox`
   - Default padding values

3. **LayoutIntegrationTests**:
   - Padding affects content area dimensions
   - Padding affects child positioning
   - Asymmetric padding (different values per side)

### Integration Tests (`Html2x.Test`)

- End-to-end: HTML with padding → PDF output
- Verify fragment geometry matches expected padding

## Common Pitfalls

1. **Shorthand parsing**: Remember individual properties take precedence. Parse shorthand first, then individual properties override.
2. **Content area calculation**: Padding reduces content area, margin affects element positioning relative to container.
3. **Default values**: Padding defaults to 0 (does not inherit), unlike some CSS properties.
4. **Unit conversion**: Use existing `CssValueConverter` - don't create new conversion logic.

## Verification Checklist

- [ ] All tests pass: `dotnet test Html2x.sln -c Release`
- [ ] Padding parsing works for individual properties
- [ ] Padding shorthand parsing works for all 4 forms
- [ ] Padding affects content area in layout
- [ ] Invalid values log warnings and default to 0
- [ ] Documentation updated
- [ ] Manual smoke test in `Html2x.Pdf.TestConsole`

## Next Steps After Implementation

1. Review code against margin implementation for consistency
2. Verify extensibility for future table cell support
3. Update release notes
4. Consider adding padding examples to test console HTML samples

