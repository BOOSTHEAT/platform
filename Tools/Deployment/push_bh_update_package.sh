#!/bin/bash

usage() {
  echo "Usage: $0 <application-name> <target-ip-address>"
  exit 1
}

if [ $# != 2 ]; then usage; fi

APPLICATION_NAME="$1"
APPLICATION_ID="$(echo -e "${APPLICATION_NAME}" | tr -d '[:space:]')"
echo "Application Name: ${APPLICATION_NAME} / Application ID: ${APPLICATION_ID}"
IP="$2"
SCRIPT_FOLDER=$(dirname "$0")
ROOT=$(readlink -f "$SCRIPT_FOLDER/../..")
APPLICATION=$(echo "$ROOT"/Applications/${APPLICATION_ID}/*Features/src/*.csproj)
ENTRY_POINT="BOOSTHEAT.Applications.${APPLICATION_ID}.Features.Main"
${SCRIPT_FOLDER}/push_update_package.sh $IP "$APPLICATION_NAME" "$APPLICATION_ID" "$APPLICATION" "$ENTRY_POINT"
