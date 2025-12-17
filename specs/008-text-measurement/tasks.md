---

description: "Task list template for feature implementation"
---

# Tasks: Font Accurate Text Measurement

**Input**: Design documents from `/specs/008-text-measurement/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/ (internal contracts)

**Tests**: Tests are REQUIRED for this feature (plan and constitution enforce test-first delivery).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

**Clarity rules**:
- Each task description (excluding code) must be 2-6 plain-English lines, understandable to an entry-level developer with the listed tech but no project background.
- Include a short code sketch for decision points, calculations, or logic-heavy steps.
- For substantial logic, include a brief **Why** (goal/risk) and **How** (steps) inside the task description before the code sketch.
- Code-change tasks MUST name the concrete class(es) being changed or added, describe the new or updated responsibility, and summarize behavioral change in that class.
- When a method name is known, include it, but class design and responsibility changes are required.
- Class names and responsibilities MUST be based on existing code. The task writer MUST inspect the current codebase and cite the file path for each class mentioned.
- Tasks that introduce or alter class design MUST align with SOLID principles and clean code practices.
- Tasks that do not involve code changes should still reference the concrete file or document being updated.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions
- Prefer "Class: <Name> in <path>" for code change tasks to make responsibilities explicit

## Path Conventions

- **Single project**: `src/`, `tests/` at repository root
- Paths shown below assume single project

## Preflight Checklist (must complete before writing tasks)

- [ ] Scan existing classes relevant to each user story and record their file paths
- [ ] Confirm any new classes align with existing naming and responsibility patterns
- [ ] Identify at least one existing class per story that will change, or justify why a new class is required

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm baseline classes and dependencies before edits

- [ ] T001 [P] Review Class: FontMetricsProvider in `src/Html2x.LayoutEngine/FontMetricsProvider.cs` and Class: DefaultTextWidthEstimator in `src/Html2x.LayoutEngine/DefaultTextWidthEstimator.cs` to document current heuristic behavior and ensure replacement tasks preserve required outputs.
  Why: avoids accidental regression when switching to real font metrics.
  How: summarize current ascent/descent and width heuristics in a short note inside `specs/008-text-measurement/research.md`.

- [ ] T002 [P] Review Class: TextRunFactory in `src/Html2x.LayoutEngine/Fragment/TextRunFactory.cs` and Class: InlineFragmentStage in `src/Html2x.LayoutEngine/Fragment/Stages/InlineFragmentStage.cs` to map where width, ascent, and descent are currently used.
  Why: ensures new measurer is wired at the correct point in the layout pipeline.
  How: capture a short note in `specs/008-text-measurement/research.md` with current data flow.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Contracts and composition wiring used by all user stories

- [ ] T003 [P] Class: ITextMeasurer in `src/Html2x.Abstractions/Layout/Text/ITextMeasurer.cs` - add a renderer-agnostic text measurement contract that returns widths and vertical metrics in points.
  Why: layout must depend only on abstractions for font measurement.
  How: define method signatures using existing `FontKey` from `src/Html2x.Abstractions/Layout/Styles/FontKey.cs`.

- [ ] T004 [P] Class: IFontSource in `src/Html2x.Abstractions/Layout/Fonts/IFontSource.cs` and Class: ResolvedFont in `src/Html2x.Abstractions/Layout/Fonts/ResolvedFont.cs` - define strict font resolution contracts that carry family, weight, style, and a stable source id.
  Why: enforce font-path-only resolution without leaking renderer types.
  How: model fields on existing `FontKey` and keep file path optional for diagnostics.

- [ ] T005 Class: LayoutServices in `src/Html2x/LayoutServices.cs` - introduce a composition DTO that carries `ITextMeasurer` and `IFontSource` for layout.
  Why: centralizes dependency injection without coupling LayoutEngine to renderers.
  How: keep it immutable and require non-null services.

- [ ] T006 Class: ILayoutBuilderFactory in `src/Html2x/ILayoutBuilderFactory.cs` and Class: LayoutBuilderFactory in `src/Html2x/LayoutBuilderFactory.cs` - add an overload or updated signature to accept `LayoutServices` and pass it to LayoutBuilder creation.
  Why: HtmlConverter must provide real measurement services to layout.
  How: keep existing `Create()` for backward compatibility if needed, but prefer the new path in HtmlConverter.

  **TODO: Remove existing `Create()` method from LayoutBuilderFactory**

- [ ] T007 Class: LayoutBuilder in `src/Html2x.LayoutEngine/LayoutBuilder.cs` and Class: FragmentBuildContext in `src/Html2x.LayoutEngine/Fragment/FragmentBuildContext.cs` - carry measurement services through fragment building.
  Why: Inline layout needs access to the measurer without new renderer dependencies.
  How: extend FragmentBuildContext with `ITextMeasurer` and `IFontSource` and update constructors to require them.

---

## Phase 3: User Story 1 - Correct measurement from provided fonts (Priority: P1)

**Goal**: Use real font measurement in layout so widths and baselines are correct.

**Independent Test**: A layout run with known fonts produces expected widths and baseline positions for a sample input.

### Tests for User Story 1

- [ ] T008 [P] [US1] Add unit tests in `src/Tests/Html2x.LayoutEngine.Test/Text/InlineFragmentStageTests.cs` that assert baseline and width changes when a deterministic measurer returns specific metrics.
  Why: verifies that layout consumes the measurer rather than heuristics.
  How: prefer a Moq `Mock<ITextMeasurer>` when practical; fall back to a fake only if mocking is impractical. Assert LineBoxFragment metrics.

### Implementation for User Story 1

- [ ] T009 [US1] Class: TextRunFactory in `src/Html2x.LayoutEngine/Fragment/TextRunFactory.cs` - replace width and metrics estimation with `ITextMeasurer` and remove reliance on `DefaultTextWidthEstimator`.
  Why: text runs must reflect real font metrics.
  How: accept `ITextMeasurer` via constructor and use it for both width and ascent/descent.

- [ ] T010 [US1] Class: InlineLayoutEngine in `src/Html2x.LayoutEngine/Box/InlineLayoutEngine.cs` - update line height computation to use `ITextMeasurer` metrics instead of `IFontMetricsProvider` heuristics.
  Why: vertical alignment must match real fonts.
  How: resolve font key and size from `ComputedStyle` then call the measurer for metrics.

- [ ] T011 [US1] Class: FragmentBuilder in `src/Html2x.LayoutEngine/Fragment/FragmentBuilder.cs` and Class: InlineFragmentStage in `src/Html2x.LayoutEngine/Fragment/Stages/InlineFragmentStage.cs` - inject a TextRunFactory that uses the new measurer.
  Why: inline fragments must be built with accurate measurements.
  How: pass services through FragmentBuildContext and construct stages with the configured factory.

- [ ] T012 [US1] Class: SkiaFontCache in `src/Html2x.Renderers.Pdf/Drawing/SkiaFontCache.cs` - reuse its font resolution logic to support file- and directory-backed resolution for the measurer.
  Why: keep font resolution consistent between layout and PDF rendering.
  How: expose a helper or new adapter class without leaking Skia types into layout.

- [ ] T013 [US1] Class: SkiaTextMeasurer in `src/Html2x/SkiaTextMeasurer.cs` - add a measurer that shapes text with HarfBuzz and returns widths and metrics in points.
  Why: ensures accurate, font-backed measurement.
  How: resolve typefaces via the font source adapter and cache results per conversion.

---

## Phase 4: User Story 2 - Reliable failure on missing or invalid fonts (Priority: P2)

**Goal**: Fail early with diagnostics when font path is missing or invalid.

**Independent Test**: Invalid or missing font path causes a diagnostic error before layout proceeds.

### Tests for User Story 2

- [ ] T014 [P] [US2] Add integration tests in `src/Tests/Html2x.Test/HtmlConverterTests.cs` that assert an exception and diagnostics event when `PdfOptions.FontPath` is missing or points to an invalid location.
  Why: validates early failure contract.
  How: call HtmlConverter with invalid options and assert diagnostics include an error event.

### Implementation for User Story 2

- [ ] T015 [US2] Class: HtmlConverter in `src/Html2x/HtmlConverter.cs` - enforce a non-empty `PdfOptions.FontPath` before layout and emit a diagnostics error when missing.
  Why: prevents layout from running with undefined fonts.
  How: validate `options.Pdf.FontPath` and use `DiagnosticsEventType.Error` on failure.

- [ ] T016 [US2] Class: SkiaTextMeasurer in `src/Html2x/SkiaTextMeasurer.cs` - surface invalid font file resolution via a descriptive exception and diagnostics signal.
  Why: missing or corrupted fonts must be reported clearly.
  How: return a failure result or throw with context that HtmlConverter captures into diagnostics.

---

## Phase 5: User Story 3 - Correct wrapping behavior for long text (Priority: P3)

**Goal**: Wrap at spaces and fall back to character-level breaks for long tokens.

**Independent Test**: Long text wraps at spaces when possible and splits long tokens when needed.

### Tests for User Story 3

- [ ] T017 [P] [US3] Add unit tests in `src/Tests/Html2x.LayoutEngine.Test/Text/LineBoxFragmentTests.cs` covering whitespace wrapping and long-token fallback using a deterministic measurer.
  Why: ensures wrapping rules are stable and regression-safe.
  How: prefer a Moq `Mock<ITextMeasurer>` when practical; assert line counts and run content for a narrow width.

### Implementation for User Story 3

- [ ] T018 [US3] Class: TextWrapper in `src/Html2x.LayoutEngine/Text/TextWrapper.cs` - introduce a wrapper that splits text by whitespace first and falls back to character boundaries.
  Why: consistent wrapping behavior is required for correctness.
  How: use `System.Globalization.StringInfo` to enumerate text elements for fallback.

- [ ] T019 [US3] Class: InlineFragmentStage in `src/Html2x.LayoutEngine/Fragment/Stages/InlineFragmentStage.cs` - integrate TextWrapper to create multiple LineBoxFragments when runs exceed available width.
  Why: inline fragments currently never wrap.
  How: compute line width from the block content box and emit multiple line fragments.

---

## Phase 6: User Story 4 - Testable layout with deterministic measurement sources (Priority: P4)

**Goal**: Allow deterministic tests without real font files.

**Independent Test**: A fake measurer produces stable line breaks and metrics across runs.

### Tests for User Story 4

- [ ] T020 [P] [US4] Add a test double Class: FakeTextMeasurer in `src/Tests/Html2x.LayoutEngine.Test/TestDoubles/FakeTextMeasurer.cs` with deterministic widths and metrics, only if Moq cannot express the needed behavior cleanly.
  Why: enables repeatable tests without font files.
  How: prefer Moq for simple expectations; implement `ITextMeasurer` with fixed outputs only when necessary.

- [ ] T021 [P] [US4] Add tests in `src/Tests/Html2x.LayoutEngine.Test/Text/InlineFragmentStageTests.cs` that inject a deterministic `ITextMeasurer` and verify repeatable output.
  Why: ensures layout supports deterministic injection.
  How: prefer a Moq `Mock<ITextMeasurer>` when practical; use FakeTextMeasurer only if needed, then compare line metrics.

### Implementation for User Story 4

- [ ] T022 [US4] Class: FragmentBuildContext in `src/Html2x.LayoutEngine/Fragment/FragmentBuildContext.cs` - make measurer injection required and ensure tests can pass FakeTextMeasurer.
  Why: determinism depends on injectable services.
  How: remove default constructors that hide dependencies.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, diagnostics, and sample coverage

- [ ] T023 [P] Update documentation in `docs/` to explain the new font measurement contracts, expected font path configuration, and failure modes.
  Why: new extension points must be documented for users.
  How: add a concise section in an existing doc or create a new one if needed.

- [ ] T024 [P] Add a sample HTML file in `src/Tests/Html2x.TestConsole/html/font-measurement.html` and wire it into `src/Tests/Html2x.TestConsole/Program.cs` or `RenderCommand.cs` for manual validation.
  Why: required by constitution and helps manual verification.
  How: include mixed fonts and long text to exercise wrapping.

- [ ] T025 Run quickstart validation using `specs/008-text-measurement/quickstart.md` and update any steps that do not match the new configuration.
  Why: ensures docs stay accurate.
  How: execute the described steps and adjust the quickstart notes.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 -> P2 -> P3 -> P4)
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Phase 2 contracts and wiring
- **User Story 2 (P2)**: Depends on Phase 2 wiring and measurer error paths
- **User Story 3 (P3)**: Depends on Phase 2 wiring and measurer output
- **User Story 4 (P4)**: Depends on Phase 2 wiring to inject test doubles

### Within Each User Story

- Tests MUST be written and fail before implementation
- Contract or helper classes before integration
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- T001 and T002 can run in parallel
- Tests for each user story can run in parallel with other stories after Phase 2

---

## Parallel Example: User Story 1

```bash
# Launch tests for User Story 1 together:
Task: "Add baseline and width tests in src/Tests/Html2x.LayoutEngine.Test/Text/InlineFragmentStageTests.cs"

# Parallel implementation tasks:
Task: "Update TextRunFactory in src/Html2x.LayoutEngine/Fragment/TextRunFactory.cs"
Task: "Update InlineLayoutEngine in src/Html2x.LayoutEngine/Box/InlineLayoutEngine.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. STOP and VALIDATE: Run tests for User Story 1

### Incremental Delivery

1. Complete Setup + Foundational
2. Add User Story 1 -> Test independently -> Validate
3. Add User Story 2 -> Test independently -> Validate
4. Add User Story 3 -> Test independently -> Validate
5. Add User Story 4 -> Test independently -> Validate

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1
   - Developer B: User Story 2
   - Developer C: User Story 3
   - Developer D: User Story 4
