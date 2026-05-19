# Html2x.LayoutEngine.Stage.Contracts

This project owns composition-facing invocation contracts for replaceable
Layout Engine stages.

Use it when `Html2x.LayoutEngine` needs to call a stage through a stable
execution seam instead of referencing a concrete implementation project. A
stage invocation contract describes how composition runs a stage for one build:
the input facts, execution plumbing, and output facts.

## Role

`Html2x.LayoutEngine.Stage.Contracts` is an execution-contract project. It is
allowed to reference `Html2x.Diagnostics.Contracts` because diagnostics sinks
are per-build execution plumbing.

It currently contains the Layout Geometry invocation contract:

```text
ILayoutGeometryStage
LayoutGeometryBuildRequest
```

The contract consumes existing handoff facts such as `StyleTree` and
`LayoutGeometryRequest`, and returns `PublishedLayoutTree`. It does not own
those facts.

## Put Types Here

Put a type in this project when all of these are true:

- The type describes how `Html2x.LayoutEngine` invokes a stage.
- The type is needed to decouple composition from one or more stage
  implementation projects.
- The type may reasonably include execution plumbing such as
  `IDiagnosticsSink`.
- The type can be implemented by an adapter in a stage implementation project.

Good examples:

```text
IExampleStage
ExampleStageBuildRequest
```

## Do Not Put Types Here

Do not put a type in this project when it is a stage handoff fact, layout
algorithm, adapter implementation, or diagnostic runtime type.

Use these owners instead:

```text
Html2x.LayoutEngine.Contracts
  StyleTree, LayoutGeometryRequest, PublishedLayoutTree, UsedGeometry.

Html2x.LayoutEngine.Geometry
  Geometry algorithms, mutable boxes, measurement, publishing, image sizing,
  table placement, and layout diagnostics emission.

Html2x.Diagnostics.Contracts
  IDiagnosticsSink and diagnostic record contracts.

Html2x.Diagnostics
  Diagnostics collection, report model, and JSON serialization.
```

## Rules

- Keep this project internal-only.
- Keep contracts small and build-scoped.
- Do not add layout behavior.
- Do not expose mutable boxes, parser objects, fragments, pagination pages, or
  renderer state.
- Do not redefine diagnostics contracts.
- Do not add a stage interface before there is a real composition need or a
  planned second implementation.
- Prefer a concrete stage dependency until replacement is a real requirement.

## Dependency Shape

Allowed:

```text
Html2x.LayoutEngine.Stage.Contracts
  -> Html2x.Diagnostics.Contracts
  -> Html2x.LayoutEngine.Contracts
```

Forbidden:

```text
Html2x.LayoutEngine.Stage.Contracts
  -> Html2x.LayoutEngine.Geometry
  -> Html2x.LayoutEngine.Style
  -> Html2x.LayoutEngine.Fragments
  -> Html2x.LayoutEngine.Pagination
  -> Html2x.Diagnostics
  -> renderer projects
  -> parser packages
```
