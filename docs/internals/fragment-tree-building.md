# Fragment Tree Building

This page explains how `Html2x.LayoutEngine.Fragments` converts published
layout facts into renderer-facing render model fragments.

## Responsibility

Fragment tree building consumes `PublishedLayoutTree` and produces `FragmentTree`.
It allocates fragment IDs and copies geometry, visual style, display metadata,
text run facts, image facts, rule facts, and table facts into render model
fragments.

Internal build state owns block bindings, visited sets, and fragment ID
reservation for one build. That state is private to
`Html2x.LayoutEngine.Fragments` and is not a public visitor or fragment plugin
surface.

## Input And Output

Input:

- `PublishedLayoutTree` from `Html2x.LayoutEngine.Contracts.Published`.

Output:

- `FragmentTree` containing block, line, text, image, rule, table, row, and
  cell fragments.

## Build Rules

- Published layout geometry is copied forward, not recalculated.
- Published text runs keep their `ResolvedFont` facts.
- Visual style values are copied into render model `VisualStyle`.
- Image fragments carry source, status, content rectangle, and border facts.
- Table fragments preserve table, row, cell, span, header, and background
  facts required by pagination diagnostics and rendering.

## Forbidden Dependencies

Fragment tree building must not consume:

- Mutable boxes.
- CSS parser state.
- DOM objects.
- Text or font adapter seams.
- Pagination pages.
- Renderer state.
- SkiaSharp.

If rendering needs a value that fragments do not carry, add the value to the
published layout or fragment contract owned by the producing stage.

## Tests

Fragment tree building tests belong in `Html2x.LayoutEngine.Fragments.Test`. They
should build `PublishedLayoutTree` inputs directly and assert `FragmentTree`
output.
