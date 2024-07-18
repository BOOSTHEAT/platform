#!/bin/bash

usage() {
  echo "Usage: $0 <target-ip-address> <application-name> <application-id> <application-project-path> <application-entry-point>"
  exit 1
}

if [ $# != 5 ]; then usage; fi

USER=root
IP="$1"
APPLICATION_NAME="$2"
APPLICATION_ID="$3"
APPLICATION_PROJECT_PATH="$4"
APPLICATION_ENTRY_POINT="$5"
SCRIPT_FOLDER=$(dirname "$0")
ROOT=$(readlink -f "$SCRIPT_FOLDER/../..")
cd "$SCRIPT_FOLDER"

SSH_NO_HOST_CHECK="-o StrictHostKeyChecking=no -o GlobalKnownHostsFile=/dev/null -o UserKnownHostsFile=/dev/null"
command -v rsync > /dev/null
HAS_LOCAL_RSYNC=$?
ssh ${SSH_NO_HOST_CHECK} "${USER}"@"${IP}" "rsync --help" > /dev/null
HAS_REMOTE_RSYNC=$?
if [ "${HAS_LOCAL_RSYNC}" -eq 0 ] && [ "${HAS_REMOTE_RSYNC}" -eq 0 ]; then
  copy-package()
  {
    rsync -e "ssh $SSH_NO_HOST_CHECK" -iaP --del --checksum $@   
  }
else
  copy-package()
  {
    scp -r ${SSH_NO_HOST_CHECK} $@
  }
fi

BUILDNUMBER=$(date +"%Y.%m%d.%H%M.%S" | sed "s/^0*//g; s/\.0*/./g")
HARMONY_PACKAGE_NAME="BOOSTHEAT.RELEASE.${APPLICATION_ID}.${BUILDNUMBER}.zip"

echo "======="
echo "Generating application ${APPLICATION_NAME} ${BUILDNUMBER} with linker"
LINKER=$ROOT/Tools/BOOSTHEAT.Device.Linker/src/BOOSTHEAT.Device.Linker.csproj
RUNTIME=$ROOT/Runtime/BOOSTHEAT.Device.Runtime/src/BOOSTHEAT.Device.Runtime.csproj
dotnet run --project "$LINKER" -- build-from-source -r "$RUNTIME" -a "$APPLICATION_PROJECT_PATH" -e "$APPLICATION_ENTRY_POINT" -v "${BUILDNUMBER}" -o "${SCRIPT_FOLDER}/boiler_app.zip"

echo "======="
echo "Packing application ${APPLICATION_NAME} ${BUILDNUMBER} into $HARMONY_PACKAGE_NAME"
./build_harmony_package.sh "${APPLICATION_NAME}" "${BUILDNUMBER}" "$HARMONY_PACKAGE_NAME"

copy-package "${HARMONY_PACKAGE_NAME}" "${USER}@${IP}":/opt

rm "${HARMONY_PACKAGE_NAME}"

cat << EOF
Note for the 🐈 lover: Open and connect a designer and...
  * general:software:UPDATE file:///opt/${HARMONY_PACKAGE_NAME} 
  * Wait for update completion... the mmi will reboot when done.
EOF