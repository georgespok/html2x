# Failure Modes

This page summarizes exceptions, recoverable fallbacks, diagnostics
availability, and cancellation behavior.

## Option Failures

Invalid required options can fail before layout begins. With the default
converter runtime, the most common case is a missing or invalid
`HtmlConverterOptions.Fonts.FontPath`, which throws `InvalidOperationException`.
Advanced runtime font or text adapters can remove that default font path
requirement.

Direct renderer calls validate `PdfRenderSettings`. Invalid image byte limits
throw before rendering begins.

## Font Failures

Font path and renderer font failures use `FontResolutionException` where
available. Missing `TextRun.ResolvedFont` during direct renderer usage fails as
a renderer input error.

## Resource And Image Failures

Image failures are usually recoverable. Missing, oversized, invalid data URI,
decode failure, and out-of-scope images produce `ImageLoadStatus` outcomes and
`image/render` diagnostics when diagnostics are enabled.

## Unsupported Input

Unsupported CSS or structures should use deterministic fallback behavior when
possible and emit diagnostics when diagnostics are enabled. Examples include
unsupported CSS declarations, floats as actual float layout, positioned layout,
flexbox, unsupported table structures, and unsupported image features.

## Contract Violations

Contract violations should fail close to the stage that introduced invalid
state. Examples include non-finite published geometry, negative renderable
sizes, or missing required renderer facts.

## Cancellation

`HtmlConverter.ToPdfAsync` accepts a `CancellationToken`. If cancellation is
requested, the active stage emits cancellation lifecycle diagnostics when
diagnostics are enabled, and downstream stages are skipped when they have not
started.

## Diagnostics Availability

When diagnostics are enabled, successful conversions return
`Html2PdfResult.DiagnosticsReport`. If conversion fails after diagnostics
collection starts, the exception may carry the report in
`Exception.Data["DiagnosticsReport"]`.
