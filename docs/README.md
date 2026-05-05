# Html2x Developer Documentation

This directory contains developer-oriented documentation for maintaining and extending Html2x. It is organized by task: understand the system, work safely, inspect internals, extend behavior, and use reference material.

## Start Here

- [Getting Started](getting-started.md): prerequisites, build commands, and test commands.
- [Public API](reference/public-api.md): converter usage, options, diagnostics, and failure behavior.
- [Supported HTML And CSS](reference/supported-html-css.md): current behavior and explicit limitations.

## Architecture

- [Overview](architecture/overview.md): project map, architectural goals, and module responsibilities.
- [Processing Pipeline](architecture/pipeline.md): HTML input through PDF output.
- [Stage Ownership](architecture/stage-ownership.md): read/write boundaries between parser, style, layout, fragments, pagination, and paint.
- [Geometry](architecture/geometry.md): geometry authority, projection rules, and validation policy.
- [Diagnostics Architecture](architecture/diagnostics.md): diagnostics report flow and ownership.

## Development

- [Coding Standards](development/coding-standards.md): style, layering, logging, error handling, and review checks.
- [Testing](development/testing.md): unit, integration, renderer, diagnostics, and snapshot guidance.
- [Manual Test Console](development/manual-test-console.md): local PDF and diagnostics verification.
- [Troubleshooting](development/troubleshooting.md): common failures and recovery paths.

## Internals

- [Layout Engine](internals/layout-engine.md)
- [Pagination](internals/pagination.md)
- [Table Layout](internals/table-layout.md)
- [PDF Renderer](internals/pdf-renderer.md)
- [Fonts And Text Measurement](internals/fonts-and-text-measurement.md)
- [Image Handling](internals/image-handling.md)

## Extending

- [CSS Support](extending/css-support.md)
- [Renderers](extending/renderers.md)
- [Diagnostics](extending/diagnostics.md)

## Reference

- [Diagnostics Events](reference/diagnostics-events.md)

## Documentation Standards

- Use direct, developer-focused language.
- Keep durable docs focused on current behavior, contracts, and operating guidance.
- Prefer current contracts, supported behavior, failure modes, and test locations over historical narrative.
- Use ASCII punctuation and stable relative links.
