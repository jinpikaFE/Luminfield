#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TOOLS_ROOT="${LUMINFIELD_TOOLS:-${HOME}/.codex/tools/luminfield}"
DOTNET_BIN="$TOOLS_ROOT/dotnet/dotnet"
GODOT_BIN="$TOOLS_ROOT/godot/Godot_mono.app/Contents/MacOS/Godot"
export DOTNET_ROOT="$TOOLS_ROOT/dotnet"
export PATH="$DOTNET_ROOT:$PATH"

"$DOTNET_BIN" build "$PROJECT_ROOT/Luminfield.sln" --no-restore
"$DOTNET_BIN" test \
  "$PROJECT_ROOT/tests/Luminfield.Tests/Luminfield.Tests.csproj" \
  --no-restore
"$GODOT_BIN" --headless --path "$PROJECT_ROOT" --editor --quit
"$GODOT_BIN" --headless --path "$PROJECT_ROOT" --quit-after 180

test -f "$PROJECT_ROOT/builds/macos/Luminfield.zip"
test -f "$PROJECT_ROOT/builds/windows/Luminfield.exe"
test -f "$PROJECT_ROOT/builds/linux/Luminfield.x86_64"

echo "Phase G release-candidate checks passed."
