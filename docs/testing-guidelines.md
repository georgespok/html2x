# Testing Guidelines

These practices keep Html2x shippable while the surface area grows. Every contribution is expected to follow incremental TDD and preserve deterministic outputs.

## Foundational Practices

- **Fail first**: Introduce one failing test, implement the minimal code to pass, then refactor. Constructors and trivial DTOs are the only exceptions.
- **Behavior over structure**: Assertions must focus on observable outcomes (fragment shapes, PDF metadata, layout positions) rather than internal state or reflection.
- **No reflection**: `Activator.CreateInstance`, `Type.GetType`, `MethodInfo.Invoke`, etc., are banned in test projects. Favor direct API calls or explicit test doubles.
- **Layer isolation**: Each stage (Abstractions, LayoutEngine, PDF renderer) must be testable in isolation. Integration tests cover the seams, not the only validation.

## Test Layout by Project

| Project | Scope | Typical Scenarios | Notes |
| --- | --- | --- | --- |
| `Html2x.LayoutEngine.Test` | Value types, DOM load, style cascade, box flow, fragmentation. | HTML fixtures producing expected fragments or pagination rules, geometry edge cases. | Use snapshot helpers sparingly; prefer explicit assertions and `[Theory]` coverage. |
| `Html2x.Renderers.Pdf.Test` | Fragment dispatch and QuestPDF integration. | Rendering simple layouts, validating fonts, line weights, warnings for unsupported fragments. | Record fragments (`RecordingFragmentRenderer`) when diagnosing traversal. |
| `Html2x.Test` | End-to-end `HtmlConverter` verification. | Whole documents, PDF header checks, logging emission via `TestOutputLoggerProvider`. | Keep scenarios realistic and document the purpose in comments. |

## Writing High-Quality Tests

- **Name** tests following `Method_Scenario_Expectation`. Prefer expressive names over abbreviations.
- **Arrange-Act-Assert**: Keep blocks visually distinct. Extract helpers when repeated setups appear.
- **Use `[Theory]` first**: Parameterize scenarios with `[InlineData]`, `[MemberData]`, or `[ClassData]` to collapse repetitive tests. Reserve `[Fact]` for unique flows.
- **No magic numbers**: When asserting coordinates or sizes, describe meaning (e.g., `expectedBaseline`) in variable names or comments.
- **Keep tests short**: Aim for <=15 executable lines. If longer, consider helper methods or additional test classes.

## Snapshot & Golden Files

- Prefer programmatic assertions. If snapshots are necessary, store them under the test project in a clearly named folder (`Snapshots/`).
- Update snapshots only with a corresponding explanation in the PR description. Include a `Assert.True(Fixture.Verify(...))` style call rather than raw string equality.
- When changing expected output intentionally, provide before/after commentary in the commit message to aid reviewers.

## PDF Validation Guidance

- Use `PdfWordParser` to read text content and attributes; verify font usage, positions, and styling.
- `PdfValidator` should back structural assertions (valid PDF, page count). Do not assert binary equality; prefer semantic checks.
- For regression debugging, store PDFs temporarily using `SavePdfForInspectionAsync` and link the temp path in test output.

## Integration Strategy

1. Cover the same user journey once. Avoid duplicating the same checks across multiple integration tests.
2. Use real options and fonts shipped with the repo to catch platform-specific issues.
3. Capture logging output via the `TestOutputLoggerProvider` when verifying diagnostics or error flows.
4. Validate error propagation; assert the thrown exception type and that logs surface the failure.

## Continuous Improvement

- Review test suites periodically to consolidate similar `[Theory]` cases.
- When adding new CSS or renderer features, start by extending unit tests, then add a focused integration scenario.
- Before merging, run `dotnet test Html2x.sln -c Release` and ensure diagnostics remain informative.

Good tests are readable specifications. Treat them as documentation for future contributors.
