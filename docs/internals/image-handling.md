# Image Handling

Html2x resolves and renders images through layout-owned image metadata and renderer-owned drawing.

## Source Resolution

Image paths are resolved relative to the configured HTML directory:

- `LayoutOptions.HtmlDirectory` for layout image resolution.
- `PdfOptions.HtmlDirectory` for PDF option context.

Use absolute paths while debugging if working directory resolution is uncertain.

## Size Policy

Images have a maximum allowed size. Oversized images should be marked through diagnostics and handled deterministically.

Relevant options:

- `LayoutOptions.MaxImageSizeBytes`
- `PdfOptions.MaxImageSizeMb`
- `PdfOptions.MaxImageSizeBytes`

## Geometry Flow

Layout resolves image dimensions, padding, borders, and content rectangles. Fragments carry those render facts forward. Pagination may translate image fragments and must preserve image content rectangles.

The renderer draws the image and border from fragment data. It should not rederive layout geometry.

## Diagnostics

Image rendering uses `image/render`. Missing and oversized images are warnings. Successfully rendered images are informational.

Payloads should include status, rendered size, border metadata, source context, and raw image source when diagnostics are enabled.
