# Layout Engine

`Html2x.LayoutEngine` owns the conversion from parsed HTML and CSS to `HtmlLayout` pages.

## Responsibilities

- Parse HTML and CSS through AngleSharp wrappers.
- Compute style values with inheritance, defaults, and supported unit conversion.
- Build display and box trees.
- Resolve block, inline, image, list, rule, and table layout.
- Project boxes into fragments.
- Paginate fragments into pages.
- Emit layout diagnostics when diagnostics are enabled.

## Primary Entry Points

- `LayoutBuilder`
- `LayoutBuilderFactory`
- `AngleSharpDomProvider`
- `CssStyleComputer`
- `BoxTreeBuilder`
- `FragmentBuilder`
- `BlockPaginator`

## Internal Boundaries

The layout engine should keep parser, style, geometry, fragment, and pagination responsibilities separable even when some implementation classes currently coordinate multiple steps.

When adding behavior:

1. Add style support first if the behavior comes from CSS.
2. Add display or box model support if layout changes.
3. Add fragment fields only when renderers need the fact.
4. Add pagination translation support for new geometry-bearing fragment types.
5. Add diagnostics for unsupported or fallback behavior.

## Unsupported Modes

Unsupported future layout modes must remain explicit. Floats, absolute positioning, and flexbox should emit diagnostics and use the documented fallback until a real formatting context exists.
