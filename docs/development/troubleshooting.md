# Troubleshooting

This page lists common local failures and the fastest recovery path.

## `HtmlConverterOptions.Fonts.FontPath` Does Not Exist

Cause: the converter requires a font file or directory before layout begins.

Recovery:

1. Use an absolute font path when debugging.
2. For test console runs, point to `src/Tests/Html2x.TestConsole/fonts`.
3. Ensure the path exists from the process working directory.

## Text Missing Or Overlapping After Page Breaks

Cause candidates:

- Fragment translation did not offset child fragments.
- Pagination moved a parent block without preserving nested line or text coordinates.
- A new fragment type lacks translation support.

Recovery:

1. Enable diagnostics JSON.
2. Inspect `layout.snapshot` and `layout/geometry-snapshot`.
3. Compare parent fragment bounds with nested line and text origins.
4. Add or fix the fragment translator registration.

## Page Count Drift

Cause candidates:

- Nondeterministic ordering before pagination.
- Mutation of source fragments during pagination.
- Platform-dependent font measurement.

Recovery:

1. Re-run the focused pagination tests.
2. Compare placement tuples containing fragment id, page number, page Y, and order index.
3. Verify font source and font path are identical between runs.

## Diagnostics JSON Missing Fields

Cause candidates:

- The emitter did not include the field in `DiagnosticFields`.
- The field value uses a type outside the constrained diagnostics value model.

Recovery:

1. Confirm the emitter adds the expected field to `DiagnosticFields`.
2. Use strings, numbers, booleans, enum names as strings, nulls, diagnostic arrays, or nested diagnostic objects.
3. Add serializer tests for important fields and nested diagnostic structures.

## Unsupported CSS Produces Unexpected Output

Cause: unsupported layout modes use documented fallbacks and diagnostics.

Recovery:

1. Check [Supported HTML And CSS](../reference/supported-html-css.md).
2. Enable diagnostics and look for `style/*` or `layout/unsupported-mode` events.
3. Add a baseline test if the fallback is intentional and not already covered.
