# Resources And Images

This page explains resource scope, image metadata, image byte loading, and image
diagnostics.

## Resource Scope

`HtmlConverterOptions.Resources.BaseDirectory` is the single public owner for
relative resource paths. When it is not set, `HtmlConverter` resolves it to
`AppContext.BaseDirectory`.

`Html2x.Resources` owns:

- Scoped path resolution.
- Data URI parsing.
- Byte size limit checks.
- Intrinsic image dimension decoding.
- Image byte loading.

Resource loading must not depend on the process current directory.

## Size Policy

Images have a maximum allowed size:

- Public converter option: `HtmlConverterOptions.Resources.MaxImageSizeBytes`.
- Internal renderer setting: `PdfRenderSettings.MaxImageSizeBytes`.

Invalid renderer settings fail before rendering. Oversized images are reported
through image status and diagnostics. File image length and reliably estimated
data URI decoded length are checked before full byte allocation; final image
decoder memory remains best-effort.

## Geometry Metadata

Geometry resolves image metadata through an internal image metadata seam. The
metadata contract returns only source, `ImageLoadStatus`, and intrinsic size.
Geometry uses that information to resolve image dimensions, content rectangle,
padding, borders, and fallback behavior.

During normal `HtmlConverter` conversions, metadata comes from a
conversion-scoped resource store. The store owns path scope, data URI parsing,
byte limits, status, retained bytes, and intrinsic dimension decoding for that
single conversion. It is not global cache state.

Geometry consumes image metadata through an internal resolver that accepts the
image source only. Base directory and byte-limit policy stay inside the
conversion-scoped resource store.

## Render Byte Loading

When the public converter drives rendering, the PDF renderer reads image bytes
from the same conversion-scoped resource store used for metadata. Successful
image bytes are retained only for that conversion and are bounded by
`MaxImageSizeBytes`.

Without a converter-provided resource store, internal renderer tests and
diagnostic paths can still load image bytes through `Html2x.Resources` using
renderer-owned resource settings. Rendering consumes fragment data and must not
rederive layout geometry.

## Status Vocabulary

`ImageLoadStatus` is the single resource-owned outcome vocabulary across
resources, metadata, published image facts, `ImageFragment`, and PDF
diagnostics. The type lives under `Html2x.RenderModel.Resources`, not fragment
tree building.

Known statuses:

- `Ok`
- `Missing`
- `Oversized`
- `InvalidDataUri`
- `DecodeFailed`
- `OutOfScope`

## Diagnostics

Image rendering uses `image/render`. Recoverable image failures are warnings.
Successful image rendering is informational.

Payloads should include status, rendered size, border metadata, source context,
and raw image source when diagnostics are enabled and configured to include raw
input.
