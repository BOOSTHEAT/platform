#!/bin/bash
set -e

SCRIPT=$(readlink -f $0)
SCRIPTPATH=`dirname $SCRIPT`

RUNTIME_PROJECT_BASE_PATH="$SCRIPTPATH/../Runtime/ImpliciX.Runtime/src"
RUNTIME_PROJECT_PATH="$RUNTIME_PROJECT_BASE_PATH/ImpliciX.Runtime.csproj"
RUNTIME_NUSPEC_PATH="$RUNTIME_PROJECT_BASE_PATH/runtime.nuspec"

LINKER_PROJECT_BASE_PATH="$SCRIPTPATH/../Tools/ImpliciX.Linker/src"
LINKER_PROJECT_PATH="$LINKER_PROJECT_BASE_PATH/ImpliciX.Linker.csproj"
LINKER_NUSPEC_PATH="$LINKER_PROJECT_BASE_PATH/linker.nuspec"

TMP_FOLDER="/tmp/populate_designer_nuget_cache/"
NUGET_GLOBAL_CACHE_FOLDER="$HOME/.nuget/packages/"
NUGET_LOCAL_BASE_FOLDER="$SCRIPTPATH/../Tools/ImpliciX.Designer/src/bin/"

echo "-- Prepare temp_folder"
if [ "$TMP_FOLDER" ]; then
  echo
  echo "-- Remove $TMP_FOLDER"
  rm -rf "$TMP_FOLDER"
fi

mkdir "$TMP_FOLDER"

echo
echo "-- Create runtime nuget package from $RUNTIME_PROJECT_PATH"
dotnet pack "$RUNTIME_PROJECT_PATH" -p:NuspecFile="$RUNTIME_NUSPEC_PATH"  -c Release -o "$TMP_FOLDER" --interactive

echo
echo "-- Create linker nuget package from $LINKER_PROJECT_PATH"
dotnet pack "$LINKER_PROJECT_PATH" -p:NuspecFile="$LINKER_NUSPEC_PATH"  -c Release -o "$TMP_FOLDER" --interactive

echo
latest_version=$(find "$NUGET_GLOBAL_CACHE_FOLDER" -type f -iname 'implicix.language.*.nupkg' | sort -V | tail -n 1)
echo "-- Copy the latest language $latest_version to $TMP_FOLDER"
cp "$latest_version" "$TMP_FOLDER"

find "$NUGET_LOCAL_BASE_FOLDER" -type f -name 'ImpliciX.Designer.App' | while read app_file; do
    echo
    app_dir=$(dirname "$app_file")
    nuget_dir="$app_dir/NuGetPackages/"
    if [ ! -d "$nuget_dir" ]; then
        echo "-- Creating $nuget_dir"
        mkdir "$nuget_dir"
    fi
    echo "-- Remove all existing implicix packages in $nuget_dir"
    find "$nuget_dir" -type d -iname 'ImpliciX.*' -exec rm -rf {} +
    echo "-- Installing nuget packages to $nuget_dir"
    find "$TMP_FOLDER" -type f -iname '*.nupkg' -exec dotnet nuget push {} -s "$nuget_dir" \;
done

echo
echo "Done!"
