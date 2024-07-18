#!/bin/bash

usage() {
  echo "Usage: $0 <training-id> <ip-address>"
  exit 1
}

if [ $# != 2 ]; then usage; fi

IP_ADDRESS="$2"
TRAINING_ID="$1"
APPLICATION_NAME="$TRAINING_ID"
APPLICATION_ID="$TRAINING_ID"
SCRIPT_FOLDER=$(dirname "$0")
ROOT=$(readlink -f "$SCRIPT_FOLDER/../..")
APPLICATION_PROJECT_PATH=$(echo "$ROOT"/Applications/Training/BOOSTHEAT.Applications.Training.${TRAINING_ID}/src/*.csproj)
APPLICATION_ENTRY_POINT="BOOSTHEAT.Applications.Training.${TRAINING_ID}.Main"


BUILDNUMBER=$(date +"%Y.%m%d.%H%M.%S" | sed "s/^0*//g; s/\.0*/./g")
HARMONY_PACKAGE_NAME="BOOSTHEAT.RELEASE.${APPLICATION_ID}.${BUILDNUMBER}.zip"


QML_FOLDER="/tmp/QML_${TRAINING_ID}"
rm -rf "${QML_FOLDER}"
OUTPUT_FOLDER="linux-x64"
mkdir -p "${QML_FOLDER}/${OUTPUT_FOLDER}"

echo "======="
echo "Generating QML for ${APPLICATION_NAME} ${BUILDNUMBER} with linker"
LINKER=$ROOT/Tools/BOOSTHEAT.Device.Linker/src/BOOSTHEAT.Device.Linker.csproj
set -x
dotnet run --project "$LINKER" -- qml-from-source -a "$APPLICATION_PROJECT_PATH" \
  -e "$APPLICATION_ENTRY_POINT" -v "${BUILDNUMBER}" -o "${QML_FOLDER}"

echo "======="
DOCKER_IMAGE=implicixpublic.azurecr.io/implicix-qt5:latest
echo "Compiling QML for ${APPLICATION_NAME} ${BUILDNUMBER} with docker"
docker run -i --rm -v "${QML_FOLDER}":/src -u "$(id -u)" $DOCKER_IMAGE << EOF
set -x
cd /src
qmake -o "${OUTPUT_FOLDER}/Makefile" && make -C "${OUTPUT_FOLDER}"
EOF

echo "======="
echo "Running GUI for ${APPLICATION_NAME} ${BUILDNUMBER} with docker"
docker run -it --name runqml --rm -v "${QML_FOLDER}/${OUTPUT_FOLDER}/BOOSTHEAT.Device.GUI:/GUI" \
           -p 5900:5900 -e QT_QPA_PLATFORM=vnc $DOCKER_IMAGE \
           /GUI "backend=${IP_ADDRESS}:9999"
