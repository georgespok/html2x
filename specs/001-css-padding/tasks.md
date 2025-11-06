# Tasks: CSS Padding Support

**Input**: Design documents from `/specs/001-css-padding/`  
**Prerequisites**: plan.md (required), spec.md (user stories), research.md, data-model.md, quickstart.md

**Tests**: Failing automated tests MUST be authored first and listed explicitly for each user story. Remove a task only after the test suite passes.

**Organization**: Tasks are grouped by user story to keep delivery incremental and independently testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- Source: `src/Html2x.*`
- Tests: `tests/Html2x.*` (`html2x.IntegrationTest` for end-to-end)
- Console harness: `src/Html2x.Pdf.TestConsole/`
- Samples and fonts: `src/Html2x.Pdf.TestConsole/html/`, `src/Html2x.Pdf.TestConsole/fonts/`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm baseline project health before feature work begins.

- [x] T001 Run `dotnet restore Html2x.sln` and verify all dependencies resolve correctly
- [x] T002 Verify analyzer and formatting configuration (`.editorconfig`) is in place
- [ ] T003 [P] Review existing margin implementation in `src/Html2x.Layout/Style/CssStyleComputer.cs` and `src/Html2x.Layout/Box/BlockLayoutEngine.cs` to understand pattern

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before any user story starts.

- [ ] T004 Add padding property constants to `src/Html2x.Layout/HtmlCssConstants.cs` in `CssProperties` class: `Padding`, `PaddingTop`, `PaddingRight`, `PaddingBottom`, `PaddingLeft`
- [ ] T005 Extend `ComputedStyle` class in `src/Html2x.Layout/Style/StyleModels.cs` with padding properties: `PaddingTopPt`, `PaddingRightPt`, `PaddingBottomPt`, `PaddingLeftPt` (default 0)

**Checkpoint**: Foundation ready - user story implementation can now begin.

---

## Phase 3: User Story 1 - Individual Padding Properties (Priority: P1) MVP

**Goal**: Enable developers to apply padding to individual sides of HTML elements using `padding-top`, `padding-right`, `padding-bottom`, and `padding-left` properties with pixel values. Padding affects content area, reducing available space for child content.

**Independent Test**: Add failing test in `Html2x.Layout.Test/CssStyleComputerTests.cs` that parses HTML with `style="padding-top: 20px; padding-right: 15px; padding-bottom: 10px; padding-left: 5px"` and asserts that `ComputedStyle` contains correct point values for each side.

### Tests for User Story 1

- [ ] T010 [P] [US1] Write failing test `ParseIndividualPaddingProperties_WithAllSides_ReturnsCorrectPointValues` in `src/Html2x.Layout.Test/CssStyleComputerTests.cs` for parsing all four individual padding properties
- [ ] T011 [P] [US1] Write failing test `ParsePaddingTop_WithPxValue_ConvertsToPoints` in `src/Html2x.Layout.Test/CssStyleComputerTests.cs` verifying px→pt conversion (20px = 15pt)
- [ ] T012 [P] [US1] Write failing test `ParsePaddingProperties_WhenNotSpecified_DefaultsToZero` in `src/Html2x.Layout.Test/CssStyleComputerTests.cs` verifying default value is 0
- [ ] T013 [P] [US1] Write failing test `ParsePaddingProperties_DoesNotInheritFromParent` in `src/Html2x.Layout.Test/CssStyleComputerTests.cs` verifying padding does not inherit (parent has padding, child has 0)

### Implementation for User Story 1

- [ ] T014 [US1] Extend `MapStyle()` method in `src/Html2x.Layout/Style/CssStyleComputer.cs` to parse individual padding properties using `_converter.GetLengthPt()` for each side
- [ ] T015 [US1] Add structured logging in `src/Html2x.Layout/Style/CssStyleComputer.cs` for invalid padding values (negative, non-numeric) with element context
- [ ] T016 [US1] Add structured logging in `src/Html2x.Layout/Style/CssStyleComputer.cs` for unsupported units (non-px) with warning message and element context

**Checkpoint**: User Story 1 fully functional, independently testable, and observable. Individual padding properties parse correctly with px units, default to 0, and do not inherit.

---

## Phase 4: User Story 2 - Padding Shorthand (Priority: P2)

**Goal**: Enable developers to use CSS `padding` shorthand property with 1, 2, 3, or 4 values following CSS specification rules. Individual properties take precedence over shorthand when both are specified.

**Independent Test**: Add failing test in `Html2x.Layout.Test/CssStyleComputerTests.cs` that exercises all shorthand forms and asserts correct individual side values, including precedence rules.

### Tests for User Story 2

