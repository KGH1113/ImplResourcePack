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
  require_file "$IMPL_RESOURCEPACK_PROJECT_ROOT/ImplResourcePack/AdofaiIpcBootstrap.json"
  require_file "$IMPL_RESOURCEPACK_PROJECT_ROOT/THIRD_PARTY_NOTICES.md"
  require_file "$IMPL_RESOURCEPACK_BUILD_OUTPUT/ImplResourcePack.dll"
  require_file "$IMPL_RESOURCEPACK_BOOTSTRAP_BUILD_OUTPUT/ImplResourcePack.Bootstrap.dll"
  require_file "$ADOFAI_IPC_DEPENDENCY_SHIM_DLL"
  require_file "$ADOFAI_IPC_BOOTSTRAP_DLL"

  # shellcheck disable=SC1090
  source "$ADOFAI_IPC_BOOTSTRAP_LOCK"

  mkdir -p "$destination"
  cp "$IMPL_RESOURCEPACK_PROJECT_ROOT/ImplResourcePack/Info.json" "$destination/"
  cp "$IMPL_RESOURCEPACK_PROJECT_ROOT/ImplResourcePack/AdofaiIpcBootstrap.json" "$destination/"
  cp "$IMPL_RESOURCEPACK_BUILD_OUTPUT/ImplResourcePack.dll" "$destination/"
  cp "$IMPL_RESOURCEPACK_BOOTSTRAP_BUILD_OUTPUT/ImplResourcePack.Bootstrap.dll" "$destination/"
  cp "$ADOFAI_IPC_DEPENDENCY_SHIM_DLL" "$destination/"
  cp "$IMPL_RESOURCEPACK_PROJECT_ROOT/THIRD_PARTY_NOTICES.md" "$destination/"
  mkdir -p "$destination/DependencyBootstrap/versions/$ADOFAIIPC_BOOTSTRAP_VERSION"
  cp "$ADOFAI_IPC_BOOTSTRAP_DLL" \
    "$destination/DependencyBootstrap/versions/$ADOFAIIPC_BOOTSTRAP_VERSION/"
  printf '{\n  "SchemaVersion": 1,\n  "Current": "%s",\n  "Previous": null,\n  "Trial": null\n}\n' \
    "$ADOFAIIPC_BOOTSTRAP_VERSION" > "$destination/DependencyBootstrap/state.json"
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
