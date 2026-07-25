#!/usr/bin/env bash
set -euo pipefail

TASK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=../../lib/context.sh
source "$TASK_DIR/../../lib/context.sh"
# shellcheck source=../../lib/guards.sh
source "$TASK_DIR/../../lib/guards.sh"

require_command zip
require_dir "$IMPL_RESOURCEPACK_PACKAGE_STAGE"

rm -f "$IMPL_RESOURCEPACK_PACKAGE_ZIP_PATH"
mkdir -p "$(dirname "$IMPL_RESOURCEPACK_PACKAGE_ZIP_PATH")"
(
  cd "$IMPL_RESOURCEPACK_PACKAGE_BUILD_ROOT"
  zip -r "$IMPL_RESOURCEPACK_PACKAGE_ZIP_PATH" ImplResourcePack \
    -x 'ImplResourcePack/*.log'
)

printf 'Packaged to %s\n' "$IMPL_RESOURCEPACK_PACKAGE_ZIP_PATH"

