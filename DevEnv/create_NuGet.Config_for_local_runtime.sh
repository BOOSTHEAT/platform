#!/bin/bash
set -e

SCRIPT_FOLDER=$(dirname $(readlink -f $0))
${SCRIPT_FOLDER}/populate_designer_nuget_cache.sh
NUGET_PACKAGES=$(readlink -f ${SCRIPT_FOLDER}/../Tools/ImpliciX.Designer/src/bin/Debug/net8.0/NuGetPackages)
PROJECT_FOLDER=$(pwd)
NUGET_CONFIG=${PROJECT_FOLDER}/NuGet.Config

cat > ${NUGET_CONFIG} <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="local" value="${NUGET_PACKAGES}" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
EOF

echo "Created ${NUGET_CONFIG}"
cat ${NUGET_CONFIG}
