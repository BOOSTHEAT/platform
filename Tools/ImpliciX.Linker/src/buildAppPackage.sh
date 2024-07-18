#!/bin/bash
set -e

#-------------------------------- Parameters management --------------------------------
Help() {

  scriptName=$(basename "$0")

  echo "Build and pack App and/or gui ready for MMI"
  echo
  echo "Syntax: [-a|-c|-e|-g|-n|-o|h]"
  echo "a              manifest app id. No App build & pack is not set. Ex : devices:mmi:app"
  echo "e  (required)  App definition name class (with its namespace). Ex : Caliper.App.Main"
  echo "g              manifest gui id. No GUI build & pack is not set. Ex : devices:mmi:gui"
  echo "n  (required)  Application Name"
  echo "o  (required)  pack output folder path"
  echo "p  (required)  C# project file path"
  echo "h              Print this Help."
  echo
  echo "Required :"
  echo "'IMPLICIX_LINKER' environment variable must contains the full path directory where we can find 'ImpliciX.Linker' executable"
  echo
  echo "- Example of call to build & pack app only :"
  echo "    $scriptName -n Caliper -p MyApp/Caliper.App.csproj -e Caliper.App.Main -a devices:mmi:app -o /tmp/Caliper"
  echo
  echo "- Example of call to build & pack gui only :"
  echo "    $scriptName -n Caliper -p MyApp/Caliper.App.csproj -e Caliper.App.Main -g devices:mmi:gui -o /tmp/Caliper"
  echo
  echo "- Example of call to build & pack app and gui :"
  echo "    $scriptName -n Caliper -p MyApp/Caliper.App.csproj -e Caliper.App.Main -a devices:mmi:app -g devices:mmi:gui -o /tmp/Caliper"
}

# Get the options
while getopts "a:e:g:n:o:p:h" option; do
  case $option in
  h) # display Help
    Help ;;
  a) # manifest app id
    MANIFEST_APP_ID=$OPTARG ;;
  p) # C# project file path
    CSPROJ_PATH=$OPTARG ;;
  e) # App definition name class
    APP_DEFINITION_CLASS=$OPTARG ;;
  g) # manifest gui id
    MANIFEST_GUI_ID=$OPTARG ;;
  n) # Application Name
    APP_NAME=$OPTARG ;;
  o) # pack output folder path
    OUTPUT_PATH=$OPTARG ;;
  \?) # Invalid option
    echo "Error: Invalid option" ;;
  esac
done

CheckRequiredParameters() {
  if [ -z "$CSPROJ_PATH" ] || [ -z "$APP_NAME" ] || [ -z "$APP_DEFINITION_CLASS" ] || [ -z "$OUTPUT_PATH" ]; then
    Help
    exit 1
  fi

  if [ -z "$MANIFEST_APP_ID" ] && [ -z "$MANIFEST_GUI_ID" ]; then
    echo
    echo "===> ERROR : You must have at least -a or -g "
    echo
    Help
    exit 1
  fi

  return 0
}

CheckRequiredParameters
#---------------------------------------------------------------------------------------

if [ $# -lt 6 ]; then usage; fi

VERSION=$(date +"%Y.%m%d.%H%M.%S" | sed "s/^0*//g; s/\.0*/./g")
PACK_FILE_OUTPUT_PATH="$OUTPUT_PATH/$APP_NAME.$VERSION.zip"
TEMP_PATH="$(mktemp -d -u)"
APP_OUTPUT="$TEMP_PATH/app.zip"

if [ -z "$IMPLICIX_LINKER" ]; then
  echo "IMPLICIX_LINKER environment variable must be set to the ImpliciX.Linker home directory path"
  exit 1
fi
LINKER="$IMPLICIX_LINKER/ImpliciX.Linker"

PackPackagesList=""

if [ ! -z "$MANIFEST_APP_ID" ]; then
  # Build from source
  $LINKER build \
    -s https://pkgs.dev.azure.com/boostheat/_packaging/ImpliciX/nuget/v3/index.json \
    -n ImpliciX.Runtime \
    -p "$CSPROJ_PATH" \
    -e "$APP_DEFINITION_CLASS" \
    -v "$VERSION" \
    -o "$APP_OUTPUT"

  PackPackagesList="-p ${MANIFEST_APP_ID},${VERSION},${APP_OUTPUT}"
fi

if [ ! -z "$MANIFEST_GUI_ID" ]; then
  # Build GUI for MMI
  GUI_OUTPUT_FILE_PATH="$TEMP_PATH/gui"
  mkdir -p "$GUI_OUTPUT_FILE_PATH"

  "$IMPLICIX_LINKER"/buildMmiGUI.sh "$CSPROJ_PATH" "$APP_DEFINITION_CLASS" "$GUI_OUTPUT_FILE_PATH"

  GUI_EXE_FILE_NAME="$GUI_OUTPUT_FILE_PATH/$(ls "$GUI_OUTPUT_FILE_PATH")"

  PackPackagesList="$PackPackagesList -p ${MANIFEST_GUI_ID},${VERSION},${GUI_EXE_FILE_NAME}"
fi

# Pack
$LINKER pack \
  -n "$APP_NAME" \
  -v "$VERSION" \
  -o "$PACK_FILE_OUTPUT_PATH" \
  $PackPackagesList

echo "Pack successfully put in $PACK_FILE_OUTPUT_PATH"
