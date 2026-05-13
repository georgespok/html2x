# Style Engine

This page explains how `Html2x.LayoutEngine.Style` turns HTML and CSS into the
parser-free `StyleTree` consumed by geometry.

## Responsibilities

- Parse raw HTML with AngleSharp.
- Apply the default user agent stylesheet when enabled.
- Parse stylesheet and inline CSS declarations.
- Compute supported CSS values.
- Traverse supported HTML elements.
- Flatten unsupported parser elements while preserving supported descendants.
- Assign style source identity and style content identity.
- Emit style diagnostics for unsupported, ignored, or partially applied
  declarations.

## Input And Output

Input:

- Raw HTML.
- `StyleBuildSettings`.
- Optional `IDiagnosticsSink`.

Output:

- `StyleTree` under `Html2x.LayoutEngine.Contracts.Style`.

`StyleTree` is the only style-stage output consumed by geometry. Geometry must
not reference AngleSharp DOM objects, CSSOM objects, selectors, or parser child
collections.

## Supported Element Traversal

The style stage starts from the rendered body root. `html`, `head`, `style`,
and metadata elements are parser inputs, but they are not rendered layout nodes.

Unsupported elements are not materialized as boxes. Their supported text or
element descendants are flattened into the nearest supported owner. A `br`
inside unsupported content remains a line break.

## Computed Style

The style stage owns CSS interpretation before values enter layout contracts:

- CSS length parsing for supported units.
- CSS color parsing before colors become render model `ColorRgba` facts.
- Initial and inherited values.
- User agent defaults for supported HTML elements.
- Unsupported declaration diagnostics.

Layout stages consume computed facts. They do not reparse authored CSS.

## Source Identity

`StyleTraversal` assigns source identity while parser context is available.
`StyleSourceIdentity` identifies styled elements and `StyleContentIdentity`
identifies ordered text, element, and line break content. Later stages copy or
generate identity from those facts rather than rebuilding source paths.

## Diagnostics

Style diagnostics use these event families:

- `style/unsupported-declaration`
- `style/ignored-declaration`
- `style/partially-applied-declaration`

Diagnostics should identify the declaration, supported fallback, source
context, and severity when available.

Authored CSS declaration lookup is centralized in Style-owned code. Mapper and
diagnostic paths use parser-backed inline style declarations instead of
manually splitting inline style text. Style keeps a narrow raw declaration
recovery path only for invalid known declarations that the parser drops, so
diagnostics can still report the authored property and value without changing
event names or field names.
