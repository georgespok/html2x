# Tasks: Basic <img> Support

**Input**: Design documents from `C:\Projects\html2x\specs\006-basic-img\`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md

## Phase 1: Setup (Shared Infrastructure)

- [ ] T001 Create `src/Html2x.Renderers.Pdf/Images` folder to host image rendering helpers (ensures a dedicated place for code and placeholder asset).
  - Why: keeps image assets separate from logic so code stays clean.
  - How: in your file explorer or via `mkdir` create that path. No coding.
- [ ] T002 Add placeholder PNG (small missing-image icon) to `src/Html2x.Renderers.Pdf/Images/placeholder.png` for use when loads fail.
  - Why: renderer needs a visible fallback when an image is missing or blocked.
  - How: save a 64x64 PNG with an “X” icon; ensure file name matches. Size is small to avoid bloat.
- [ ] T003 Ensure `dotnet restore Html2x.sln` succeeds and dependencies (QuestPDF) are available; document command in `specs/006-basic-img/quickstart.md`.
  - Command: `dotnet restore Html2x.sln`
  - If it succeeds, copy the command and its success note into quickstart.md so newcomers know the first step.

## Phase 2: Foundational (Blocking Prerequisites)

- [ ] T004 Define image fragment contracts in `src/Html2x.Abstractions/Layout/Fragments/ImageFragment.cs` to carry src, computed size, and failure state; keep fields readonly and XML documented.
  - Add properties: `string Src`, `double? AuthoredWidthPx`, `double? AuthoredHeightPx`, `double IntrinsicWidthPx`, `double IntrinsicHeightPx`, `bool IsMissing`, `bool IsOversize`.
  - Code sketch:
    ```csharp
    public sealed class ImageFragment : Fragment
    {
        public string Src { get; }
        public double? AuthoredWidthPx { get; }
        public double? AuthoredHeightPx { get; }
        public double IntrinsicWidthPx { get; }
        public double IntrinsicHeightPx { get; }
        public bool IsMissing { get; }
        public bool IsOversize { get; }
    }
    ```
- [ ] T005 Add image diagnostics payload fields in `src/Html2x.Diagnostics/HtmlPayload.cs` for image load result (enum), applied scale, and warning; update factory methods.
  - Add enum `ImageStatus { Ok, Missing, Oversize }` in diagnostics namespace; store the enum value, not a string.
  - Code sketch:
    ```csharp
    public enum ImageStatus { Ok, Missing, Oversize }
    payload.ImageStatus = fragment.IsMissing ? ImageStatus.Missing
                       : fragment.IsOversize ? ImageStatus.Oversize
                       : ImageStatus.Ok;
    payload.AppliedScale = scale;
    if (fragment.IsOversize) payload.Warning = "Image rejected: >10MB";
    ```
- [ ] T006 Wire image fragment construction in layout pipeline: update `src/Html2x.LayoutEngine/Fragment/FragmentBuilder.cs` to emit `ImageFragment` with authored width/height, provisional intrinsic size, and oversize flags.
  - Without an image decoder in layout, set `intrinsicW/H` to authored values when present, otherwise 0; renderer will re-validate size against bytes and apply caps.
  - Code sketch:
    ```csharp
    var iw = authoredWidth ?? 0;
    var ih = authoredHeight ?? 0;
    var frag = new ImageFragment(src, authoredWidth, authoredHeight, iw, ih, isMissing, isOversize);
    fragments.Add(frag);
    ```
- [ ] T007 Add inline-block handling for images in `src/Html2x.LayoutEngine/Box/DisplayTreeBuilder.cs` to ensure images create inline boxes with baseline alignment metadata.
  - Create an inline `Box` with width/height from fragment, set `BaselineOffset = height` for now or a simple `height * 0.8` if font metrics unavailable.
  - Code sketch:
    ```csharp
    var box = new InlineBox(width, height) { BaselineOffset = height * 0.8 };
    box.Fragment = imageFragment;
    currentLine.Add(box);
    ```
- [ ] T008 Add configurable max image size (MB) to the options layer (`PdfOptions` in `src/Html2x.Abstractions/Options/PdfOptions.cs`, default 10) and thread it through to the renderer.
  - Add property: `public double MaxImageSizeMb { get; set; } = 10;`
  - Convert once to bytes and pass via existing options plumbing into the renderer.
  - Code sketch:
    ```csharp
    public double MaxImageSizeMb { get; set; } = 10;
    public long MaxImageSizeBytes => (long)(MaxImageSizeMb * 1024 * 1024);
    // later in renderer creation
    renderer.MaxImageSizeBytes = options.MaxImageSizeBytes;
    ```
- [ ] T009 Implement image byte loading helper in `src/Html2x.Renderers.Pdf/ImageLoader.cs` to read local files and data URIs with path-scope check.
  - Steps: if `src` starts with `data:`, decode base64; else combine with input HTML directory and read bytes; reject if outside allowed scope.
  - Code sketch:
    ```csharp
    if (IsDataUri(src)) return DecodeDataUri(src);
    var full = Path.GetFullPath(Path.Combine(htmlDir, src));
    if (!full.StartsWith(htmlDir, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("out of scope");
    return File.ReadAllBytes(full);
    ```

## Phase 3: User Story 1 - Inline image renders with explicit size (Priority: P1)

**Goal**: Render images at authored width or height while preserving aspect ratio.
**Independent Test**: Render HTML with width-only and height-only images; verify sizes match expectations without distortion.

- [ ] T010 [US1] Implement size resolution logic in `src/Html2x.LayoutEngine/Fragment/StyleConverter.cs` to compute missing dimension from intrinsic aspect ratio when only one is provided.
  - Formula: `missing = given * intrinsicOther / intrinsicGiven`.
  - Code sketch:
    ```csharp
    double Resolve(double? w, double? h, double iw, double ih)
    {
        if (w.HasValue && h.HasValue) return (w.Value, h.Value); // caller handles both
        if (w.HasValue) return (w.Value, w.Value * ih / iw);
        if (h.HasValue) return (h.Value * iw / ih, h.Value);
        return (iw, ih);
    }
    ```
- [ ] T011 [US1] Enforce container/page fitting in `src/Html2x.LayoutEngine/Box/BoxTreeBuilder.cs`: cap box size to available inline space and page bounds while preserving aspect ratio.
  - Steps: compute availableWidth; if image width > availableWidth, scale both width/height by factor `availableWidth / width`.
  - Code sketch:
    ```csharp
    var scale = Math.Min(1.0, availableWidth / width);
    width *= scale; height *= scale;
    ```
- [ ] T011a [US1] Add layout unit test in `src/Tests/Html2x.LayoutEngine.Test/BoxTreeBuilderTests.cs` to verify container fitting: given 200px available width and image target 300x150, expect final 200x100.
  - Use layout test helper to simulate available inline width.
  - Code sketch:
    ```csharp
    var box = BuildImageBox(300,150, availableWidth:200);
    box.Width.Should().BeApproximately(200,1);
    box.Height.Should().BeApproximately(100,1);
    ```
- [ ] T012 [US1] Render images in PDF: add a renderer helper `src/Html2x.Renderers.Pdf/ImageRenderer.cs` that accepts `ImageFragment`, sets QuestPDF `Image` with computed width/height, and preserves aspect ratio.
  - Guard size caps and missing flag:
    ```csharp
    if (fragment.IsMissing || fragment.IsOversize)
        return RenderPlaceholder(width, height, page);
    if (imgBytes.Length > maxImageSizeBytes)
        return RenderPlaceholder(width, height, page);
    page.Element().Width(width).Height(height).Image(imgBytes);
    ```
  - `RenderPlaceholder` will be implemented in US3 tasks.
- [ ] T013 [US1] Add sample HTML with explicit width and height cases to `src/Tests/Html2x.TestConsole/html/img-explicit-size.html` demonstrating P1 scenarios.
  - Include examples:
    ```html
    <p>Width only: <img src="images/cat.png" width="120" /></p>
    <p>Height only: <img src="images/cat.png" height="80" /></p>
    ```
- [ ] T014 [US1] Add a renderer unit test in `src/Tests/Html2x.Renderers.Pdf.Test/ImageRenderingTests.cs` that renders the width-only/height-only sample and asserts diagnostics report `ImageStatus.Ok` and sizes within ±1px of expected.
  - Use TestConsole pipeline or a minimal render helper to produce diagnostics without persisting PDF.
  - Code sketch:
    ```csharp
    var result = RenderHtml(sampleHtml);
    result.Diagnostics.Images.Should().ContainSingle(i => i.Status == ImageStatus.Ok);
    result.Diagnostics.Images[0].RenderedWidth.Should().BeApproximately(120, 1);
    ```

## Phase 4: User Story 2 - Images align with surrounding text (Priority: P2)

**Goal**: Images behave as inline-block so text flows naturally around them with correct baseline.
**Independent Test**: Render paragraph with mid-sentence image; text stays on line until natural wrap and image sits on baseline.

- [ ] T020 [US2] Ensure inline box baseline metrics set: update `src/Html2x.LayoutEngine/Models/InlineBox.cs` to carry image baseline offset derived from fragment.
  - Baseline idea: `BaselineOffset = boxHeight * 0.8` as a starting heuristic.
  - Code sketch:
    ```csharp
    BaselineOffset = Height * 0.8;
    ```
- [ ] T021 [US2] In PDF renderer, place image inside inline container respecting baseline in `src/Html2x.Renderers.Pdf/ImageRenderer.cs` (baseline offset applied via QuestPDF text span alignment or vertical translate).
  - Apply vertical translate: move image down by `(baselineOffset - height)` to align with text baseline.
  - Code sketch:
    ```csharp
    canvas.TranslateY(baselineOffset - height);
    canvas.DrawImage(img);
    ```
- [ ] T022 [US2] Add inline alignment sample HTML to `src/Tests/Html2x.TestConsole/html/img-inline-flow.html` showing text before/after image.
  - Example:
    ```html
    <p>Hello <img src="images/cat.png" width="40" /> world flowing text.</p>
    ```
- [ ] T023 [US2] Add a layout unit test in `src/Tests/Html2x.LayoutEngine.Test/InlineAlignmentTests.cs` that builds a line with an inline image and asserts baseline alignment (image baseline within 1px of surrounding text ascent).
  - Use existing layout test helpers to produce line boxes and compare `BaselineOffset`.
  - Code sketch:
    ```csharp
    var line = BuildLine("Hello ", imgFragment40px, " world");
    line.InlineBoxes[1].BaselineOffset.Should().BeApproximately(line.Baseline, 1);
    ```

## Phase 5: User Story 3 - Graceful handling when images fail (Priority: P3)

**Goal**: Keep layout readable when image is missing/unreachable.
**Independent Test**: Render HTML with invalid image src; placeholder appears with expected size; layout stable.

- [ ] T030 [US3] Implement path-scope validation in `src/Html2x/LayoutEngine/Box/DisplayTreeBuilder.cs` (or a new validator) to allow only data URIs or file paths under the input HTML directory; mark fragment as missing otherwise.
  - Check: `if (!isDataUri && !path.StartsWith(inputDir, StringComparison.OrdinalIgnoreCase)) mark missing`.
  - Code sketch:
    ```csharp
    bool allowed = isDataUri || path.StartsWith(htmlDir, StringComparison.OrdinalIgnoreCase);
    fragment.IsMissing = !allowed;
    ```
- [ ] T031 [US3] Add oversize rejection (>10 MB) before rendering in `src/Html2x.Renderers.Pdf/ImageRenderer.cs`; log warning via Diagnostics payload.
  - Use options: if `bytes.Length > MaxImageSizeBytes`, set `IsOversize = true`, skip image draw.
  - Code sketch:
    ```csharp
    if (bytes.Length > maxImageSizeBytes) {
        payload.Warning = "Image rejected: oversize";
        return placeholder;
    }
    ```
- [ ] T032 [US3] Implement placeholder rendering in `src/Html2x.Renderers.Pdf/ImageRenderer.cs` using the embedded placeholder PNG when load fails or is rejected; keep box size equal to expected dimensions.
  - Load placeholder bytes once (static) and draw with same width/height as requested.
  - Code sketch:
    ```csharp
    var bytes = PlaceholderBytes.Value;
    page.Element().Width(width).Height(height).Image(bytes);
    ```
- [ ] T033 [US3] Add failure-handling sample HTML to `src/Tests/Html2x.TestConsole/html/img-missing.html` with bad src and oversize note.
  - Example:
    ```html
    <p>Broken image: <img src="images/does-not-exist.png" width="120" /></p>
    ```
- [ ] T034 [US3] Add a renderer diagnostic test in `src/Tests/Html2x.Renderers.Pdf.Test/ImageFailureTests.cs` that uses the bad-src sample and asserts diagnostics show `ImageStatus.Missing` and that a placeholder was rendered (flag or size match).
  - Code sketch:
    ```csharp
    var result = RenderHtml(badSample);
    result.Diagnostics.Images[0].Status.Should().Be(ImageStatus.Missing);
    result.Diagnostics.Images[0].RenderedWidth.Should().Be(120);
    ```

## Phase 6: Polish & Cross-Cutting

- [ ] T040 Add documentation snippet to `docs/coding-standards.md` describing image handling rules (size caps, path scope, placeholder behavior).
  - Include bullet list of the rules; no code changes.
- [ ] T041 Add note to `quickstart.md` about running the new HTML samples through TestConsole and expected outputs.
  - Show command:
    ```bash
    dotnet run --project src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj -- --input src/Tests/Html2x.TestConsole/html/img-inline-flow.html --output build/img-inline.pdf
    ```
- [ ] T042 Run `dotnet test Html2x.sln -c Release` and record results in `specs/006-basic-img/quickstart.md`.
  - Paste the summary line from test output (e.g., `Passed! 100 tests`).

## Dependencies & Execution Order

- Foundational Phase (T004-T007) must complete before any user story tasks.
- User Story order by priority: US1 (T010-T013) -> US2 (T020-T022) -> US3 (T030-T033). Each story is independently testable once its tasks finish.
- Polish tasks follow all stories.

## Parallel Opportunities

- T001-T003 can run in parallel (different files/assets).
- Within Foundational: T004 and T005 can run in parallel; T006 depends on T004; T007 can proceed after T006.
- US1: T010-T011 in parallel; T012 after T010/T011; T013 independent.
- US2: T020-T021 sequential; T022 independent.
- US3: T030 before T031/T032; T033 independent after T030.

## Implementation Strategy

- MVP: Deliver US1 first (T010-T013) after foundational; verify explicit sizing works.
- Incremental: Add US2 for inline alignment; then US3 for failure handling.
- Validation: Use TestConsole HTML samples per story; ensure diagnostics warnings appear for missing/oversize cases.
