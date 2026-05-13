# Fragment Kinds

Use this page when adding a new render model fragment kind. A fragment kind is
not a local renderer detail; it crosses fragment tree building, pagination,
diagnostics snapshots, and rendering.

## Required Updates

Adding a fragment kind requires coordinated updates to:

- Published layout facts, if geometry must publish new data.
- Fragment tree building from `PublishedLayoutTree`.
- Render model fragment type or closed fragment set.
- Pagination clone and translation behavior.
- Pagination audit metadata when placement diagnostics need the new facts.
- Diagnostics snapshot mapping.
- Renderer paint command resolution and drawing.
- Tests in the owning stage, fragment tree building, pagination, diagnostics, and
  renderer projects.
- Reference docs when the feature changes supported behavior.

## Boundary Rules

- Do not make renderers inspect DOM, CSS, style, or box objects.
- Do not make pagination repair missing geometry.
- Do not remeasure text or images in fragment tree building.
- Do not add custom fragment subclasses as an unsupported extension path for
  the built-in pipeline.
- Do not rely on diagnostics leaking unknown fragment type names. Unknown
  fragment subclasses are reported with the bounded `unsupported` snapshot kind.

## Validation Focus

Use semantic assertions over binary equality. For pagination, assert translated
coordinates and preserved nested facts. For rendering, assert paint behavior,
diagnostics, or extracted output rather than raw PDF byte equality.
