#!/bin/bash

usage() {
  echo "Build for MMI board your local QML project and copy the executable generated to the output path chosen."
  echo
  echo "Usage: $(basename "$0") <full path to qt project file (.pro)> <output path>"
  echo
  echo "Example of call :"
  echo "./$(basename "$0") /home/christophe/Documents/QtProjects/piechart/piechart.pro /tmp/qtbuild/"
  exit 1
}

if [ $# -lt 2 ]; then usage; fi

QT_PROJECT_FILE_FULL_PATH=$1
OUTPUT_PATH=$2
QT_PROJECT_DIR_FULL_PATH=$(dirname "$QT_PROJECT_FILE_FULL_PATH")

echo "------------------- QML ARM BUILDING -----------------------"

QML_BUILDER_DOCKER_IMAGE_NAME=yocto

CheckRequiredForQMLBuilding() 
{
    dockerImagesRequiredTrace=$(docker images | grep $QML_BUILDER_DOCKER_IMAGE_NAME)

    if [ -z "$dockerImagesRequiredTrace" ]; then
        echo "'$QML_BUILDER_DOCKER_IMAGE_NAME' docker image is required to build qml project for MMI, but this image is unknwon on this computer (docker images)".
        echo "To build $QML_BUILDER_DOCKER_IMAGE_NAME image :"
        echo "  1. Get mmi2-meta-boostheat repository from CI platform"
        echo "  2. Go to mmi2-meta-boostheat/scripts folder"
        echo "  3. Run : docker -build -t $QML_BUILDER_DOCKER_IMAGE_NAME ."
        exit 1
    fi
    return 0
}

CheckRequiredForQMLBuilding
set -e

QML_EXE_NAME=$(cat "$QT_PROJECT_FILE_FULL_PATH" | grep "TARGET = " | cut -d' ' -f3 | tr -d '\r')
if [ -z "$QML_EXE_NAME" ]; then
  echo "ERROR : 'TARGET = <executable name file>' must be define in $QT_PROJECT_FILE_FULL_PATH"
  exit 1
fi

SDK_INSTALLER_SCRIPT_NAME="boostheat-fb-glibc-x86_64-Boostheat_generate_sdk-armv7at2hf-neon-colibri-imx7-emmc-toolchain-5.0.0.sh"
HOST_IMPLICIX_BSP_PATH="$HOME/implicix/BSP"

docker run -i --rm -v "$HOST_IMPLICIX_BSP_PATH":/bsp -v "$QT_PROJECT_DIR_FULL_PATH":/src -v "$OUTPUT_PATH":/out -u "$(id -u)" $QML_BUILDER_DOCKER_IMAGE_NAME <<EOF
set -e

cd /bsp

if [ -d yocto-sdk ]; then
  echo "=====> yocto-sdk is already installed"
else
  echo "------------- Install cross-compile SDK for MMI ----------------"

  if ! [ -f ./$SDK_INSTALLER_SCRIPT_NAME ]; then
    echo
    echo "ERROR : This file is missing : '$HOST_IMPLICIX_BSP_PATH/$SDK_INSTALLER_SCRIPT_NAME'"
    echo "  ==> Please :"
    echo "      1. Download it from ImpliciX.mmi2-meta-boostheat BSP artifact"
    echo "      2. Put it in '$HOST_IMPLICIX_BSP_PATH'"
    exit 1
  fi
 
  echo 'y'|sh ./$SDK_INSTALLER_SCRIPT_NAME -d yocto-sdk
fi

. yocto-sdk/environment-setup-armv7at2hf-neon-tdx-linux-gnueabi

cd /src
rm -rf build_arm
mkdir -p build_arm
qmake -o build_arm/Makefile
make -C build_arm

cp build_arm/$QML_EXE_NAME /out/

EOF
