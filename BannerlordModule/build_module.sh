#!/bin/bash
# Build script for Linux - поддерживает и ModKit и NuGet

BANNERLORD_PATH="${BANNERLORD_PATH:-$HOME/.steam/steamapps/common/Mount & Blade II Bannerlord}"
MODKIT_PROJ="NordInvasion.ModKit.csproj"
NUGET_PROJ="NordInvasion.csproj"

echo "Building Nord Invasion Better Edition..."

if [ -f "$BANNERLORD_PATH/bin/Win64_Shipping_Client/TaleWorlds.Core.dll" ]; then
  echo "Found Bannerlord at $BANNERLORD_PATH -> building ModKit version ($MODKIT_PROJ)"
  echo "BannerlordPath=$BANNERLORD_PATH"
  dotnet build "$MODKIT_PROJ" -c Release -p:BannerlordPath="$BANNERLORD_PATH"
  BUILD_RESULT=$?
else
  echo "Bannerlord not found at $BANNERLORD_PATH -> building NuGet version ($NUGET_PROJ)"
  echo "To build ModKit version, set BANNERLORD_PATH env"
  dotnet restore "$NUGET_PROJ"
  dotnet build "$NUGET_PROJ" -c Release
  BUILD_RESULT=$?
fi

if [ $BUILD_RESULT -ne 0 ]; then
  echo "Build failed!"
  exit 1
fi

echo "Copying to Modules (if Bannerlord path exists)..."
if [ -d "$BANNERLORD_PATH/Modules/NordInvasion" ]; then
  mkdir -p "$BANNERLORD_PATH/Modules/NordInvasion/bin/Win64_Shipping_Client"
  mkdir -p "$BANNERLORD_PATH/Modules/NordInvasion/bin/Win64_Shipping_Server"
  cp -f Modules/NordInvasion/bin/Win64_Shipping_Client/NordInvasion.dll "$BANNERLORD_PATH/Modules/NordInvasion/bin/Win64_Shipping_Client/" 2>/dev/null || echo "No Client DLL yet"
  cp -f Modules/NordInvasion/bin/Win64_Shipping_Server/NordInvasion.dll "$BANNERLORD_PATH/Modules/NordInvasion/bin/Win64_Shipping_Server/" 2>/dev/null || cp -f Modules/NordInvasion/bin/Win64_Shipping_Client/NordInvasion.dll "$BANNERLORD_PATH/Modules/NordInvasion/bin/Win64_Shipping_Server/" 2>/dev/null || true
  cp -rf Modules/NordInvasion/ModuleData "$BANNERLORD_PATH/Modules/NordInvasion/" 2>/dev/null || true
  cp -f Modules/NordInvasion/SubModule.xml "$BANNERLORD_PATH/Modules/NordInvasion/" 2>/dev/null || true
  echo "Copied to $BANNERLORD_PATH/Modules/NordInvasion"
else
  echo "Bannerlord Modules folder not found, skipping copy. DLL in Modules/NordInvasion/bin/"
fi

echo "Build complete! For ModKit see docs/MODKIT_GUIDE_RU.md"
