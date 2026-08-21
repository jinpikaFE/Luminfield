#!/bin/zsh
set -eu

SCRIPT_DIR="${0:A:h}"
PROJECT_ROOT="${SCRIPT_DIR:h}"
TOOLS_ROOT="${LUMINFIELD_TOOLS:-${HOME}/.codex/tools/luminfield}"
DOTNET_ROOT_LOCAL="$TOOLS_ROOT/dotnet"
GODOT_BIN="$TOOLS_ROOT/godot/Godot_mono.app/Contents/MacOS/Godot"

if [[ ! -x "$DOTNET_ROOT_LOCAL/dotnet" ]]; then
  print -u2 "Luminfield .NET SDK not found: $DOTNET_ROOT_LOCAL"
  exit 1
fi

if [[ ! -x "$GODOT_BIN" ]]; then
  print -u2 "Luminfield Godot editor not found: $GODOT_BIN"
  exit 1
fi

export DOTNET_ROOT="$DOTNET_ROOT_LOCAL"
export PATH="$DOTNET_ROOT_LOCAL:$PATH"

exec "$GODOT_BIN" --editor --path "$PROJECT_ROOT" "$@"
