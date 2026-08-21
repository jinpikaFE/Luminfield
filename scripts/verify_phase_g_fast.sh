#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TOOLS_ROOT="${LUMINFIELD_TOOLS:-${HOME}/.codex/tools/luminfield}"
DOTNET_BIN="$TOOLS_ROOT/dotnet/dotnet"
export DOTNET_ROOT="$TOOLS_ROOT/dotnet"
export PATH="$DOTNET_ROOT:$PATH"

"$DOTNET_BIN" build "$PROJECT_ROOT/Luminfield.sln" --no-restore
"$DOTNET_BIN" test \
  "$PROJECT_ROOT/tests/Luminfield.Tests/Luminfield.Tests.csproj" \
  --no-restore \
  --filter \
  "FullyQualifiedName~PhaseG|FullyQualifiedName~PlaytestScenarioRegistryTests|FullyQualifiedName~SaveServiceTests|FullyQualifiedName~RuntimeAssetReferenceTests"

echo "Phase G fast checks passed."
