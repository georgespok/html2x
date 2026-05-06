# Supported HTML And CSS

Html2x supports a practical static subset of HTML and CSS for deterministic
business-report PDF generation. It is not a browser engine.

This page is the support contract for HTML tags, HTML attributes, and CSS
properties consumed by the style, box, geometry, fragment, and renderer
pipeline. AngleSharp may parse additional HTML and CSS syntax, but parsed syntax
outside this page is not part of the supported layout and rendering contract.

## Interpretation Rules

- Tag names, attribute names, CSS property names, and supported keyword values
  are matched case-insensitively.
- Normal documents can use the `html` wrapper. The rendered style tree starts at
  `body`; `html`, `head`, `style`, and metadata elements are not materialized as
  rendered nodes.
- Inline CSS and stylesheet CSS are parsed by AngleSharp. Html2x consumes only
  the supported CSS properties listed below.
- Unsupported elements are not materialized as boxes. Their text descendants are
  flattened into the nearest supported owner. A `br` inside unsupported content
  is preserved as a line break.
- Any CSS property not listed in this document is outside the supported contract.

## Supported HTML Tags

Exact renderable tag list:

`body`, `h1`, `h2`, `h3`, `h4`, `h5`, `h6`, `p`, `span`, `div`, `table`,
`tbody`, `thead`, `tfoot`, `tr`, `td`, `th`, `img`, `hr`, `br`, `ul`, `ol`,
`li`, `section`, `main`, `header`, `footer`, `b`, `i`, `strong`, `u`, `s`.

Supported roles:

| Role | Tags | Notes |
| --- | --- | --- |
| Render root | `body` | Owns page margin input and root block flow. |
| Block containers | `div`, `section`, `main`, `header`, `footer`, `p` | Participate in block flow. |
| Headings | `h1`, `h2`, `h3`, `h4`, `h5`, `h6` | Block flow with default heading font sizes, bold weight, and margins. |
| Inline text | `span`, `b`, `i`, `strong`, `u`, `s` | Participate in inline flow. Default styles apply bold, italic, underline, or line-through where appropriate. |
| Line break | `br` | Creates a forced line break in inline content. |
| Lists | `ul`, `ol`, `li` | Generates bullet markers for unordered lists and numeric markers for ordered lists. |
| Tables | `table`, `thead`, `tbody`, `tfoot`, `tr`, `td`, `th` | Supports rectangular tables without spans. `th` is treated as a header cell and is bold by default. |
| Images | `img` | Supports `src`, intrinsic image metadata, HTML size attributes, CSS dimensions, padding, and borders. |
| Horizontal rule | `hr` | Renders as a block rule with default top border and zero content height. |

## Supported HTML Attributes

| Attribute | Applies to | Support |
| --- | --- | --- |
| `id` | All supported tags | Used in source identity, diagnostics, and structural paths. |
| `class` | All supported tags | Used by CSS selector matching and source identity. |
| `style` | All supported tags | Inline declarations are parsed. Only supported CSS properties affect layout and rendering. |
| `src` | `img` | Image resource path used by image metadata and rendering. |
| `width` | `img` | Numeric CSS pixel value. CSS `width` overrides it for layout sizing. |
| `height` | `img` | Numeric CSS pixel value. CSS `height` overrides it for layout sizing. |
| `colspan` | `td`, `th` | Only missing value or value `1` is supported. Other values reject the table layout and emit diagnostics. |
| `rowspan` | `td`, `th` | Only missing value or value `1` is supported. Other values reject the table layout and emit diagnostics. |

Attributes outside this table may be retained in parsed element facts, but they
do not affect supported layout or rendering behavior.

## Supported CSS Values

### Lengths

Supported CSS length forms:

- `0`
- `<number>px`
- `<number>pt`

CSS pixels are converted to points using the repository CSS unit conversion
rules. Unsupported length units include `%`, `em`, `rem`, `in`, `cm`, `mm`,
`vh`, `vw`, `ex`, `ch`, and `pc`.

