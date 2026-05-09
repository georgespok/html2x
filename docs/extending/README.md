# Extending Html2x

This directory describes supported extension paths for developers changing the
codebase. Choose the path by the behavior being added.

## Extension Paths

| Goal | Start here |
| --- | --- |
| Add a CSS property, supported value, selector behavior, or fallback | [CSS Support](css-support.md) |
| Add an observable diagnostic record | [Diagnostics](diagnostics.md) |
| Add or change renderer behavior | [Renderers](renderers.md) |
| Add a new render model fragment kind | [Fragment Kinds](fragment-kinds.md) |

## Rule Of Ownership

Add behavior where the owning stage already makes the decision. If a later
stage needs new data, publish that data through the previous stage output
instead of adding a backward reference.

Use [Module Boundaries](../architecture/module-boundaries.md) and
[Contracts And Invariants](../architecture/contracts-and-invariants.md) before
changing cross-stage contracts.
