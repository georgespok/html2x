# Data Model: CSS Padding Support

**Date**: 2025-11-06  
**Feature**: CSS Padding Support

## Overview

Padding support extends existing style computation and box model entities. No new entities are introduced; existing models are extended with padding properties following the margin pattern.

## Entities

### ComputedStyle

**Location**: `src/Html2x.Layout/Style/StyleModels.cs`

**Current State**: Contains margin properties (`MarginTopPt`, `MarginRightPt`, `MarginBottomPt`, `MarginLeftPt`)

**Extension**:
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

**Validation Rules**:
- Default value: 0 for all sides (padding does not inherit)
- Value range: Non-negative floats (negative values treated as 0 with warning)
- Unit: Points (converted from px during style computation)

**Relationships**:
- One `ComputedStyle` per DOM element (via `StyleNode.Style`)
- Used by `BoxTreeBuilder` to create box models
- Referenced by `BlockLayoutEngine` for layout calculations

### BlockBox

**Location**: `src/Html2x.Layout/Box/BoxModels.cs`

**Current State**: Contains `Margin` property of type `Spacing`

**Extension Consideration**: 
- Option A: Add `Padding` property of type `Spacing` (reuse existing class)
- Option B: Store padding separately if layout logic differs significantly

**Decision**: Reuse `Spacing` class for padding (Option A) to maintain consistency with margin pattern. If future requirements reveal padding-specific layout needs, can refactor later.

**Extension**:
```csharp
public sealed class BlockBox : DisplayNode
{
    // ... existing properties ...
    public Spacing Margin { get; set; } = new();
    public Spacing Padding { get; set; } = new(); // NEW
}
```

**Validation Rules**:
- Padding values copied from `ComputedStyle` during box tree construction
- Applied to content area calculations (reduces available width/height)

**Relationships**:
- Created by `BoxTreeBuilder` from `ComputedStyle`
- Used by `BlockLayoutEngine` for positioning child content

### InlineBox

**Location**: `src/Html2x.Layout/Box/BoxModels.cs`

**Current State**: Basic inline element representation

**Extension Consideration**: Padding may need different handling for inline elements (affects horizontal spacing, vertical padding behavior differs).

**Decision**: Extend `InlineBox` with padding support, but implementation may differ from block elements in layout engine. Design must accommodate inline-specific behavior.

**Extension** (if needed):
```csharp
public sealed class InlineBox : DisplayNode
{
    // ... existing properties ...
    public Spacing Padding { get; set; } = new(); // NEW (if box model needs it)
}
```

**Note**: Padding may be applied directly from `ComputedStyle` during inline layout without explicit box storage, depending on implementation approach. Decision deferred to implementation phase.

### Spacing

**Location**: `src/Html2x.Layout/Box/BoxModels.cs`

**Current State**: Used for margin values

**Reuse**: Existing `Spacing` class will be reused for padding values:
```csharp
public sealed class Spacing
{
    public float Top { get; set; }
    public float Right { get; set; }
    public float Bottom { get; set; }
    public float Left { get; set; }
}
```

**Rationale**: Padding has identical structure to margin (four sides), so reusing `Spacing` maintains consistency and reduces code duplication.

## Data Flow

### Style Computation Stage

1. `CssStyleComputer.MapStyle()` reads CSS properties:
   - Individual: `padding-top`, `padding-right`, `padding-bottom`, `padding-left`
   - Shorthand: `padding` (expanded to individual values)
2. Values converted from px to pt using `CssValueConverter.TryGetLengthPt()`
3. Values stored in `ComputedStyle` properties (default 0 if not specified)
4. Individual properties take precedence over shorthand if both present

### Box Model Stage

1. `BoxTreeBuilder` reads `ComputedStyle.Padding*Pt` values
2. Creates `Spacing` instance with padding values
3. Assigns to `BlockBox.Padding` (or applies to `InlineBox` as needed)

### Layout Stage

1. `BlockLayoutEngine` reads `BlockBox.Padding`
2. Reduces content area: `contentWidth = totalWidth - padding.Left - padding.Right`
3. Adjusts child positioning: `childX = parentX + padding.Left`
4. Accounts for vertical padding in cursor positioning

## State Transitions

**N/A**: Padding values are computed once during style computation and remain immutable through the pipeline. No state transitions.

## Constraints

1. **Non-inheritable**: Padding does NOT inherit from parent elements
2. **Non-negative**: Negative values treated as 0 with warning
3. **Unit restriction**: Only `px` units supported (converted to pt)
4. **Default value**: 0 for all sides when not specified

## Future Extensibility

- **Table cells**: Design must allow adding padding to `TableCellBox` without architectural changes
- **Additional units**: Data model supports any float value, but parsing logic restricts to px for now
- **Box-sizing**: Current model assumes content-box; border-box support would require additional properties

