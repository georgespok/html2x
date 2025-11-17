# Data Model: Basic HTML-to-PDF Essentials

## TextRunFragment (existing, extended)
- **Fields**: `Text`, `Color`, `LineHeight`, `TextAlign`, `DisplayRole`.
- **Rules**: Color limited to RGB; `LineHeight` defaults to style cascade value; `TextAlign` flows from CSS cascade without mutating DOM.
- **Relations**: Linked to parent `LineBoxFragment` and inherits display role via `DisplayRoleMap`. Diagnostics reference runs via XPath-style DOM locations rather than fragment-specific IDs.

## LineBoxFragment (existing, behavior update)
- **Fields**: `Rect`, `BaselineY`, `LineHeight`, `Runs`, `Style`, `ZOrder`.
- **Rules**: Inline traversal MUST flush the current line and start a new `LineBoxFragment` whenever a `<br>` (or CSS break-after) element is encountered so each physical line is explicit in the fragment tree.
- **Relations**: Children of block fragments (e.g., paragraphs). Diagnostics count and enumerate these line boxes to verify `<br>` handling without parsing PDFs.

## ImageFragment (existing, extended)
- **Fields**: `SourceType` (File|DataUri), `WidthPx`, `HeightPx`, `IntrinsicRatio`, `MaxWidthPx`.
- **Rules**: Exactly one of width/height may be missing, derived via `IntrinsicRatio`; both must respect `MaxWidthPx` guard; `SourceType` determines validation routine.
- **Relations**: Belongs to a block-level fragment; diagnostics locate the fragment via DOM XPath plus fragment index to avoid embedding troubleshooting IDs.

## FragmentBorderMetadata (new helper record)
- **Fields**: `Top`, `Right`, `Bottom`, `Left` each capturing `Thickness`, `Color`, `Style`.
- **Rules**: Thickness clamped to <=20px; color limited to RGB; style limited to `solid|dashed|dotted` for MVP.
- **Relations**: Attached to block-level fragments, consumed by renderer visitors.

## DisplayRoleMap
- **Fields**: `TagName`, `Role` (Block|Inline|InlineBlock|ListItem), `FallbackRole`.
- **Rules**: Covers default HTML tags used in MVP samples (p, span, img, ul, li, div, strong, em, etc.); parser may override only through explicit CSS display values.
- **Relations**: Used during style-to-box conversion to ensure stage isolation.
