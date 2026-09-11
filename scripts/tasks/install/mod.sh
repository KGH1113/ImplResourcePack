#!/usr/bin/env bash
set -euo pipefail

TASK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=../../lib/context.sh
source "$TASK_DIR/../../lib/context.sh"
# shellcheck source=../../lib/guards.sh
source "$TASK_DIR/../../lib/guards.sh"
# shellcheck source=../../lib/artifacts.sh
source "$TASK_DIR/../../lib/artifacts.sh"

assert_non_root_path "$IMPL_RESOURCEPACK_INSTALL_PATH"
mkdir -p "$IMPL_RESOURCEPACK_INSTALL_PATH"

if [ "${1:-}" = "--runtime-only" ]; then
  require_file "$IMPL_RESOURCEPACK_INSTALL_PATH/Info.json"
  copy_runtime_payload "$IMPL_RESOURCEPACK_INSTALL_PATH"
  printf 'Updated runtime at %s (restart the game to load it)\n' "$IMPL_RESOURCEPACK_INSTALL_PATH"
  exit 0
fi

copy_core_payload "$IMPL_RESOURCEPACK_INSTALL_PATH"
copy_assets "$IMPL_RESOURCEPACK_INSTALL_PATH"
copy_debug_symbols "$IMPL_RESOURCEPACK_INSTALL_PATH"

printf 'Installed to %s\n' "$IMPL_RESOURCEPACK_INSTALL_PATH"
