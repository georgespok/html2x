# Data Model

## Entities

### ImageSource
- Fields: src (string), scheme (enum: data, file), intrinsicWidthPx (int), intrinsicHeightPx (int), status (enum: loaded, missing, unsupported, corrupted), byteLength (int?)
- Relationships: linked to one RenderedImageBox.
- Validation: allow only file paths or data URIs; reject others (provider enforces scope).
- Constraints: if byteLength > 10 MB, mark oversize during layout/provider; renderer trusts the status and draws placeholder.

### RenderedImageBox
- Fields: widthPx (double), heightPx (double), scaleFactor (double), baselineOffsetPx (double), placeholderShown (bool), diagnosticsId (string)
- Relationships: references one ImageSource.
- Validation: preserve aspect ratio unless both width and height supplied; cap by container/page bounds.
- State: derived size after applying explicit attributes, intrinsic ratio, density,.

## Derived Rules
- If only width is provided, height = width * intrinsicHeight / intrinsicWidth.
- If only height is provided, width = height * intrinsicWidth / intrinsicHeight.
- If both provided, respect width and adjust height to maintain aspect ratio (no distortion).
- Reject when byteLength > 10 MB (configurable cap) in the provider/layout stage; aspect ratio rules still inform expected size for placeholders.
- PlaceholderShown = true when status is missing, unsupported, or corrupted.
