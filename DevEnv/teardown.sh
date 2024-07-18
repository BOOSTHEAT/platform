#!/bin/bash
set -euo pipefail
if [[ "$(uname -s)" == "Darwin" ]]; then READLINK="realpath"; else READLINK="readlink -f"; fi

CURRENT_DIR=$(dirname "$($READLINK "$0")")
pushd "${CURRENT_DIR}" > /dev/null

docker compose -f dev-env-compose.yml down -v

popd > /dev/null
