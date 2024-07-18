dotnet build
qtproj=$(dotnet run)
cd $qtproj
pwd

DOCKER_IMAGE=implicixpublic.azurecr.io/implicix-qt5:latest
OUTPUT_DIR=build_linux-x64
mkdir -p ${OUTPUT_DIR}

docker run -i --net=host \
  -e DISPLAY -e QT_QUICK_BACKEND=software \
  -v $HOME/.Xauthority:/root/.Xauthority:rw \
  -v "$(pwd)":/src \
  -u "$(id -u)" "${DOCKER_IMAGE}" <<EOF

cd /src
qmake -o ${OUTPUT_DIR}/Makefile && make -C ${OUTPUT_DIR}
./${OUTPUT_DIR}/ImpliciX.GUI

EOF