- [ ] T020 [P] [US2] Write failing test `ParsePaddingShorthand_SingleValue_SetsAllSides` in `src/Html2x.Layout.Test/CssStyleComputerTests.cs` for `padding: 10px` → all sides = 10px
- [ ] T021 [P] [US2] Write failing test `ParsePaddingShorthand_TwoValues_SetsVerticalAndHorizontal` in `src/Html2x.Layout.Test/CssStyleComputerTests.cs` for `padding: 10px 20px` → top/bottom=10px, left/right=20px
- [ ] T022 [P] [US2] Write failing test `ParsePaddingShorthand_ThreeValues_SetsTopHorizontalBottom` in `src/Html2x.Layout.Test/CssStyleComputerTests.cs` for `padding: 10px 20px 15px` → top=10px, left/right=20px, bottom=15px
- [ ] T023 [P] [US2] Write failing test `ParsePaddingShorthand_FourValues_SetsAllSidesIndividually` in `src/Html2x.Layout.Test/CssStyleComputerTests.cs` for `padding: 10px 20px 15px 5px` → top=10px, right=20px, bottom=15px, left=5px
- [ ] T024 [P] [US2] Write failing test `ParsePaddingShorthand_WithIndividualProperty_IndividualTakesPrecedence` in `src/Html2x.Layout.Test/CssStyleComputerTests.cs` for `padding: 10px; padding-top: 25px` → top=25px, others=10px

### Implementation for User Story 2

- [ ] T025 [US2] Add `ApplyPaddingShorthand()` method in `src/Html2x.Layout/Style/CssStyleComputer.cs` to parse and expand shorthand values (similar to `ApplyPageMargins` pattern)
- [ ] T026 [US2] Implement shorthand value parsing logic in `src/Html2x.Layout/Style/CssStyleComputer.cs` to handle 1, 2, 3, and 4 value forms
- [ ] T027 [US2] Ensure individual properties take precedence over shorthand in `src/Html2x.Layout/Style/CssStyleComputer.cs` by parsing shorthand first, then individual properties override
- [ ] T028 [US2] Add structured logging in `src/Html2x.Layout/Style/CssStyleComputer.cs` for invalid shorthand values with element context

**Checkpoint**: User Stories 1 and 2 remain independently deliverable. Padding shorthand parsing works for all four forms with correct precedence rules.

---

## Phase 5: User Story 3 - Padding in Box Layout (Priority: P3)

**Goal**: Ensure padding affects layout of block and inline elements, reducing available content area where child elements are positioned. Padding applied inside border (if present) and outside content.

**Independent Test**: Add failing test in `src/Html2x.Layout.Test/BoxTreeBuilderTests.cs` or `src/Html2x.Layout.Test/LayoutIntegrationTests.cs` that creates block element with `style="width: 200px; padding: 20px"` and asserts child content width is 120 points (200px * 0.75 = 150pt total, minus 30pt horizontal padding) and X position accounts for left padding (15pt).

### Tests for User Story 3

- [ ] T030 [P] [US3] Write failing test `BlockBoxWithPadding_ReducesContentArea` in `src/Html2x.Layout.Test/BoxTreeBuilderTests.cs` verifying padding values copied from `ComputedStyle` to `BlockBox.Padding`
- [ ] T031 [P] [US3] Write failing test `LayoutBlockWithPadding_AdjustsContentWidth` in `src/Html2x.Layout.Test/LayoutIntegrationTests.cs` for block with `width: 200px; padding: 20px` → content width = 120 points (200px * 0.75 = 150pt total, minus 30pt horizontal padding)
- [ ] T032 [P] [US3] Write failing test `LayoutBlockWithPadding_AdjustsChildPosition` in `src/Html2x.Layout.Test/LayoutIntegrationTests.cs` verifying child X position accounts for left padding
- [ ] T033 [P] [US3] Write failing test `LayoutBlockWithAsymmetricPadding_PositionsCorrectly` in `src/Html2x.Layout.Test/LayoutIntegrationTests.cs` for `padding: 10px 20px 15px 5px` with correct offsets
- [ ] T048 [P] [US3] Write failing test `LayoutInlineWithPadding_AffectsHorizontalSpacing` in `src/Html2x.Layout.Test/LayoutIntegrationTests.cs` for inline element with `style="padding: 10px"` verifying horizontal spacing is affected and padding values are applied

### Implementation for User Story 3

**Note**: Inline element padding may be handled differently than block elements per data-model.md decision. Padding may be applied directly from `ComputedStyle` during inline layout without explicit `InlineBox.Padding` storage, depending on implementation approach. The T048 test will drive the implementation decision.

