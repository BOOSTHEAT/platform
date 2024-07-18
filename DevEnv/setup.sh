#!/bin/bash
set -euo pipefail

if [[ "$(uname -s)" == "Darwin" ]]; then READLINK="realpath"; else READLINK="readlink -f"; fi

INFLUXDB=${INFLUXDB:-"influxdb"}
REDIS=${REDIS:-"redis"}
CLOUD_ENV=${1:-"dev"}

SERVICES="${INFLUXDB} ${REDIS}"

CURRENT_DIR=$(dirname "$($READLINK "$0")")
pushd "${CURRENT_DIR}" > /dev/null

echo "sourcing ${CLOUD_ENV}.env"
source "${CLOUD_ENV}.env"

# shellcheck disable=SC2086
docker compose -f dev-env-compose.yml up -d ${SERVICES}

ENROLLMENT_GROUP_KEY_HEX=$(echo "$ENROLLMENT_GROUP_KEY" | base64 -d | xxd -p -c 64)
DEVICE_KEY=$(printf "%s" "$(hostname)" | openssl sha256 -binary -mac hmac -macopt hexkey:"$ENROLLMENT_GROUP_KEY_HEX" | base64)                                                                                                                                  
docker exec "${REDIS}" redis-cli config set notify-keyspace-events KEA >> /dev/null
docker exec "${REDIS}" redis-cli -n 3 hset general:serial_number at "09:00:00.0000000" value "SN_$(hostname)" >> /dev/null
docker exec "${REDIS}" redis-cli -n 3 hset general:harmony:id_scope at "09:00:00.0000000" value "$ID_SCOPE" >> /dev/null
docker exec "${REDIS}" redis-cli -n 3 hset general:harmony:device_key at "09:00:00.0000000" value "$DEVICE_KEY" >> /dev/null
docker exec "${REDIS}" redis-cli -n 3 hset general:thingsboard:host at "09:00:00.0000000" value "${THINGSBOARD_HOST}" >> /dev/null
docker exec "${REDIS}" redis-cli -n 3 hset general:thingsboard:access_token at "09:00:00.0000000" value "${THINGSBOARD_ACCESS_TOKEN}" >> /dev/null

mkdir -p /tmp/slot
printf "%s" '{"device": "BOOSTHEAT.20_V2", "revision": "9.9.9.999", "date": "2021-09-20T15:26:21", "sha256": "e84e07b13453ab7bdabdac2c6e78941fa1457f89667ff517722887292fe76bd4", "content": {"APPS": [{"part-number": 99999999999, "target": "boiler_app", "hrRevision": "9.9.9.999", "revision": "9.9.9.999", "filename": "boiler_app.zip"}]}}' > /tmp/slot/manifest.json

popd > /dev/null
