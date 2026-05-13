# Html2x Developer Documentation

This directory is the durable developer documentation set for Html2x. It
documents current behavior, public contracts, architecture, internals, extension
paths, diagnostics, and reproducible build/test commands.

Local planning, validation logs, review notes, and workflow instructions live
under `.work/`, not under `docs/`.

## Start

- [Getting Started](getting-started.md): shortest path from checkout to verified
  local build.
- [Build, Run, And Test](development/build-run-test.md): command reference for
  restore, build, tests, and manual rendering.
- [Troubleshooting](development/troubleshooting.md): common failures and
  recovery paths.

## Understand The System

- [Core Concepts](concepts/core-concepts.md): shared model for stages, handoff
  facts, geometry, fragments, pagination, diagnostics, resources, and text.
- [Glossary](concepts/glossary.md): canonical terminology and capitalization.
- [Source Identity](concepts/source-identity.md): source identity, generated
  identity, layout identity, and diagnostics fields.
- [Architecture Overview](architecture/overview.md): project map and primary
  data flow.
- [Processing Pipeline](architecture/pipeline.md): HTML/CSS input through PDF
  bytes.
- [Module Boundaries](architecture/module-boundaries.md): project ownership and
  dependency direction.
- [Contracts And Invariants](architecture/contracts-and-invariants.md): handoff
  contracts, mutation policy, and validation rules.
- [Diagnostics Architecture](architecture/diagnostics.md): diagnostics
  dependency direction, runtime flow, and ownership.

## Inspect Internals

- [Layout Engine](internals/layout-engine.md): composition layer and stage
  orchestration.
- [Style Engine](internals/style-engine.md): HTML/CSS parsing and `StyleTree`
  construction.
- [Geometry](internals/geometry.md): geometry authority, `UsedGeometry`,
  publishing, and layout invariants.
- [Fragment Tree Building](internals/fragment-tree-building.md): published layout to
  render model fragments.
- [Pagination](internals/pagination.md): page placement and pagination audit
  facts.
- [Table Layout](internals/table-layout.md): table structure, grid derivation,
  placement, diagnostics, and rendering interaction.
- [Resources And Images](internals/resources-and-images.md): resource scope,
  image metadata, byte loading, statuses, and diagnostics.
- [Text And Fonts](internals/text-and-fonts.md): font source, text
  measurement, resolved font facts, and renderer requirements.
- [PDF Renderer](internals/pdf-renderer.md): paint ordering, settings, fonts,
  images, and diagnostics.

## Extend Behavior

- [Extending Overview](extending/README.md): choose the right extension path.
- [CSS Support](extending/css-support.md): adding a property, value, selector
  behavior, or fallback.
- [Diagnostics](extending/diagnostics.md): adding observable diagnostics without
  leaking producer-local models.
- [Renderers](extending/renderers.md): adding or changing renderer behavior.
- [Fragment Kinds](extending/fragment-kinds.md): adding a new render model
  fragment kind.

## Reference Contracts

- [Public API](reference/public-api.md): converter entry point, result type,
  diagnostics, and public surface classification.
- [Options](reference/options.md): public option groups, defaults, validation,
  and mapping to internal settings.
- [Supported HTML And CSS](reference/supported-html-css.md): supported input
  contract and explicit limitations.
- [Diagnostics Events](reference/diagnostics-events.md): event families,
  schema shape, and sample JSON.
- [Failure Modes](reference/failure-modes.md): exceptions, diagnostics
  availability, cancellation, and recoverable fallbacks.

## Documentation Rules

- Use direct, developer-focused language.
- Document current behavior, contracts, and operating guidance.
- Keep local workflow and transient implementation notes under `.work/`.
- Use ASCII punctuation and stable relative links.
