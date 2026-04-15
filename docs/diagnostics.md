# Html2x Diagnostics

This document is the working vocabulary for Html2x diagnostics. It will be expanded as the diagnostics gap feature is implemented.

## Purpose

Diagnostics are developer-facing troubleshooting artifacts. They explain conversion decisions, unsupported input, layout behavior, rendering decisions, and serializer output without requiring a debugger.

## Canonical Severity

Diagnostics will use these severity levels:

- `Info`: expected limitation or useful trace detail.
- `Warning`: recoverable issue that may affect output.
- `Error`: conversion-blocking or stage-failing issue.

## Stage Lifecycle

Stage diagnostics will use these lifecycle states:

- `Started`
- `Succeeded`
- `Failed`
- `Skipped`

Duration and performance fields are out of scope for this feature.

## Diagnostic Context

Emitters should include context that helps locate the affected input or output when available:

- Selector or selector-like context.
- Element identity, such as tag, role, id, or class.
- Style declaration or raw style value.
- Structural path through DOM, display tree, fragment tree, table, or stage.
- Subsystem or stage identity.

Missing context fields should not make a diagnostic unreadable.

## Raw User Input

Raw user input, including raw document text, may appear in diagnostics when diagnostics are enabled. Diagnostics are an explicit troubleshooting artifact, and raw input can be necessary to reproduce parser, style, layout, and serialization issues.

Structured context remains required when available so raw input is not the only way to locate a problem.

## Payload Compatibility

Existing diagnostic names and payload shapes may be renamed or restructured when the replacement preserves equivalent troubleshooting value.

Payload contracts that cross project boundaries belong in `Html2x.Abstractions`. Serialization and export behavior belong in `Html2x.Diagnostics`.

## Diagnostic Export And Snapshots

Diagnostics JSON preserves canonical event fields for type, name, description, timestamp, severity, stage state, context, raw user input, and known payload fields. Unknown payloads still export their `kind` so forward-compatible producers can be identified.

Layout snapshots now carry visual verification fields when the fragment has them available:

- Resolved text color and background color.
- Margin, padding, border, width, height, and display.
- Existing fragment metadata such as display role, formatting context, table row index, cell column index, and header status.

Table diagnostics use `layout/table` for supported and unsupported table layout decisions. The payload keeps the existing summary fields and adds row, cell, column, and group context collections when available. Unsupported table structures also emit `layout/table/unsupported-structure`.

Pagination diagnostics use `layout/pagination/*` event names and include matching payload `EventName`, canonical severity, and diagnostic context. Oversized blocks are warnings; ordinary page creation, placement, movement, and empty-document traces are informational.

Image diagnostics use the canonical `image/render` event name. The event and payload include severity, status, rendered size, border metadata, source context, and raw image source input. Missing and oversize images are warnings; successfully rendered images are informational.

Migration note: `image/render` replaces the previous `ImageRender` event name. Diagnostics producers should not emit both names unless a consumer compatibility requirement is explicitly added.

## Style Diagnostics

Style diagnostics explain why a CSS declaration was applied, ignored, partially applied, or treated as unsupported. They use canonical severity `Warning` because the converter can continue while the affected visual output may differ from the source template.

Common event names:

- `style/unsupported-declaration`: the declaration uses a known property with an unsupported value or unit.
- `style/ignored-declaration`: the declaration is invalid and is not used.
- `style/partially-applied-declaration`: part of the declaration is preserved while an unsafe or unsupported part is adjusted.

Unsupported declaration example:

```csharp
new StyleDiagnosticPayload
{
    PropertyName = "width",
    RawValue = "10rem",
    Decision = "Unsupported",
    Reason = "Unsupported unit 'rem' for width.",
    Context = new DiagnosticContext(
        Selector: "#invoice",
        ElementIdentity: "section#invoice",
        StyleDeclaration: "width: 10rem",
        StructuralPath: "html/body/section#invoice",
        RawUserInput: "width: 10rem; padding: 8px;")
};
```

Ignored declaration example:

```csharp
new StyleDiagnosticPayload
{
    PropertyName = "padding",
    RawValue = "1px 2px 3px 4px 5px",
    Decision = "Ignored",
    Reason = "Invalid padding shorthand: expected 1 to 4 values.",
    Context = new DiagnosticContext(
        Selector: "#invoice",
        ElementIdentity: "section#invoice",
        StyleDeclaration: "padding: 1px 2px 3px 4px 5px",
        StructuralPath: "html/body/section#invoice",
        RawUserInput: "padding: 1px 2px 3px 4px 5px;")
};
```

Partially applied declaration example:

```csharp
new StyleDiagnosticPayload
{
    PropertyName = "padding-top",
    RawValue = "-4px",
    NormalizedValue = "0",
    Decision = "PartiallyApplied",
    Reason = "Negative padding is clamped to zero.",
    Context = new DiagnosticContext(
        Selector: ".summary",
        ElementIdentity: "div.summary",
        StyleDeclaration: "padding-top: -4px",
        StructuralPath: "html/body/div.summary",
        RawUserInput: "padding-top: -4px;")
};
```

## Code Sketch

```csharp
public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public enum DiagnosticStageState
{
    Started,
    Succeeded,
    Failed,
    Skipped
}

public sealed record DiagnosticContext(
    string? Selector,
    string? ElementIdentity,
    string? StyleDeclaration,
    string? StructuralPath,
    string? RawUserInput);
```

Example event shape:

```csharp
var diagnostic = new DiagnosticsEvent
{
    Name = "stage/style",
    Severity = DiagnosticSeverity.Info,
    StageState = DiagnosticStageState.Started,
    Context = new DiagnosticContext(
        Selector: ".invoice-total",
        ElementIdentity: "p#total",
        StyleDeclaration: "width: 120qu",
        StructuralPath: "html/body/p[3]",
        RawUserInput: "<p id=\"total\">$42</p>")
};
```

## Current Work In Progress

The diagnostics gap feature tracks the active inventory and implementation plan in:

- `C:\Projects\html2x\specs\015-diagnostics-gap\diagnostics-inventory.md`
- `C:\Projects\html2x\specs\015-diagnostics-gap\plan.md`
- `C:\Projects\html2x\specs\015-diagnostics-gap\tasks.md`
