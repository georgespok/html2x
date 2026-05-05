# Image Handling

Html2x resolves and renders images through a shared resource module, geometry-owned image metadata, and renderer-owned drawing.

## Source Resolution

Image paths are resolved relative to the configured HTML directory:

- `HtmlConverterOptions.Resources.BaseDirectory` is the single public owner.
- When no base directory is supplied, `HtmlConverter` uses `AppContext.BaseDirectory`.
- `HtmlConverter` maps the resolved directory into layout geometry metadata resolution and PDF image byte loading.
- `Html2x.Resources` owns scoped path resolution and data URI parsing.

Use an explicit base directory while debugging. Resource loading must not depend on the process current directory.

## Size Policy

Images have a maximum allowed size. Oversized images should be marked through diagnostics and handled deterministically.

Relevant options:

- `HtmlConverterOptions.Resources.MaxImageSizeBytes`

The facade maps this value into layout metadata checks and PDF rendering.
Direct PDF renderer usage can pass the same value through renderer-owned
`PdfRenderSettings`. `PdfRenderer` throws before rendering when
`PdfRenderSettings.MaxImageSizeBytes` is less than or equal to zero.

## Geometry Flow

Geometry resolves image metadata through the internal image metadata seam. The
metadata contract returns only source, `ImageLoadStatus`, and intrinsic size.
The shared resource module decodes intrinsic dimensions from the same file and
data URI policy used for render byte loading.

Layout resolves image dimensions, padding, borders, and content rectangles from authored values and image metadata. Render model fragments carry those render facts and the image load status forward. Pagination may translate image fragments and must preserve image content rectangles and status.

`ImageLoadStatus` is the single outcome vocabulary for resources, geometry
metadata, published image facts, `ImageFragment`, and PDF diagnostics.
`IsMissing` and `IsOversize` are derived from that status rather than stored as
independent facts.

The renderer draws the image and border from render model fragment data. It loads image bytes through `Html2x.Resources` and should not rederive layout geometry.

## Diagnostics

Image rendering uses `image/render`. Missing, oversized, invalid data URI, decode failure, and out-of-scope images are warnings. Successfully rendered images are informational.

Payloads should include status, rendered size, border metadata, source context, and raw image source when diagnostics are enabled.
