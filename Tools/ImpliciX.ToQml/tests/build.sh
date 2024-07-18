#!/bin/bash

set -x
SCRIPT_FOLDER=$(readlink -f $(dirname $0))

clear
dotnet build --no-incremental
dotnet test --filter ImpliciX.ToQml.Tests.Sample3.Test
if [ $? -ne 0 ]; then
  exit
fi
  

SRC_FOLDER="/tmp/$(ls -t /tmp | head -1)"

export OUTPUT_FOLDER=linux-x64
mkdir -p ${SRC_FOLDER}/${OUTPUT_FOLDER}

DOCKER_IMAGE=implicixpublic.azurecr.io/implicix-qt5:latest

docker run -i --rm -v "${SRC_FOLDER}":/src -u "$(id -u)" $DOCKER_IMAGE << EOF

set -x
cd /src
qmake -o ${OUTPUT_FOLDER}/Makefile && make -C ${OUTPUT_FOLDER}

EOF

#DISPLAY_MODE=VNC
if [ "${DISPLAY_MODE}" == "VNC" ]; then
  DOCKER_OPTIONS=" --name runqml -i -p 5900:5900 -e QT_QPA_PLATFORM=vnc"
else
  DOCKER_OPTIONS="-i --net=host -e DISPLAY -e QT_QUICK_BACKEND=software -v $HOME/.Xauthority:/root/.Xauthority:rw"
fi

docker run --rm -v "${SRC_FOLDER}/${OUTPUT_FOLDER}/ImpliciX.GUI:/GUI" \
           ${DOCKER_OPTIONS} $DOCKER_IMAGE \
           /GUI backend=127.0.0.1:9999

#rm -rf ${SRC_FOLDER}
