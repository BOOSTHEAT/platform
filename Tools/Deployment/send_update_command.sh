#!/bin/bash

usage() {
  echo "Usage: $0 <target-ip-address> <package-path>"
  exit 1
}

if [ $# != 2 ]; then usage; fi

WEBSOCATCMD=${WEBSOCAT:-"websocat"}
echo "Using ${WEBSOCATCMD}"
USER=root
IP="$1"
PACKAGE="$2"

message() {
cat << EOF
{
    "Command": "command",
    "Parameter":
    {
        "Urn": "general:software:UPDATE",
        "Argument": "${PACKAGE}",
        "At": "00:00:00"
    }
}
EOF

}

message | tr --delete '\n' | "${WEBSOCATCMD}" "ws://${IP}:9999/"
