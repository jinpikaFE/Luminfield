#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TOOLS_ROOT="${LUMINFIELD_TOOLS:-/Users/edy/.codex/tools/luminfield}"
GODOT_BIN="$TOOLS_ROOT/godot/Godot_mono.app/Contents/MacOS/Godot"
export DOTNET_ROOT="$TOOLS_ROOT/dotnet"
export PATH="$DOTNET_ROOT:$PATH"

mkdir -p \
  "$PROJECT_ROOT/builds/macos" \
  "$PROJECT_ROOT/builds/windows" \
  "$PROJECT_ROOT/builds/linux"

dotnet build "$PROJECT_ROOT/Luminfield.sln" --configuration ExportRelease

SIGN_STAGE="$(mktemp -d /tmp/luminfield-export.XXXXXX)"
trap 'rm -rf -- "${SIGN_STAGE:?}"' EXIT

"$GODOT_BIN" --headless --path "$PROJECT_ROOT" \
  --export-release "macOS" "$SIGN_STAGE/Luminfield-raw.zip"
ditto -x -k "$SIGN_STAGE/Luminfield-raw.zip" "$SIGN_STAGE/app"

MAC_APP="$SIGN_STAGE/app/Luminfield.app"
codesign --force --deep --sign - --options runtime \
  --entitlements "$PROJECT_ROOT/packaging/macos.entitlements" \
  --generate-entitlement-der "$MAC_APP"
codesign --verify --deep --strict "$MAC_APP"
ditto -c -k --sequesterRsrc --keepParent \
  "$MAC_APP" "$PROJECT_ROOT/builds/macos/Luminfield.zip"

"$GODOT_BIN" --headless --path "$PROJECT_ROOT" \
  --export-release "Windows Desktop" \
  "$PROJECT_ROOT/builds/windows/Luminfield.exe"
"$GODOT_BIN" --headless --path "$PROJECT_ROOT" \
  --export-release "Linux" \
  "$PROJECT_ROOT/builds/linux/Luminfield.x86_64"

echo "Exports are ready under $PROJECT_ROOT/builds"
