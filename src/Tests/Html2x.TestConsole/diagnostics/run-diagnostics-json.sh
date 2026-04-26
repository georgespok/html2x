#!/usr/bin/env bash
set -euo pipefail

INPUT_HTML=${1:-src/Tests/Html2x.TestConsole/html/centralize-layout-font-policy.html}
OUTPUT_PDF=${2:-build/centralize-layout-font-policy.pdf}
DIAGNOSTICS_JSON=${3:-build/diagnostics/centralize-layout-font-policy.json}

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
REPO_ROOT=$(cd "$SCRIPT_DIR/../../../.." && pwd)

resolve_path() {
  local candidate=$1
  case "$candidate" in
    /*) printf '%s\n' "$candidate" ;;
    *) printf '%s\n' "$REPO_ROOT/$candidate" ;;
  esac
}

INPUT_PATH=$(resolve_path "$INPUT_HTML")
OUTPUT_PATH=$(resolve_path "$OUTPUT_PDF")
DIAGNOSTICS_PATH=$(resolve_path "$DIAGNOSTICS_JSON")

PROJECT="$REPO_ROOT/src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj"
ARGS=(
  "$INPUT_PATH"
  "$OUTPUT_PATH"
  --diagnostics
  --diagnostics-json "$DIAGNOSTICS_PATH"
)

json_dir=$(dirname "$DIAGNOSTICS_PATH")
mkdir -p "$json_dir"

dotnet run --project "$PROJECT" -- "${ARGS[@]}"
