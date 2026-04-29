# Layout Engine

`Html2x.LayoutEngine` composes the conversion pipeline from raw HTML to
`HtmlLayout` pages. It does not own parser implementation details or layout
geometry algorithms directly.

## Responsibilities

- Call the style module to parse HTML and CSS and produce `StyleTree`.
- Call the geometry module to produce `PublishedLayoutTree`.
- Project published layout into fragments.
- Paginate fragments into pages.
- Emit layout diagnostics when diagnostics are enabled.

## Related Modules

- `Html2x.LayoutEngine.Style` owns AngleSharp usage, user agent stylesheet
  application, CSS parsing, computed style construction, style diagnostics, and
  the parser-free `StyleTree` handoff.
- `Html2x.LayoutEngine.Geometry` consumes `StyleTree` and produces published
  layout geometry.
- `Html2x.Renderers.Pdf` consumes `HtmlLayout` only.

## Primary Entry Points

- `LayoutBuilder`
- `IStyleTreeBuilder`
- `StyleTreeBuilder`
- `LayoutGeometryBuilder`
- `FragmentBuilder`
- `BoxToFragmentProjector`
- `BlockPaginator`

## Internal Boundaries

`LayoutBuilder` constructs the concrete pipeline stages for the converter flow.
The style stage is reached through `IStyleTreeBuilder`. The geometry stage is
reached through `LayoutGeometryBuilder`.

The layout engine should keep style, geometry, fragment, and pagination
responsibilities separable even when implementation classes coordinate multiple
steps.

When adding behavior:

1. Add style support first if the behavior comes from CSS.
2. Add geometry support if layout changes.
3. Add fragment fields only when renderers need the fact.
4. Add pagination translation support for new geometry-bearing fragment types.
5. Add diagnostics for unsupported or fallback behavior.

## Unsupported Modes

Unsupported future layout modes must remain explicit. Floats, absolute
positioning, and flexbox should emit diagnostics and use the documented fallback
until a real formatting context exists.
