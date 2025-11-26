---
description: "Independent Borders Implementation Tasks"
---

# Tasks: Independent Borders

**Input**: Design documents from `/specs/005-independent-borders/`
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/renderer-contract.md, research.md

**Tests**: Tests are included for backward compatibility verification and new visual sample creation as requested.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Phase 0: Layout Engine Updates (Parsing)

**Purpose**: Implement parsing of independent border properties into `ComputedStyle.Borders`.

- [x] T001 Refactor `ApplyBorders` in `src/Html2x.LayoutEngine/Style/CssStyleComputer.cs` to handle individual border properties.
  ```csharp
  private void ApplyBorders(ICssStyleDeclaration css, ComputedStyle style)
  {
      // Initialize borders to None if not present, then fill individually
      style.Borders ??= new BorderEdges();
      
      // Parse individual sides (Top, Right, Bottom, Left)
      // Each side needs: Width, Style, Color
      ApplyBorderSide(css, style, "top", s => style.Borders.Top = s);
      ApplyBorderSide(css, style, "right", s => style.Borders.Right = s);
      // ...
  }
  ```
- [x] T002 Implement parsing logic for `border-top-width`, `border-right-width`, `border-bottom-width`, `border-left-width` in `src/Html2x.LayoutEngine/Style/CssStyleComputer.cs`.
  ```csharp
  // Use GetLengthPt helper or _converter
  var width = _converter.TryGetLengthPt(css.GetPropertyValue($"border-{side}-width"), out var w) ? w : 0f;
  ```
- [x] T003 Implement parsing logic for `border-top-style`, `border-right-style`, `border-bottom-style`, `border-left-style` in `src/Html2x.LayoutEngine/Style/CssStyleComputer.cs` (using `ParseBorderStyle`).
  ```csharp
  var style = ParseBorderStyle(css.GetPropertyValue($"border-{side}-style"));
  ```
- [x] T004 Implement parsing logic for `border-top-color`, `border-right-color`, `border-bottom-color`, `border-left-color` in `src/Html2x.LayoutEngine/Style/CssStyleComputer.cs` (using `ColorRgba.FromCss`). Handle default to element's color if not specified.
  ```csharp
  var colorStr = css.GetPropertyValue($"border-{side}-color");
  var color = !string.IsNullOrWhiteSpace(colorStr) 
      ? ColorRgba.FromCss(colorStr, style.Color) 
      : style.Color; // Default to current text color (currentColor)
  ```
- [x] T005 Implement logic for `border-color` shorthand (1-4 values) in `src/Html2x.LayoutEngine/Style/CssStyleComputer.cs`.
  ```csharp
  // Expand shorthand manually if needed, OR rely on AngleSharp's computed style 
  // which might already expand shorthand to longhand properties?
  // Check if AngleSharp's ComputeCurrentStyle already expands 'border-color' to 'border-top-color' etc.
  // If yes, T002-T004 logic is sufficient. If no, manual expansion needed.
  // Assumption: AngleSharp usually expands shorthands in computed styles.
  ```
- [x] T006 Implement logic for `border-style` shorthand (1-4 values) in `src/Html2x.LayoutEngine/Style/CssStyleComputer.cs`.
- [x] T007 Implement logic for `border-width` shorthand (1-4 values) in `src/Html2x.LayoutEngine/Style/CssStyleComputer.cs`.
- [x] T008 Update `ApplyBorders` to construct `BorderEdges` from the parsed individual sides in `src/Html2x.LayoutEngine/Style/CssStyleComputer.cs`.
  ```csharp
  style.Borders = new BorderEdges {
      Top = new BorderSide(topWidth, topColor, topStyle),
      Right = new BorderSide(rightWidth, rightColor, rightStyle),
      // ...
  };
  ```
- [x] T009 Run `Build_WithIndependentBorderSides_ProducesExpectedFragmentsAsync` test in `src/Tests/Html2x.LayoutEngine.Test/LayoutIntegrationTests.cs` and ensure it passes.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and dependency management

- [x] T010 Add SkiaSharp dependency to Html2x.Renderers.Pdf.csproj
  ```xml
  <ItemGroup>
    <PackageReference Include="SkiaSharp" Version="3.119.1" /> <!-- Or appropriate version -->
  </ItemGroup>
  ```
