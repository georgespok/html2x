# Testing

Html2x tests should describe observable behavior: computed styles, geometry, fragments, diagnostics, PDF validity, extracted text, and public API results.

## Test Projects

| Project | Scope |
| --- | --- |
| `Html2x.LayoutEngine.Test` | Style computation, box role behavior, box layout, geometry, fragments, pagination, diagnostics snapshots. |
| `Html2x.Renderers.Pdf.Test` | PDF renderer behavior, drawing helpers, fonts, images, borders, table rendering, PDF validation helpers. |
| `Html2x.Test` | End-to-end `HtmlConverter` scenarios and diagnostics behavior. |
| `Html2x.TestConsole.Test` | Manual harness parsing and diagnostics envelope behavior. |

## Practices

- Start with a failing test for meaningful logic.
- Assert behavior, not implementation details.
- Do not use reflection in unit tests.
- Use Moq when it keeps dependency setup explicit and simpler than handwritten fakes.
- Prefer `[Theory]` with `[InlineData]`, `[MemberData]`, or `[ClassData]` for permutations.
- Keep arrange, act, and assert blocks compact.
- Avoid binary PDF equality.

## Naming

Use `[Unit]_[Condition]_[ExpectedBehavior]`. Omit the condition when it does not add useful meaning.

Examples:

- `ToPdfAsync_UsesSingleFontPath`
- `LayoutEngine_TableHasNoWidths_DistributesColumnsEvenly`

Rules:

- Target 70 characters or fewer. Never exceed 120 characters.
- Use domain terms such as layout, box tree, fragment tree, rendering, diagnostics, and font path.
- Avoid filler words such as `Should`, `Correctly`, `Properly`, and `Successfully`.
- Do not include implementation flags, helper names, or debug concepts unless they are observable behavior.
- If a name needs multiple expectations, split the test.

## Fact vs Theory

Use `[Fact]` for one clear behavior. Use `[Theory]` when the same behavior is exercised with different inputs. Prefer inline data for simple scalar cases and member data for structured fragments, styles, diagnostics, or renderer cases. Do not convert a test to a theory when the data table hides the scenario.

## Diagnostics-First Verification

When PDF internals are hard to assert, expose the fact through diagnostics and assert the serialized payload. This is more stable than parsing raw content streams.

Good diagnostic assertions cover:

- Event name.
- Severity.
- Stage state when relevant.
- Payload kind.
- Source context.
- Stable ordering where ordering is part of the contract.

## Snapshot Guidance

Prefer explicit assertions. Use snapshots only when the value is structured, deterministic, and easier to understand as a whole.

If a snapshot changes intentionally:

- Keep the snapshot focused.
- Explain the behavior change in the commit or PR summary.
- Update nearby tests when the change crosses layers.

## Commands

```powershell
dotnet test src/Html2x.sln -c Release
dotnet test src/Tests/Html2x.LayoutEngine.Test/Html2x.LayoutEngine.Test.csproj -c Release --filter "FullyQualifiedName~Geometry"
dotnet test src/Tests/Html2x.Renderers.Pdf.Test/Html2x.Renderers.Pdf.Test.csproj -c Release
```
