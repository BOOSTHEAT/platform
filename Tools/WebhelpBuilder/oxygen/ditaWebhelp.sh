#!/bin/sh

# Oxygen WebHelp Plugin
# Copyright (c) 1998-2017 Syncro Soft SRL, Romania.  All rights reserved.

# Adapted to docker image ditaot/dita-ot-base

SRC_DIR="/opt/dita-ot/data"

usage() {
  cat <<EOF

Usage: $0 <export-type> <dita-map-relative-path> <css-relative-path>
<dita-map-relative-path> and <css-relative-path> shall be relative to the volume mapped on ${SRC_DIR}
Available values for <export-type>:
 * webhelp
EOF
  exit 42
}

if [ "$#" -ne 3 ]
then 
  usage
fi

# One of the following three values: 
#      webhelp
#      webhelp-responsive
#      webhelp-feedback
#      webhelp-mobile
TRANSTYPE=$1
DITA_MAP=$2

# The path of the directory of the input DITA map file
DITA_MAP_BASE_DIR=${SRC_DIR}/$(dirname "$DITA_MAP")

# The name of the input DITA map file
DITAMAP_FILE=$(basename "$DITA_MAP")

CSS=${SRC_DIR}/$3

#######################################################

# The path of the Java Virtual Machine
WEBHELP_JAVA=java
if [ -f "${JAVA_HOME}/bin/java" ]
then
  WEBHELP_JAVA="${JAVA_HOME}/bin/java"
fi

WEBHELP_HOME=/home/dita-ot/DITA-OT/plugins/com.oxygenxml.webhelp.classic

# The path of the DITA Open Toolkit install directory
DITA_OT_INSTALL_DIR="$(dirname "$(dirname "${WEBHELP_HOME}")")"


# The name of the DITAVAL input filter file 
#DITAVAL_FILE=x1000.ditaval

# The path of the directory of the DITAVAL input filter file
#DITAVAL_DIR="${HOME}/OxygenXMLEditor/samples/dita/mobile-phone/ditaval"

DITA_MAP_OUT_DIR="/opt/dita-ot/out"



"$WEBHELP_JAVA"\
 -Xmx1024m\
 -classpath\
 "$DITA_OT_INSTALL_DIR/tools/ant/lib/ant-launcher.jar:$DITA_OT_INSTALL_DIR/lib/ant-launcher.jar"\
 "-Dant.home=$DITA_OT_INSTALL_DIR/tools/ant" org.apache.tools.ant.launch.Launcher\
  -lib "$DITA_OT_INSTALL_DIR/plugins/com.oxygenxml.webhelp.classic/lib"\
 -lib "$DITA_OT_INSTALL_DIR"\
 -lib "$DITA_OT_INSTALL_DIR/lib"\
 -lib "$DITA_OT_INSTALL_DIR/lib/saxon"\
 -lib "$DITA_OT_INSTALL_DIR/plugins/com.oxygenxml.highlight/lib/xslthl-2.1.1.jar"\
 -f "$DITA_OT_INSTALL_DIR/build.xml"\
 "-Dtranstype=$TRANSTYPE"\
 "-Dbasedir=$DITA_MAP_BASE_DIR"\
 "-Dargs.css=${SRC_DIR}/toc_custom.css"\
 "-Doutput.dir=$DITA_MAP_OUT_DIR/$TRANSTYPE"\
 "-Ddita.temp.dir=/tmp/$TRANSTYPE"\
 "-Dargs.hide.parent.link=no"\
 "-Ddita.dir=$DITA_OT_INSTALL_DIR"\
 "-Dargs.xhtml.classattr=yes"\
 "-Dargs.input=$DITA_MAP_BASE_DIR/$DITAMAP_FILE"\
 "-Dargs.xsl=${SRC_DIR}/dita2webhelpImpl.xsl"\
 "-Dargs.copycss=yes"\
 "-Dargs.css=${SRC_DIR}/toc_custom.css"\
 "-Dargs.cssroot=${SRC_DIR}/"\
 "-Dwebhelp.skin.css=$CSS"\
 "-Dwebhelp.head.script=${SRC_DIR}/addtohead.xhtml"\
 "-DbaseJVMArgLine=-Xmx1024m"
