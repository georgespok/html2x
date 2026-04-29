# Testing

Html2x tests should describe observable behavior: computed styles, geometry,
fragments, diagnostics, PDF validity, extracted text, and public API results.

## Test Projects

| Project | Scope |
| --- | --- |
| `Html2x.LayoutEngine.Contracts` | Internal pipeline handoff contracts shared by style, geometry, composition, diagnostics, and fragment projection. |
| `Html2x.LayoutEngine.Style.Test` | CSS computation, `StyleTreeBuilder`, parser-backed style behavior, user agent stylesheet behavior, style diagnostics, and the parser-free `StyleTree` handoff contract. |
| `Html2x.LayoutEngine.Test` | Pipeline integration, box role behavior, initial box construction, fragments, pagination, diagnostics snapshots, and architecture guardrails. |
| `Html2x.LayoutEngine.Geometry.Test` | Geometry algorithms, published layout output, table layout, image layout, block and inline geometry, and parser-free `StyleTree` inputs. |
| `Html2x.Renderers.Pdf.Test` | PDF renderer behavior, drawing helpers, fonts, images, borders, table rendering, and PDF validation helpers. |
| `Html2x.Test` | End-to-end `HtmlConverter` scenarios and diagnostics behavior. |
| `Html2x.TestConsole.Test` | Manual harness parsing and diagnostics envelope behavior. |

## Ownership Rules

Style tests belong in `Html2x.LayoutEngine.Style.Test` when they exercise:

- `CssStyleComputer`.
- `StyleTreeBuilder`.
- User agent stylesheet application.
- Parser-backed style traversal.
- Style diagnostics.
- `StyleTree`, `StyledElementFacts`, and `StyleContentNode` contract behavior.

Pure contract validation can remain beside the stage tests that already assert
the behavior. If a separate `Html2x.LayoutEngine.Contracts.Test` project is
created, put validation-only coverage there: identity value validation,
defensive copies, published identity separation, and final geometry value
validation.

Geometry tests belong in `Html2x.LayoutEngine.Geometry.Test` when they exercise
layout geometry or published layout facts. Geometry tests must not reference AngleSharp, AngleSharp.Css, `IElement`, `INode`, DOM child nodes, or DOM mocks.
Use parser-free builders with `StyledElementFacts` and `StyleContentNode` when
constructing `StyleTree` input.

Geometry tests must not add parser references just because contracts moved to
`Html2x.LayoutEngine.Contracts`. Use contract style input for geometry
algorithms and reserve parser-backed traversal for Style.Test.

Pipeline tests belong in `Html2x.LayoutEngine.Test` when they exercise
`LayoutBuilder`, diagnostics flow, fragments, pagination, or architecture
guardrails. Pipeline tests should use the public or intended module facades, not
direct parser or style implementation construction.

LayoutEngine.Test owns orchestration, diagnostics, fragments, pagination, and
architecture guardrails. It may inspect contract handoff facts, but it must not
construct parser providers or mutable geometry internals outside focused helper
coverage.

## Practices

- Start with a failing test for meaningful logic.
- Assert behavior, not implementation details.
- Do not use reflection in unit tests.
- Use Moq when it keeps dependency setup explicit and simpler than handwritten
  fakes.
- Prefer `[Theory]` with `[InlineData]`, `[MemberData]`, or `[ClassData]` for
  permutations.
- Keep arrange, act, and assert blocks compact.
- Avoid binary PDF equality.

## Identity-Aware Fixtures

Prefer `StyleTreeBuilder` for production-like identity tests. It exercises the
same parser traversal, `StyleSourceIdentity`, and `StyleContentIdentity`
assignment path used by the converter.

Use shared style node or geometry builders when direct model construction is
required. Keep ad hoc fixtures parser-free and assign explicit identity only
when the test asserts identity behavior.

Use unspecified identity only for tests that do not assert source identity. A
test that verifies `SourceNodeId`, `SourceContentId`, `SourcePath`, source
order, or generated source kind must build specified source identity.

Do not use the Reflection namespace in unit tests. Use Moq or small explicit
test doubles when dependency setup needs isolation.

Contract tests must not use the Reflection namespace. Validate through public
constructors, internal friend-assembly access, or shared builders.

Geometry tests must not reference AngleSharp, AngleSharp.Css, parser DOM
interfaces, DOM child nodes, or DOM mocks.

Diagnostics tests must assert `NodePath` and `SourcePath` separately when
source identity is present. `NodePath` is layout identity from published
geometry. `SourcePath` is source identity from style or generated geometry
identity.

## Naming

Use `[Unit]_[Condition]_[ExpectedBehavior]`. Omit the condition when it does not
add useful meaning.

Examples:

- `ToPdfAsync_UsesSingleFontPath`
- `LayoutEngine_TableHasNoWidths_DistributesColumnsEvenly`

Rules:

- Target 70 characters or fewer. Never exceed 120 characters.
- Use domain terms such as layout, box tree, fragment tree, rendering,
  diagnostics, and font path.
- Avoid filler words such as `Should`, `Correctly`, `Properly`, and
  `Successfully`.
- Do not include implementation flags, helper names, or debug concepts unless
  they are observable behavior.
- If a name needs multiple expectations, split the test.

## Fact vs Theory

Use `[Fact]` for one clear behavior. Use `[Theory]` when the same behavior is
exercised with different inputs. Prefer inline data for simple scalar cases and
member data for structured fragments, styles, diagnostics, or renderer cases. Do
not convert a test to a theory when the data table hides the scenario.

## Diagnostics-First Verification

When PDF internals are hard to assert, expose the fact through diagnostics and
assert the serialized payload. This is more stable than parsing raw content
streams.

Good diagnostic assertions cover:

- Event name.
- Severity.
- Stage state when relevant.
- Payload kind.
- Source context.
- Stable ordering where ordering is part of the contract.

## Snapshot Guidance

Prefer explicit assertions. Use snapshots only when the value is structured,
deterministic, and easier to understand as a whole.

If a snapshot changes intentionally:

- Keep the snapshot focused.
- Explain the behavior change in the commit or PR summary.
- Update nearby tests when the change crosses layers.

## Commands

```powershell
dotnet build src\Html2x.sln -c Release --no-restore
dotnet test src\Tests\Html2x.LayoutEngine.Style.Test\Html2x.LayoutEngine.Style.Test.csproj -c Release --no-build
dotnet test src\Tests\Html2x.LayoutEngine.Geometry.Test\Html2x.LayoutEngine.Geometry.Test.csproj -c Release --no-build
dotnet test src\Tests\Html2x.LayoutEngine.Test\Html2x.LayoutEngine.Test.csproj -c Release --no-build
dotnet test src\Html2x.sln -c Release --no-build
dotnet test src\Tests\Html2x.LayoutEngine.Test\Html2x.LayoutEngine.Test.csproj -c Release --no-build --filter FullyQualifiedName~Architecture
```
