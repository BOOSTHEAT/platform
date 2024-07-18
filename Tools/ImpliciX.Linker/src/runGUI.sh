#!/bin/bash
set -e

usage() {
  echo "Build and run for Linux64 your GUI executable with your local C# project (without use CI/CD platform) (#6759)"
  echo
  echo "Usage: $(basename "$0") <full path to app main csproj> <full name of app definition class> (VNC|X11) [<backend IP>]"
  echo "VNC: run GUI as a remote VNC server"
  echo "XCB: run GUI by connecting to the local X11 display"
  echo
  echo "Required :"
  echo "'IMPLICIX_LINKER' environement variable must contains the full path directory where we can find 'ImpliciX.Linker' executable"
  echo
  echo "Example of call :"
  echo "./$(basename "$0") /home/christophe/WS/Boostheat/ImpliciX.Demonstrators/applications/Caliper/Caliper.App/src/Caliper.App.csproj Caliper.App.Main 127.0.0.1"
  exit 1
}

if [ $# -lt 2 ]; then usage; fi

CSPROJ_PATH=$1
APP_DEFINITION_CLASS=$2
DISPLAY_MODE=$3
BACKEND_IP=${4:-127.0.0.1}

echo "----------------- QML BUILDING FROM SOURCE --------------------"

cd $(dirname $CSPROJ_PATH)
VERSION=$(date +"%Y.%m%d.%H%M.%S" | sed "s/^0*//g; s/\.0*/./g")
TMP_GUI_DIR_NAME=RunGUI
TMP_GUI_FULL_PATH="/tmp/$TMP_GUI_DIR_NAME"
rm -rf ${TMP_GUI_FULL_PATH}
LINKER="$IMPLICIX_LINKER/ImpliciX.Linker"

$LINKER qml \
  -p "$CSPROJ_PATH" \
  -e "$APP_DEFINITION_CLASS" \
  -v "$VERSION" \
  -s https://pkgs.dev.azure.com/boostheat/_packaging/ImpliciX/nuget/v3/index.json \
  -o "$TMP_GUI_FULL_PATH"

echo "------ GUI BUILDING AND RUN (BACKEND IP=$BACKEND_IP) ------"

cd "$TMP_GUI_FULL_PATH" || exit
echo "Temporary GUI DIR=$TMP_GUI_FULL_PATH"
cat >version.js <<EOF
var version = "$VERSION";
EOF

GUI_EXE_NAME=$(cat main.pro | grep "TARGET = " | cut -d' ' -f3)
OUTPUT_DIR=build_linux-x64
mkdir -p ${OUTPUT_DIR}

if [ "${DISPLAY_MODE}" == "VNC" ]; then
  DOCKER_OPTIONS="-i --rm -p 5900:5900 -e QT_QPA_PLATFORM=vnc"
else
  DOCKER_OPTIONS="-i --net=host -e DISPLAY -e QT_QUICK_BACKEND=software -v $HOME/.Xauthority:/root/.Xauthority:rw"
fi

DOCKER_IMAGE=implicixpublic.azurecr.io/implicix-qt5:latest
docker run -v "$(pwd)":/src -u "$(id -u)" ${DOCKER_OPTIONS} $DOCKER_IMAGE <<EOF

cd /src
echo -n "version.js : "
cat version.js

qmake -o ${OUTPUT_DIR}/Makefile && make -C ${OUTPUT_DIR}

if [ "${DISPLAY_MODE}" == "VNC" ]; then
  echo -e "\n------------------------ I AM RUNNING -------------------------\n"
  echo -n "You can connect to my VNC ip address="
  hostname -i

  echo -e "\nYou can kill me with :"
  echo -n "     docker kill "
  hostname
  echo "  or"
  echo "     docker kill ""$""(docker ps | grep ::5900 | cut -d' ' -f1)"
  echo -e "\n---------------------------------------------------------------\n"
fi

./${OUTPUT_DIR}/${GUI_EXE_NAME} backend=$BACKEND_IP:9999 loglevel=VERBOSE

EOF
