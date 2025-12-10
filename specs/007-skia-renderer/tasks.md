# Tasks: SkiaSharp Renderer Migration

**Input**: Design documents from `/specs/007-skia-renderer/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md (contracts not used for in-proc features)
**Tests**: Tests included where acceptance requires observable behavior
**Organization**: Tasks grouped by user story to enable independent implementation and testing

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 Restore solution to confirm toolchain baseline `src/Html2x.sln`
  ```powershell
  dotnet restore src/Html2x.sln
  ```
- [X] T002 [P] Pin SkiaSharp 3.119.1 central package version in `Directory.Packages.props`
  ```xml
  <PackageVersion Include="SkiaSharp" Version="3.119.1" />
  ```

---

## Phase 2: Foundational (Blocking Prerequisites)

- [ ] T003 [P] Align fragment/render instruction contracts to data-model in `src/Html2x.Abstractions` (add Id and PageNumber to `Fragment`; update Block/LineBox/Image/Rule fragments accordingly; ensure `RenderInstruction` DTO mirrors geometry + fragment id; add validation guards against NaN/Infinity)
  ```csharp
  public abstract class Fragment
  {
      public int FragmentId { get; init; }
      public int PageNumber { get; init; }
      public RectangleF Rect { get; init; }
      public int ZOrder { get; init; }
      public VisualStyle Style { get; init; } = null!;
  }
  ```
- [ ] T004 Ensure renderer project references diagnostics/layout assemblies for Skia path in `src/Html2x.Renderers.Pdf/Html2x.Renderers.Pdf.csproj`
  ```xml
  <ProjectReference Include="..\\Html2x.Diagnostics\\Html2x.Diagnostics.csproj" />
  <ProjectReference Include="..\\Html2x.LayoutEngine\\Html2x.LayoutEngine.csproj" />
  ```

---

## Phase 3: User Story 1 - Deterministic Skia rendering (Priority: P1) ?? MVP

**Goal**: Render fragments via SkiaSharp using absolute coordinates with deterministic output.
**Independent Test**: Render fixed HTML twice and compare geometry annotations/logs for equality.

- [ ] T005 [P] [US1] Add Skia migration HTML sample for manual/test use in `src/Tests/Html2x.TestConsole/html/skia-sample.html`
  ```html
  <p class="abs" style="position:absolute; left:40px; top:80px;">Skia sample</p>
  ```
- [ ] T006 [P] [US1] Add determinism regression test that renders twice and compares fragment geometry in `src/Tests/Html2x.Renderers.Pdf.Test/SkiaDeterminismTests.cs`
  ```csharp
  var run1 = RenderAndDumpGeometry(html);
  var run2 = RenderAndDumpGeometry(html);
  run2.ShouldBe(run1);
  ```
- [ ] T007 [P] [US1] Add diagnostics propagation test covering missing/oversize images and render failures in `src/Tests/Html2x.Renderers.Pdf.Test/SkiaDiagnosticsTests.cs` (assert diagnostics from fragments surface unchanged, including `FragmentId` and optional `SourcePath`; run before T008/T009)
  ```csharp
  var diag = diagnostics.Single(d => d.Code == DiagnosticCode.ImageMissing);
  diag.Message.ShouldContain("missing.png");
  diag.SourcePath.ShouldBe("/html/body/img[1]");
  ```
- [ ] T008 [P] [US1] Add Fragment->RenderInstruction mapping unit test in `src/Tests/Html2x.Renderers.Pdf.Test/RenderInstructionMappingTests.cs` (Why: enforce geometry/id integrity and command type correctness; How: map sample LineBox/Image fragments, assert geometry and IDs preserved, commands are DrawText/DrawImage, and diagnostics are observable when present)
  ```csharp
  var instructions = mapper.Map(fragments);
  instructions.ShouldContain(i => i.Command == DrawCommand.Text && i.FragmentId == frag.FragmentId);
  instructions.ShouldContain(i => i.Command == DrawCommand.Image && i.Geometry.Width == frag.Rect.Width);
  ```
- [ ] T008 [US1] Implement `SkiaPdfRenderer` drawing fragments to `SKDocument` without layout fixes in `src/Html2x.Renderers.Pdf/SkiaPdfRenderer.cs`
  ```csharp
  using var doc = SKDocument.CreatePdf(outputStream);
  using var canvas = doc.BeginPage(page.Width, page.Height);
  foreach (var fragment in fragments) drawer.Draw(canvas, fragment);
  ```
- [ ] T009 [P] [US1] Implement deterministic text/image/path drawers with explicit paints and disposal in `src/Html2x.Renderers.Pdf/ImageRenderer.cs` and `src/Html2x.Renderers.Pdf/TextRenderer.cs`
  ```csharp
  using var paint = new SKPaint { IsAntialias = true, Color = color };
  canvas.DrawText(text, (float)x, (float)y, paint);
  ```
- [ ] T010 [US1] Instrument renderer to forward diagnostics and log render failures in `src/Html2x.Renderers.Pdf/SkiaPdfRenderer.cs` and helpers (emit Html2x.Diagnostics events, avoid renderer-side validation; depends on T007)
  ```csharp
  _recorder.Record(new DiagnosticsEvent
  {
      Type = DiagnosticsEventType.Warning,
      Payload = new RenderDiagnostic { FragmentId = fragment.FragmentId, SourcePath = fragment.SourcePath }
  });
  try { drawer.Draw(canvas, fragment); }
  catch (Exception ex) { _recorder.Record(RenderFailure(fragment.FragmentId, fragment.SourcePath, ex)); throw; }
  ```
- [ ] T011 [US1] Wire Skia renderer into composition/entry point so HtmlConverter uses it by default in `src/Html2x/HtmlConverter.cs` without dependency injection (instantiate on demand alongside the existing layout factory pattern).
  ```csharp
  var renderer = new SkiaPdfRenderer();
  var pdfBytes = await renderer.RenderAsync(layout, options.Pdf, session);
  ```

**Checkpoint**: Skia renderer produces repeatable output; determinism test passes.

---

## Phase 4: User Story 2 - Remove QuestPdf dependency (Priority: P2)

**Goal**: Build and runtime depend only on SkiaSharp; no QuestPdf references remain.
**Independent Test**: Build succeeds and dependency inspection shows only SkiaSharp renderer path.

- [ ] T012 [US2] Remove QuestPdf package references from `Directory.Packages.props` and `src/Html2x.Renderers.Pdf/Html2x.Renderers.Pdf.csproj`
  ```xml
  <!-- remove -->
  <!-- <PackageReference Include="QuestPDF" /> -->
  ```
- [ ] T013 [P] [US2] Replace QuestPdf usings/classes with Skia equivalents or delete unused files in `src/Html2x.Renderers.Pdf` and `src/Tests/Html2x.Renderers.Pdf.Test`
  ```csharp
  // delete using QuestPDF.Fluent;
  // use SkiaSharp canvases instead
  ```
- [ ] T014 [US2] Update console harness/project files to Skia-only pipeline in `src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj` and `specs/007-skia-renderer/quickstart.md`
  ```xml
  <PackageReference Include="SkiaSharp" />
  <PackageReference Include="SkiaSharp.NativeAssets.Win32" />
  ```

**Checkpoint**: Build and tests reference only SkiaSharp; no QuestPdf namespaces remain.

---

## Phase 5: User Story 3 - Controlled migration posture (Priority: P3)

**Goal**: Keep Skia renderer tests always on; no skip flags or compatibility modes.
**Independent Test**: Full test suite runs without skips; Skia is the sole renderer.

- [ ] T015 [P] [US3] Remove any Skia migration skips and ensure renderer tests run unconditionally in `src/Tests/Html2x.Renderers.Pdf.Test/*.cs`.
  ```csharp
  [Fact] // no SkippableFact
  public void Renderer_test() { /* ... */ }
  ```
- [ ] T016 [US3] Delete config flags/environment switches related to skipping renderer tests; test startup should always execute the Skia path.
- [ ] T017 [US3] Update `specs/007-skia-renderer/quickstart.md` to state there is no compatibility or skip mode—Skia is the default and only renderer path.

**Checkpoint**: Skipped tests are explicit, reversible, and reported.

---

## Dependencies & Execution Order

- Setup (Phase 1) -> Foundational (Phase 2) -> User Stories (Phases 3-5).
- Story order by priority: US1 (P1) -> US2 (P2) -> US3 (P3). US2 can start after T004 but should not ship before US1 is green.
- Tests within a story follow: determinism test before renderer implementation; no skip toggles or compatibility modes.

### Parallel Opportunities
- T002, T003, T005, T006, T008, T011, T013, T016 can run in parallel (different files, no blocking dependencies).
- After Phase 2, US1 and US2 groundwork can proceed in parallel if coordination on shared files (`SkiaPdfRenderer.csproj`) is sequenced.

## Implementation Strategy

MVP = complete US1 (deterministic Skia renderer) after Setup and Foundational, then validate determinism test before proceeding. Deliver US2 (QuestPdf removal) next, followed by US3 (skip posture). Polish documents the runbook and quickstart verification.