- [ ] T034 [US3] Add `Padding` property of type `Spacing` to `BlockBox` class in `src/Html2x.Layout/Box/BoxModels.cs`
- [ ] T035 [US3] Extend box tree building in `src/Html2x.Layout/Box/BoxTreeBuilder.cs` to copy padding values from `ComputedStyle` to `BlockBox.Padding` using `Spacing` instance
- [ ] T036 [US3] Modify content area calculation in `src/Html2x.Layout/Box/BlockLayoutEngine.cs` to account for padding: `contentWidth = totalWidth - padding.Left - padding.Right`
- [ ] T037 [US3] Adjust child positioning in `src/Html2x.Layout/Box/BlockLayoutEngine.cs` to account for padding: `contentX = parentX + margin.Left + padding.Left`
- [ ] T038 [US3] Update vertical cursor positioning in `src/Html2x.Layout/Box/BlockLayoutEngine.cs` to account for top padding when positioning first child
- [ ] T039 [US3] Ensure block total width remains unchanged in `src/Html2x.Layout/Box/BlockLayoutEngine.cs` while content area is reduced by padding
- [ ] T049 [US3] Implement inline element padding support in `src/Html2x.Layout/Box/BlockLayoutEngine.cs` (or inline layout engine) based on T048 test requirements, ensuring padding affects horizontal spacing without breaking existing inline layout behavior

**Checkpoint**: All user stories function independently with passing tests and observability hooks. Padding affects layout correctly, reducing content area while maintaining element dimensions.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories and final validation.

- [ ] T040 [P] Update `docs/extending-css.md` to include padding as example of CSS property extension, referencing margin pattern
- [ ] T041 [P] Add padding examples to HTML samples in `src/Html2x.Pdf.TestConsole/html/` for manual smoke testing
- [ ] T042 Verify all edge cases are handled: invalid values, unsupported units, inheritance, zero/auto values
- [ ] T050 [P] Write regression test in `src/Html2x.Layout.Test/LayoutIntegrationTests.cs` verifying inline elements without padding continue to layout correctly (ensure padding support doesn't break existing inline behavior)
- [ ] T043 Validate `dotnet test Html2x.sln -c Release` passes on Windows with all new padding-related tests
- [ ] T044 Run manual smoke test in `src/Html2x.Pdf.TestConsole/` with padding examples and verify PDF output
- [ ] T045 Verify deterministic rendering: identical HTML/CSS inputs produce identical fragment geometry
- [ ] T046 Review code against margin implementation in `src/Html2x.Layout/Style/CssStyleComputer.cs` and `src/Html2x.Layout/Box/BlockLayoutEngine.cs` for consistency
- [ ] T047 Verify design extensibility for future table cell support (no architectural blockers)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion - blocks all user stories.
- **User Stories (Phase 3+)**: Depend on Foundational completion; US2 depends on US1, US3 depends on US1 and US2.
- **Polish (Phase 6)**: Depends on chosen user stories being complete.

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational; no dependency on other stories. This is the MVP.
- **User Story 2 (P2)**: Depends on US1 completion (uses individual property parsing). Shorthand expands to individual values.
- **User Story 3 (P3)**: Depends on US1 and US2 completion (uses computed padding values from style stage).

### Within Each User Story

- Write tests first and ensure they fail before implementation (TDD approach per Constitution Principle III).
- Update shared contracts (`ComputedStyle`, `BlockBox`) before layout engine changes.
- Keep determinism checks and logging tasks visible in the plan.
- Confirm documentation and release notes before closing the story.

### Parallel Opportunities

- Setup and Foundational tasks marked [P] can run in parallel.
- Test tasks marked [P] within a user story can be developed in parallel (different test methods).
- Once US1 completes, US2 tests can be written in parallel with US1 polish tasks.
- Documentation and release note tasks can run alongside polish work.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (add constants and extend `ComputedStyle`).
3. Complete Phase 3: User Story 1 with passing tests and observability in place.
4. Stop and validate deterministic outputs before continuing to US2.

### Incremental Delivery

1. Finish Setup and Foundational.
2. Deliver User Story 1 (MVP) with smoke tests - individual padding properties working.
3. Layer User Story 2 (shorthand) with failing-tests-first workflow.
4. Layer User Story 3 (layout impact) with failing-tests-first workflow.
5. Merge only after updating docs, logging, and release notes.

### Parallel Team Strategy

1. Team completes Setup and Foundational together.
2. Assign US1 to one contributor, US2 tests can be prepared in parallel once US1 structure is clear.
3. Coordinate through `ComputedStyle` contract and deterministic baselines.
4. Re-run full test suite and console smoke test before merge.

---

## Notes

- Mark tasks complete when code, tests, and docs update together.
- Follow margin implementation pattern for consistency (see `src/Html2x.Layout/Style/CssStyleComputer.cs` and `src/Html2x.Layout/Box/BlockLayoutEngine.cs`).
- Padding does NOT inherit - always defaults to 0 when not specified.
- Only `px` units supported - other units should log warning and default to 0.
- Design must remain extensible for future table cell support without architectural changes.

