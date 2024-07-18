#!/bin/bash

BOILER_APP_ARTEFACT_NAME="boiler_app.zip"
APPLICATIONNAME="$1"
BUILDNUMBER=$2
BINARIES_ARCHIVE_FILE_NAME="${BUILDNUMBER}.zip"
HARMONY_PACKAGE_NAME=$3
OUTPUT_JSON="BOOSTHEAT.20.V2.RELEASE.${BUILDNUMBER}.manifest.json"

zip -q ${BINARIES_ARCHIVE_FILE_NAME} ${BOILER_APP_ARTEFACT_NAME}

echo '{"device": "'${APPLICATIONNAME}'", "revision": "'${BUILDNUMBER}'", "date": "'"$(date +'%Y-%m-%dT%H:%M:%S')"'", "sha256": "'"$(sha256sum ${BINARIES_ARCHIVE_FILE_NAME} | cut -d " " -f1)"'", "content": {"APPS": [{"part-number": 99999999999, "target": "devices:mmi:boiler_app", "hrRevision": "'${BUILDNUMBER}'", "revision": "'${BUILDNUMBER}'", "filename": "boiler_app.zip"}]}}' > "${OUTPUT_JSON}"
echo "Manifest for ${APPLICATIONNAME} ${BUILDNUMBER}:"
cat ${OUTPUT_JSON}

zip -q ${HARMONY_PACKAGE_NAME} ${OUTPUT_JSON} ${BINARIES_ARCHIVE_FILE_NAME}

rm -rf ${OUTPUT_JSON} ${BINARIES_ARCHIVE_FILE_NAME} ${BOILER_APP_ARTEFACT_NAME}
