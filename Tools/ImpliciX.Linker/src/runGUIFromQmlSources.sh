#!/bin/bash
set -e

usage() {
  echo "Build and run GUI Linux64 with your local QML project"
  echo
  echo "Usage: $(basename "$0") <full path to QML main project (.pro)> [VNC|X11] [GUI parameters]"
  echo "VNC: run GUI as a remote VNC server"
  echo "XCB: run GUI by connecting to the local X11 display"
  echo
  echo "Example of call :"
  echo "./$(basename "$0") /home/christophe/Documents/QtProjects/piechart/piechart.pro"
  exit 1
}

if [ $# -lt 1 ]; then usage; fi

QT_PROJECT_FILE_FULL_PATH=$1
DISPLAY_MODE=${2:-"X11"}
GUI_PARAMETERS=${3:-""}

QT_PROJECT_DIR_FULL_PATH=$(dirname "$QT_PROJECT_FILE_FULL_PATH")

echo "----------------- QML BUILDING FROM SOURCE --------------------"

OUTPUT_DIR="/tmp/$(basename "$0")-build_linux-x64"
mkdir -p "${OUTPUT_DIR}"
echo "QML OUTPUT DIR=$OUTPUT_DIR"

QML_EXE_NAME=$(cat "$QT_PROJECT_FILE_FULL_PATH" | grep "TARGET = " | cut -d' ' -f3 | tr -d '\r')
if [ -z "$QML_EXE_NAME" ]; then
  echo "ERROR : 'TARGET = <executable name file>' must be define in $QT_PROJECT_FILE_FULL_PATH"
  exit 1
fi

if [ "${DISPLAY_MODE}" == "VNC" ]; then
  DOCKER_OPTIONS="-i --rm -p 5900:5900 -e QT_QPA_PLATFORM=vnc"
else
  DOCKER_OPTIONS="-i --net=host -e DISPLAY -e QT_QUICK_BACKEND=software -v $HOME/.Xauthority:/root/.Xauthority:rw"
fi

DOCKER_IMAGE=implicixpublic.azurecr.io/implicix-qt5:latest
docker run ${DOCKER_OPTIONS} -v "$QT_PROJECT_DIR_FULL_PATH":/src -u "$(id -u)" $DOCKER_IMAGE <<EOF

cd /src

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

${OUTPUT_DIR}/${QML_EXE_NAME} $GUI_PARAMETERS

EOF