HTML `img` `width` and `height` attributes are numeric CSS pixels, not CSS
length declarations.

### Colors

Supported color forms for `color`, `background-color`, and border colors:

- Hex: `#rgb`, `#rgba`, `#rrggbb`, `#rrggbbaa`
- Functions: `rgb(r, g, b)`, `rgba(r, g, b, a)`
- RGB integer components from `0` to `255`
- RGB percentage components from `0%` to `100%`
- Alpha values from `0` to `1`
- `transparent`
- Named colors: `black`, `silver`, `gray`, `white`, `maroon`, `red`,
  `purple`, `fuchsia`, `green`, `lime`, `olive`, `yellow`, `navy`, `blue`,
  `teal`, `aqua`

## Supported CSS Properties

The properties below are the complete supported CSS property set. Any property
not listed here is unsupported, even if AngleSharp accepts it syntactically.

### Text And Font Properties

| Property | Supported values and behavior |
| --- | --- |
| `font-family` | Uses the first family name from the computed family list. Quotes are stripped. |
| `font-size` | Supported lengths only: `0`, `px`, and `pt`. |
| `font-weight` | `bold` and numeric weights `600` or greater produce bold text. |
| `font-style` | `italic` and `oblique` produce italic text. |
| `text-align` | `left`, `right`, `center`, `justify`. |
| `line-height` | Positive unitless multiplier, `normal`, or `inherit`. Length line heights are not supported. |
| `text-decoration` | `none`, `underline`, `line-through`, `overline`, or supported token combinations. |
| `color` | Supported color forms listed above. Invalid authored values inherit the fallback color. |
| `background-color` | Supported color forms listed above. Invalid authored values are ignored. |

### Display And Flow Properties

| Property | Supported values and behavior |
| --- | --- |
| `display` | `block`, `inline`, `inline-block`, `list-item`, `table`, `table-row-group`, `table-header-group`, `table-footer-group`, `table-row`, `table-cell`. |
| `float` | `left`, `right`, `none`. `left` and `right` are parsed and reported as unsupported layout modes. |
| `position` | `static`, `relative`, `absolute`. `absolute` is parsed and reported as an unsupported positioned layout mode. |

Notes:

- `display: flex` does not create a flex formatting context. It is reported as
  unsupported and falls back to the element default layout role.
- `display: none` is not a supported hiding behavior.
- CSS positioned offsets such as `top`, `right`, `bottom`, and `left` are not
  supported.
- CSS floats are not implemented as float layout. They are reported as
  unsupported layout modes.

### Spacing Properties

| Property | Supported values and behavior |
| --- | --- |
| `margin` | One to four supported length values. Negative values are applied and reported. |
| `margin-top` | Supported length value. Overrides `margin` for the top side when authored. |
| `margin-right` | Supported length value. Overrides `margin` for the right side when authored. |
| `margin-bottom` | Supported length value. Overrides `margin` for the bottom side when authored. |
| `margin-left` | Supported length value. Overrides `margin` for the left side when authored. |
| `padding` | One to four supported length values. Negative values are clamped to `0` and reported. |
| `padding-top` | Supported length value. Overrides `padding` for the top side when authored. Negative values are clamped to `0`. |
| `padding-right` | Supported length value. Overrides `padding` for the right side when authored. Negative values are clamped to `0`. |
| `padding-bottom` | Supported length value. Overrides `padding` for the bottom side when authored. Negative values are clamped to `0`. |
| `padding-left` | Supported length value. Overrides `padding` for the left side when authored. Negative values are clamped to `0`. |

### Dimension Properties

| Property | Supported values and behavior |
| --- | --- |
| `width` | Supported non-negative length value. Negative values are ignored and reported. |
| `min-width` | Supported non-negative length value. Negative values are ignored and reported. |
| `max-width` | Supported non-negative length value or `none`. Negative values are ignored and reported. |
| `height` | Supported non-negative length value. Negative values are ignored and reported. |
| `min-height` | Supported non-negative length value. Negative values are ignored and reported. |
| `max-height` | Supported non-negative length value or `none`. Negative values are ignored and reported. |

