#!/bin/bash
# Build script for Linux

BANNERLORD_PATH="$HOME/.steam/steam/steamapps/common/Mount & Blade II Bannerlord"

echo "Building Nord Invasion Better Edition..."

if [ ! -f "$BANNERLORD_PATH/bin/Win64_Shipping_Client/TaleWorlds.Core.dll" ]; then
  echo "Bannerlord not found at $BANNERLORD_PATH"
  echo "Edit this script and set BANNERLORD_PATH"
  exit 1
fi

dotnet build NordInvasion.csproj -c Release

if [ $? -ne 0 ]; then
  echo "Build failed!"
  exit 1
fi

echo "Copying to Modules..."
mkdir -p "$BANNERLORD_PATH/Modules/NordInvasion/bin/Win64_Shipping_Client"
cp -f Modules/NordInvasion/bin/Win64_Shipping_Client/NordInvasion.dll "$BANNERLORD_PATH/Modules/NordInvasion/bin/Win64_Shipping_Client/"
cp -rf Modules/NordInvasion/ModuleData "$BANNERLORD_PATH/Modules/NordInvasion/"
cp -f Modules/NordInvasion/SubModule.xml "$BANNERLORD_PATH/Modules/NordInvasion/"

echo "Build complete!"
