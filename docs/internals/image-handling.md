# Image Handling

Html2x resolves and renders images through geometry-owned image metadata and renderer-owned drawing.

## Source Resolution

Image metadata paths are resolved relative to the configured HTML directory:

- `HtmlConverterOptions.Resources.BaseDirectory` is the single public owner.
- `HtmlConverter` maps it into layout geometry metadata resolution and PDF image byte loading.

Use absolute paths while debugging if working directory resolution is uncertain.

## Size Policy

Images have a maximum allowed size. Oversized images should be marked through diagnostics and handled deterministically.

Relevant options:

- `HtmlConverterOptions.Resources.MaxImageSizeBytes`

The facade maps this value into layout metadata checks. PDF rendering receives
resource context through renderer-owned `PdfRenderSettings`.

## Geometry Flow

Geometry resolves image metadata through `IImageMetadataResolver`. The metadata contract returns only source, status, and intrinsic size. It does not load render bytes.

Layout resolves image dimensions, padding, borders, and content rectangles from authored values and image metadata. Render model fragments carry those render facts forward. Pagination may translate image fragments and must preserve image content rectangles.

The renderer draws the image and border from render model fragment data. It loads image bytes through renderer-owned infrastructure and should not rederive layout geometry.

## Diagnostics

Image rendering uses `image/render`. Missing and oversized images are warnings. Successfully rendered images are informational.

Payloads should include status, rendered size, border metadata, source context, and raw image source when diagnostics are enabled.
