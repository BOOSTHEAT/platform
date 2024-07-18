#!/bin/bash

mkdir -p $HOME/.ssh

sudo apt update

sudo apt-get install -y zip

if [ ! -d $HOME/.dotnet ]; then

wget https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh
bash ./dotnet-install.sh -c 3.1
bash ./dotnet-install.sh -c 6.0
rm dotnet-install.sh

cat >> $HOME/.profile <<EOF

if [ -d "\$HOME/.dotnet" ] ; then
    PATH="\$HOME/.dotnet:$PATH"
fi
EOF

fi