- [x] T011 Create drawing namespace folder src/Html2x.Renderers.Pdf/Drawing/
- [x] T012 Add SkiaSharp dependency to src/Tests/Html2x.Renderers.Pdf.Test/Html2x.Renderers.Pdf.Test.csproj
  ```xml
  <ItemGroup>
    <PackageReference Include="SkiaSharp" Version="3.119.1" />
    <!-- Linux Assets: Use NoDependencies variant for Azure Functions/Alpine compatibility -->
    <PackageReference Include="SkiaSharp.NativeAssets.Linux.NoDependencies" Version="3.119.1" />
    <!-- Windows Assets: Required for local dev and Windows environments -->
    <PackageReference Include="SkiaSharp.NativeAssets.Win32" Version="3.119.1" />
  </ItemGroup>
  ```
- [x] T013 Add project reference from src/Tests/Html2x.Renderers.Pdf.Test/Html2x.Renderers.Pdf.Test.csproj to src/Html2x.Renderers.Pdf/Html2x.Renderers.Pdf.csproj
  ```xml
  <ItemGroup>
    <ProjectReference Include="..\..\Html2x.Renderers.Pdf\Html2x.Renderers.Pdf.csproj" />
  </ItemGroup>
  ```
---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core drawing service that MUST be complete before integration

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T014 [P] Implement BorderShapeDrawer class in src/Html2x.Renderers.Pdf/Drawing/BorderShapeDrawer.cs
  ```csharp
  public class BorderShapeDrawer
  {
      // Helper for testing geometry
      public (SKRect Top, SKRect Right, SKRect Bottom, SKRect Left) CalculateRects(SKSize size, BorderEdges borders) { /* ... */ }
      
      public void Draw(SKCanvas canvas, SKSize size, BorderEdges borders) { /* ... */ }
  }
  ```
- [x] T015 Implement Draw method in BorderShapeDrawer with rectangular overlap logic (Top, Right, Bottom, Left)
  ```csharp
  // Rectangular Overlap Geometry
  var w = size.Width;
  var h = size.Height;
  var t = (float)borders.Top.Width;
  var r = (float)borders.Right.Width;
  var b = (float)borders.Bottom.Width;
  var l = (float)borders.Left.Width;
  
  // Draw order: Top -> Right -> Bottom -> Left
  // Example: Top border
  if (t > 0)
  {
      using var paint = CreatePaint(borders.Top);
      canvas.DrawRect(0, 0, w, t, paint);
  }
  // Example: Right border (full height)
  if (r > 0)
  {
      using var paint = CreatePaint(borders.Right);
      canvas.DrawRect(w - r, 0, r, h, paint);
  }
  // ... and so on for Bottom and Left
  ```
- [x] T016 Add logic to map BorderLineStyle to SKPaint (Color, Style, PathEffect) in BorderShapeDrawer
  ```csharp
  private SKPaint CreatePaint(BorderSide side)
  {
      var paint = new SKPaint { Color = side.Color.ToSkColor(), Style = SKPaintStyle.Fill }; // Borders are filled rectangles
      if (side.Style == BorderLineStyle.Dashed) {
          paint.PathEffect = SKPathEffect.CreateDash(new float[] { side.Width * 3, side.Width }, 0);
      }
      else if (side.Style == BorderLineStyle.Dotted) {
          paint.PathEffect = SKPathEffect.CreateDash(new float[] { side.Width, side.Width }, 0);
      }
      // For solid, no PathEffect needed.
      return paint;
  }
  ```
- [x] T017 [P] Modify QuestPdfFragmentRenderer to expose SKCanvas access (using .Canvas() or .Element())
  ```csharp
  // In QuestPdfFragmentRenderer.cs, within ApplyBlockDecorations (or similar method)
  container
      .Element(element => {
          element.Canvas((canvas, size) => {
             // Will call drawer here
          });
      });
  ```
- [x] T018 [P] Create BorderShapeDrawerTests.cs in src/Tests/Html2x.Renderers.Pdf.Test/
- [x] T019 [P] Implement CalculateRects_CalculatesCorrectOverlap test in src/Tests/Html2x.Renderers.Pdf.Test/BorderShapeDrawerTests.cs
  ```csharp
  // See previous sketch for content
  ```
- [x] T020 [P] Implement Draw_RendersTopBorderColor test in src/Tests/Html2x.Renderers.Pdf.Test/BorderShapeDrawerTests.cs
  ```csharp
  // See previous sketch for content
  ```
