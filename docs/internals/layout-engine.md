# Layout Engine

`Html2x.LayoutEngine` owns the conversion from parsed HTML and CSS to `HtmlLayout` pages.

## Responsibilities

- Parse HTML and CSS through AngleSharp wrappers.
- Compute style values with inheritance, defaults, and supported unit conversion.
- Build style and box trees.
- Resolve block, inline, image, list, rule, and table layout.
- Project boxes into fragments.
- Paginate fragments into pages.
- Emit layout diagnostics when diagnostics are enabled.

## Primary Entry Points

- `LayoutBuilder`
- `AngleSharpDomProvider`
- `CssStyleComputer`
- `BoxTreeBuilder`
- `InitialBoxTreeBuilder`
- `BlockLayoutEngine`
- `InlineLayoutEngine`
- `TableLayoutEngine`
- `FragmentBuilder`
- `BoxToFragmentProjector`
- `BlockPaginator`

## Internal Boundaries

`LayoutBuilder` constructs the concrete pipeline stages for the converter flow. The current structure favors direct internal collaborators over separate interfaces for the main layout stages.

`BoxTreeBuilder` coordinates the initial box pass and layout geometry pass. `InitialBoxTreeBuilder` materializes box roles from the style tree, then the block, inline, image, and table layout services publish `UsedGeometry` and layout metadata.

The layout engine should keep parser, style, box construction, geometry, fragment, and pagination responsibilities separable even when some implementation classes coordinate multiple steps.

When adding behavior:

1. Add style support first if the behavior comes from CSS.
2. Add box role or box model support if layout changes.
3. Add fragment fields only when renderers need the fact.
4. Add pagination translation support for new geometry-bearing fragment types.
5. Add diagnostics for unsupported or fallback behavior.

## Unsupported Modes

Unsupported future layout modes must remain explicit. Floats, absolute positioning, and flexbox should emit diagnostics and use the documented fallback until a real formatting context exists.
