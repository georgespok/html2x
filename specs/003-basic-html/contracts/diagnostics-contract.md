# Contracts: Diagnostics Payload Schema

Html2x emits diagnostics snapshots during layout and rendering. The contracts below define the in-process structures consumed by tests and tooling.

## DiagnosticsSnapshot
- `SequenceStart` (int) – initial counter value, defaults to 0 per render.
- `Fragments` (IReadOnlyList<FragmentDiagnostics>) – ordered entries describing text runs, images, and containers.
- `Borders` (IReadOnlyList<BorderDiagnostics>) – optional per-side definitions attached to block fragments.
- `Warnings` (IReadOnlyList<string>) – unsupported CSS, missing assets, or clamped values.

## FragmentDiagnostics
- `FragmentIndex` (int)
- `Type` (`Text`, `Image`, `ListItem`, `Block`)
- `DisplayRole` (Block|Inline|InlineBlock|ListItem)
- `LineIndex` (int) – positional hint derived from the surrounding `LineBoxFragment`
- `Color`, `LineHeight`, `TextAlign` (for text)
- `WidthPx`, `HeightPx`, `SourceType`, `MaxWidthPx` (for images)

## BorderDiagnostics
- `FragmentIndex`
- `Top|Right|Bottom|Left` each capturing `{ ThicknessPx, ColorHex, Style }`

## Serialization
Diagnostics snapshots serialize to JSON for the TestConsole and integration tests:
```
{
  "sequenceStart": 0,
  "fragments": [ { "fragmentIndex": 0, "type": "text", "lineIndex": 0, ... } ],
  "borders": [ { "fragmentIndex": 0, "top": { "thicknessPx": 2, "colorHex": "#000000" } } ],
  "warnings": [ "Image max-width clamp applied" ]
}
```

Consumers should treat this schema as internal but stable for the MVP to keep tests deterministic and tooling aligned.