**Checkpoint**: BorderShapeDrawer is ready to accept a canvas and borders.

---

## Phase 3: User Story 1 - Distinct Styles Per Side (Priority: P1) 🎯 MVP

**Goal**: Enable rendering of distinct styles (width, color, style) for each of the 4 sides of a container.

**Independent Test**: Render independent-borders.html and verify 4 distinct sides with rectangular corner overlaps.

### Implementation for User Story 1

- [x] T021 [US1] Update QuestPdfFragmentRenderer.ApplyBlockDecorations to remove GetUniformBorder check
  ```csharp
  // Remove this check:
  // var uniformBorder = BorderPainter.GetUniformBorder(style.Borders);
  // if (uniformBorder != null) { ... }
  ```
- [x] T022 [US1] Integrate BorderShapeDrawer call into QuestPdfFragmentRenderer for non-uniform borders
  ```csharp
  // Replace with:
  container.Element(e => {
       e.Canvas((canvas, size) => {
           // Assuming _borderShapeDrawer is injected or instantiated
           _borderShapeDrawer.Draw(canvas, size, style.Borders);
       });
  });
  ```
- [x] T023 [US1] Implement logic to skip drawing sides with Width=0 or Style=None in BorderShapeDrawer
  ```csharp
  // Inside BorderShapeDrawer.Draw method:
  if (borders.Top.Width > 0 && borders.Top.Style != BorderLineStyle.None) {
      // Draw top border
  }
  // ... similar checks for other sides
  ```
### Verification

- [x] T024 [US1] Create src/Tests/Html2x.TestConsole/html/independent-borders.html sample file (as per Quickstart)
- [X] T025 [US1] Run TestConsole to generate PDF and visually verify distinct sides and rectangular corner overlaps.

**Checkpoint**: User Story 1 should be fully functional. Distinct borders appear correctly.

---

## Phase 4: User Story 2 - Single Side Borders (Priority: P2)

**Goal**: Enable rendering of borders on only specific sides (e.g. underline).

**Independent Test**: Render a sample with only border-bottom and verify only that line appears.

### Implementation for User Story 2

- [x] T026 [US2] Verify BorderShapeDrawer correctly handles 3 zero-width sides (logic implemented in T023 covers this, verify via test)
- [x] T027 [US2] Ensure LayoutEngine correctly calculates ContentBox with partial borders (FR-004 content-box logic)

### Verification

- [x] T028 [US2] Create src/Tests/Html2x.TestConsole/html/single-side-border.html sample
- [ ] T029 [US2] Run TestConsole to generate PDF and verify only single side is drawn

**Checkpoint**: Single side borders work as expected.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Improvements and backward compatibility

- [x] T030 [P] Verify backward compatibility by running basic.html through TestConsole (SC-002)
- [x] T031 Clean up any unused code (e.g. potentially obsolete BorderPainter methods if fully replaced)
- [x] T032 Documentation updates in docs/ if extension points changed

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (Layout Engine)**: No dependencies.
- **Phase 1 (Setup)**: Depends on Phase 0.
- **Phase 2 (Foundational)**: Depends on Phase 1. BLOCKS all user stories.
- **Phase 3 (User Story 1)**: Depends on Phase 2.
- **Phase 4 (User Story 2)**: Depends on Phase 2 (and largely US1 integration).
- **Phase 5 (Polish)**: Depends on US1 & US2.

### User Story Dependencies

- **User Story 1 (P1)**: Core integration of the new Drawer.
- **User Story 2 (P2)**: Relies on the same Drawer logic but validates specific partial-border scenarios.

### Parallel Opportunities

- T014 and T017 can run in parallel.
- T018, T019, T020 (test implementation) can run in parallel with T014-T017.
- T024 and T028 (Sample creation) can run in parallel with implementation.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 0, 1 & 2 (Layout Parsing, SkiaSharp setup & Drawer logic).
2. Integrate Drawer into Renderer (Phase 3).
3. Verify with independent-borders.html.
4. **STOP and VALIDATE**: Does it render 4 different colored sides?

### Incremental Delivery

1. Phase 0, 1 & 2 ready -> Drawer unit testable.
2. US1 -> Full independent borders.
3. US2 -> Verify partial borders (underlines).
4. Polish -> Ensure no regressions.
