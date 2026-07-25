#!/usr/bin/env bash

if [ "${IMPL_RESOURCEPACK_ARTIFACTS_LOADED:-0}" = "1" ]; then
  return 0
fi
IMPL_RESOURCEPACK_ARTIFACTS_LOADED=1

IMPL_RESOURCEPACK_ARTIFACTS_LIB_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=context.sh
source "$IMPL_RESOURCEPACK_ARTIFACTS_LIB_DIR/context.sh"
# shellcheck source=guards.sh
source "$IMPL_RESOURCEPACK_ARTIFACTS_LIB_DIR/guards.sh"

copy_core_payload() {
  local destination="$1"

  require_file "$IMPL_RESOURCEPACK_PROJECT_ROOT/ImplResourcePack/Info.json"
  require_file "$IMPL_RESOURCEPACK_PROJECT_ROOT/THIRD_PARTY_NOTICES.md"
  require_file "$IMPL_RESOURCEPACK_BUILD_OUTPUT/ImplResourcePack.dll"

  mkdir -p "$destination"
  cp "$IMPL_RESOURCEPACK_PROJECT_ROOT/ImplResourcePack/Info.json" "$destination/"
  cp "$IMPL_RESOURCEPACK_BUILD_OUTPUT/ImplResourcePack.dll" "$destination/"
  cp "$IMPL_RESOURCEPACK_PROJECT_ROOT/THIRD_PARTY_NOTICES.md" "$destination/"
}

copy_assets() {
  local destination="$1"
  local source="$IMPL_RESOURCEPACK_PROJECT_ROOT/ImplResourcePack/Assets"
  local target="$destination/Assets"

  if [ ! -d "$source" ]; then
    return 0
  fi

  if [ -e "$target" ]; then
    safe_remove_tree "$target" "$destination"
  fi
  cp -R "$source" "$target"
}

copy_debug_symbols() {
  local destination="$1"

  if [ -f "$IMPL_RESOURCEPACK_BUILD_OUTPUT/ImplResourcePack.pdb" ]; then
    cp "$IMPL_RESOURCEPACK_BUILD_OUTPUT/ImplResourcePack.pdb" "$destination/"
  fi
}
