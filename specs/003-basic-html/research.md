# Research: Basic HTML-to-PDF Essentials

## Decision 1: Image sources limited to disk paths + data URIs
- **Rationale**: Keeps MVP deterministic, avoids network/security concerns, and matches current TestConsole capabilities.
- **Alternatives considered**: Allowing HTTP(S) downloads (rejected due to sandbox complexity); embedding base64-only images (rejected because many scenarios rely on file references).

## Decision 2: Diagnostics-first verification (no PDF parsing)
- **Rationale**: Constitution mandates diagnostics coverage; fragment metadata already exposes text, image, and border attributes needed for tests.
- **Alternatives considered**: Parsing PDF binary output (rejected as costly and flaky); adding golden PDF snapshots (rejected due to cross-platform noise).

## Decision 3: Single-page scope without advanced formatting contexts
- **Rationale**: User explicitly deferred multi-page logic; keeping scope single-page reduces risk while delivering essential HTML features.
- **Alternatives considered**: Implement pagination alongside MVP (rejected because it would delay delivery and obscure diagnostics focus).

## Decision 4: Default display role table sourced from HTML spec
- **Rationale**: Provides predictable behavior without bespoke overrides and aligns with Stage Layout Discipline by centralizing role mappings.
- **Alternatives considered**: Hard-code only the handful of tags in sample docs (rejected because it would regress when new tags appear); rely on AngleSharp defaults (rejected because it exposes DOM internals to later stages).
- 2025-11-17: Verified stage isolation via dotnet msbuild src/Html2x.sln /t:ProjectReferenceGraph. See build/dependency-graph.txt for details.

## Reflection 2025-11-19: Inline/Text Deduplication
- Consolidated padding and alignment calculations in `InlineFragmentStage` by threading the owning `BlockBox` through recursion instead of discovering it per inline node. This removed repeated parent lookups and keeps line-fragment math anchored to a single context for each block.
- Introduced a reusable `RenderRowWithOffset` helper inside `QuestPdfFragmentRenderer` so both line and block children share the same left-offset logic. This keeps bullet/list handling aligned with general block rendering, avoiding drift when we adjust offset math later.
- Outcome: Text-focused diagnostics stay deterministic, renderer logic remains isolated to abstractions, and we eliminated duplicated padding + row-offset branches without changing observable behavior.
