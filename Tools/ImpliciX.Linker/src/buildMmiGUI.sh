#!/bin/bash
set -e

usage() {
  echo "Build for MMI board your GUI executable with your local C# project source (without use CI/CD platform)"
  echo
  echo "Usage: $(basename "$0") <path to app main csproj> <full name of app definition class> <gui file output path>"
  exit 1
}

if [ $# -lt 3 ]; then usage; fi

CSPROJ_PATH=$1
APP_DEFINITION_CLASS=$2
OUTPUT_PATH=$3
VERSION=$(date +"%Y.%m%d.%H%M.%S" | sed "s/^0*//g; s/\.0*/./g")
TMP_GUI_DIR_NAME=RunMmiGUI

if [ -z "$IMPLICIX_LINKER" ]; then
  echo "IMPLICIX_LINKER environment variable must be set to the ImpliciX.Linker home directory path"
  exit 1
fi

LINKER="$IMPLICIX_LINKER/ImpliciX.Linker"

if [ -d "/tmp/$TMP_GUI_DIR_NAME" ]; then
  echo "$TMP_GUI_DIR_NAME exists in /tmp : we remove it"
  rm -rf /tmp/$TMP_GUI_DIR_NAME
fi

TMP_GUI_FULL_PATH="/tmp/$TMP_GUI_DIR_NAME"

echo "---------------- GUI QML BUILDING FROM SOURCE ------------------"

$LINKER qml \
  -p "$CSPROJ_PATH" \
  -e "$APP_DEFINITION_CLASS" \
  -v "$VERSION" \
  -s https://pkgs.dev.azure.com/boostheat/_packaging/ImpliciX/nuget/v3/index.json \
  -o "$TMP_GUI_FULL_PATH"

cat >"$TMP_GUI_FULL_PATH/version.js" <<EOF
var version = "${VERSION}";
EOF

echo "------------------- GUI QML ARM BUILDING -----------------------"

"$IMPLICIX_LINKER"/buildQtProjectForMmi.sh "$TMP_GUI_FULL_PATH/main.pro" "$OUTPUT_PATH"
