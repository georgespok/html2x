# Diagnostic Trace Plan

TR001 – Rename `LayoutDiagnosticsDumper` and related assets to the “Trace” terminology (e.g., `LayoutTraceBuilder`, category `trace/layout`) and verify no tooling depends on the old identifiers.

TR002 – Update the structured diagnostics contract naming (e.g., `StructuredTraceDocument`, `StructuredTraceNode`) or provide aliases so trace terminology is reflected end-to-end without breaking serialization.

TR003 – Implement `DomTraceBuilder` that walks the AngleSharp DOM, records key identity/attribute data, enforces node-count limits, and publish the trace immediately after `_domProvider.LoadAsync`.

TR004 – Implement `StyleTraceBuilder` for the computed style tree, capturing layout/typography properties and CSS origin metadata while guarding against shared-node cycles; publish right after `_styleComputer.Compute`.

TR005 – Modernize the existing Box trace to the new naming, include pagination context (page margins/topology), and preserve current summary metrics for backward compatibility.

TR006 – Create `FragmentTraceBuilder` that summarizes inline measurement outputs (runs, glyph counts, overflow flags) with text truncation controls; publish after `_fragmentBuilder.Build`.

TR007 – Emit a (currently minimal) fragmentation trace record so the stage remains observable once it diverges from inline measurement; document the placeholder schema.

TR008 – Produce a pagination trace describing final `HtmlLayout` pages, margins, and block mappings to close the pipeline observability loop.

TR009 – Introduce configuration toggles (e.g., per-stage enablement on `IDiagnosticSession.Descriptor`) and sampling safeguards to prevent oversized trace payloads in batch workloads.

TR010 – Extend automated tests to assert trace publication order/content and update `docs/diagnostics.md` (or equivalent) with the new trace categories, payload schemas, and viewer guidance.
