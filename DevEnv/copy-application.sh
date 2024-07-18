#!/bin/bash

usage() {
  echo "Usage: $0 <src-folder> <dst-folder>"
  exit 1
}

if [ $# != 2 ]; then usage; fi

SRCFOLDER=$(readlink -f $1)
SRCAPP=$(basename "${SRCFOLDER}")
DSTFOLDER=$(readlink -f $2)
DSTAPP=$(basename "${DSTFOLDER}")

echo "Creating ${DSTAPP} from ${SRCAPP}"

rm -rf "${DSTFOLDER}"
cp -r "${SRCFOLDER}" "${DSTFOLDER}"

rm -rf $(find "${DSTFOLDER}" -name obj)
rm -rf $(find "${DSTFOLDER}" -name bin)

for f in $(find "${DSTFOLDER}" -name "*${SRCAPP}*" -type d)
do
  newName=$(echo $f | sed "s/${SRCAPP}/${DSTAPP}/g")
  mv "$f" "$newName"
done

for f in $(find "${DSTFOLDER}" -name "*${SRCAPP}*" -type f)
do
  newName=$(echo $f | sed "s/${SRCAPP}/${DSTAPP}/g")
  mv "$f" "$newName"
done

for f in $(find "${DSTFOLDER}" -type f)
do
  sed -i "s/${SRCAPP}/${DSTAPP}/g" $f
done

echo "Do no forget to modify the AppName"
grep "AppName" $(find "${DSTFOLDER}" -type f)