### Border Properties

Html2x consumes the computed side border model. The following authored border
properties are supported when AngleSharp expands them to side widths, styles,
and colors:

| Property | Supported values and behavior |
| --- | --- |
| `border` | Width, style, and optional color shorthand. |
| `border-top` | Top side width, style, and optional color shorthand. |
| `border-right` | Right side width, style, and optional color shorthand. |
| `border-bottom` | Bottom side width, style, and optional color shorthand. |
| `border-left` | Left side width, style, and optional color shorthand. |
| `border-width` | One to four supported length values through CSS shorthand expansion. |
| `border-style` | One to four supported style values through CSS shorthand expansion. |
| `border-color` | One to four supported color values through CSS shorthand expansion. |
| `border-top-width` | Supported length value. |
| `border-right-width` | Supported length value. |
| `border-bottom-width` | Supported length value. |
| `border-left-width` | Supported length value. |
| `border-top-style` | `none`, `solid`, `dashed`, `dotted`. |
| `border-right-style` | `none`, `solid`, `dashed`, `dotted`. |
| `border-bottom-style` | `none`, `solid`, `dashed`, `dotted`. |
| `border-left-style` | `none`, `solid`, `dashed`, `dotted`. |
| `border-top-color` | Supported color forms listed above. |
| `border-right-color` | Supported color forms listed above. |
| `border-bottom-color` | Supported color forms listed above. |
| `border-left-color` | Supported color forms listed above. |

Unsupported border styles such as `double`, `groove`, `ridge`, `inset`, and
`outset` are ignored and reported when authored as side styles.

## Table Support

Supported:

- `table` with direct `tr` children.
- `table` with direct `thead`, `tbody`, or `tfoot` children containing direct
  `tr` children.
- `tr` with direct `td` or `th` children.
- Rectangular table structures without spans.
- Equal-width column derivation when explicit column widths are absent.
- Header cell identity for `th`.
- Row and cell backgrounds.
- Cell borders and padding.

Unsupported:

- `colspan` values other than missing or `1`.
- `rowspan` values other than missing or `1`.
- Nested table sections.
- Mixing direct rows and direct sections under the same table.
- Generic unsupported children directly under `table`, table section, or `tr`.
- Complex browser table layout behavior.

## Lists

Supported:

- `ul` and `ol` as block list containers.
- `li` as list items.
- Generated bullet markers for `ul`.
- Generated one-based numeric markers for `ol`.

Unsupported:

- `list-style`, `list-style-type`, `list-style-position`, `start`, and `type`.
- Custom marker content.

## Images

Supported:

- `img src`.
- Intrinsic dimensions from the configured image metadata resolver.
- HTML `width` and `height` attributes as CSS pixels.
- CSS `width`, `height`, `min-width`, `max-width`, `min-height`, and
  `max-height`.
- CSS `padding` and borders around images.
- Aspect-ratio preservation when only one dimension is known and intrinsic size
  is available.
- Scaling down image content when it exceeds available width.

Unsupported:

- CSS `object-fit`, `object-position`, `aspect-ratio`, filters, transforms, and
  image backgrounds.

## Unsupported Or Partial CSS

The following are intentionally outside the supported contract unless listed
above:

- Flexbox and grid layout.
- Floats as actual float layout.
- Absolute positioning as positioned layout.
- CSS transforms, filters, opacity, shadows, clipping, overflow, and z-index.
- Background images and gradients.
- Media queries and scripting.
- CSS custom properties.
- Percentage, viewport, font-relative, and physical length units other than
  `px` and `pt`.

Unsupported or ignored declarations should emit diagnostics when diagnostics are
enabled and the declaration is authored in a supported diagnostic path.

## Determinism Notes

Determinism depends on using the same HTML, options, fonts, image files,
operating system font behavior, and dependency versions. Use repository test
fonts for stable test scenarios.
