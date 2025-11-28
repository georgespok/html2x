# Data Model

## Entities

### ImageSource
- Fields: src (string), scheme (enum: data, file), intrinsicWidthPx (int), intrinsicHeightPx (int), status (enum: loaded, missing, unsupported, corrupted), byteLength (int?)
- Relationships: linked to one RenderedImageBox.
- Validation: allow only file paths or data URIs; reject others.
- Constraints: if byteLength > 10 MB or pixels > 10 MP, mark oversize and require downscale before use.

### RenderedImageBox
- Fields: widthPx (double), heightPx (double), scaleFactor (double), baselineOffsetPx (double), placeholderShown (bool), diagnosticsId (string)
- Relationships: references one ImageSource.
- Validation: preserve aspect ratio unless both width and height supplied; cap by container/page bounds.
- State: derived size after applying explicit attributes, intrinsic ratio, density,.

## Derived Rules
- If only width is provided, height = width * intrinsicHeight / intrinsicWidth.
- If only height is provided, width = height * intrinsicWidth / intrinsicHeight.
- If both provided, respect width and adjust height to maintain aspect ratio (no distortion).
- Reject when pixel count > 10 MP or byteLength > 10 MB (configurable cap); aspect ratio rules still inform expected size.
- PlaceholderShown = true when status is missing, unsupported, or corrupted.
