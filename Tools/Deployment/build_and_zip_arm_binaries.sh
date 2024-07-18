#!/bin/bash

APPLICATION_NAME=$1
APPLICATION="$2"
ENTRY_POINT="$3"
VERSION=$4
echo "======="
echo "Generating application $APPLICATION_NAME $VERSION"

SCRIPT_FOLDER=$(dirname "$0")
ROOT=$(readlink -f "$SCRIPT_FOLDER/../..")
LINKER=$ROOT/Tools/BOOSTHEAT.Device.Linker/src/BOOSTHEAT.Device.Linker.csproj
RUNTIME=$ROOT/Runtime/BOOSTHEAT.Device.Runtime/src/BOOSTHEAT.Device.Runtime.csproj

dotnet run --project "$LINKER" -- build-from-source -r "$RUNTIME" -a "$APPLICATION" -e "$ENTRY_POINT" -v "$VERSION" -o "${SCRIPT_FOLDER}/boiler_app.zip"
