#!/usr/bin/env bash
set -euo pipefail

INPUT_HTML=${1:-src/Tests/Html2x.TestConsole/html/example.html}
OUTPUT_PDF=${2:-build/diagnostics/example.pdf}
DIAGNOSTICS_JSON=${3:-build/diagnostics/session.json}

PROJECT="src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj"
ARGS=(
  "$INPUT_HTML"
  "$OUTPUT_PDF"
  --diagnostics
  --diagnostics-json "$DIAGNOSTICS_JSON"
)

json_dir=$(dirname "$DIAGNOSTICS_JSON")
mkdir -p "$json_dir"

dotnet run --project "$PROJECT" -- "${ARGS[@]}"
