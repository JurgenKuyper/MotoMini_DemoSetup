#!/bin/bash

# make a temp staging folder for the .yip archive and copy only the files we need on the pendant
rm -Rf /tmp/MotoMini_DemoSetup
rm MotoMini_DemoSetup.yip
mkdir -p /tmp/MotoMini_DemoSetup
mkdir -p /tmp/MotoMini_DemoSetup/images
cp images/* /tmp/MotoMini_DemoSetup/images/
cp *.yml bin/Debug/netcoreapp2.2/linux-arm/publish/* /tmp/MotoMini_DemoSetup/

# Finally, ask Smart Packager to create a unprotected package using the JSONNET template & the temp folder as archive .yip content
SmartPackager --unprotected --package MotoMini_DemoSetup.yip --new MotoMini_DemoSetup-extension-yip-template.jsonnet --archive /tmp/MotoMini_DemoSetup

if cp MotoMini_DemoSetup.yip /media/jurge/3046-8345/; then
   echo "Copy Code: $? - Successful"
else
   echo "Copy Code: $? - Unsuccessful"
fi