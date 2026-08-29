#!/bin/bash
# Bannerlord Dedicated Server start script for Linux - Native MP (GameType NordInvasion)

SERVER_DIR="$(cd "$(dirname "$0")" && pwd)"
# Repo root is 2 levels up from DedicatedServer/Bannerlord/
REPO_ROOT="$(cd "$SERVER_DIR/../.." && pwd)"
cd "$REPO_ROOT"

echo "Starting Bannerlord Nord Invasion Dedicated Server (GameType NordInvasion)"
echo "Native MP, not Co-op mod - stable 32+ players like Full Invasion 3"
echo "Make sure token exists in Documents/Mount and Blade II Bannerlord/Tokens/ (customserver.gettoken)"
echo ""

# Find exe
DEDICATED_EXE="bin/Win64_Shipping_Server/DedicatedCustomServer.Starter.exe"
if [ ! -f "$DEDICATED_EXE" ]; then
  DEDICATED_EXE="bin/Win64_Shipping_Client/TaleWorlds.MountAndBlade.DedicatedCustomServer.exe"
fi
if [ ! -f "$DEDICATED_EXE" ]; then
  DEDICATED_EXE="TaleWorlds.MountAndBlade.DedicatedCustomServer.exe"
fi

if [ ! -f "$DEDICATED_EXE" ] && ! command -v wine &> /dev/null; then
  echo "Dedicated server exe not found! Download via SteamCMD:"
  echo "steamcmd +login anonymous +app_update 1058080 validate +quit"
  echo "Expected at: $DEDICATED_EXE"
  exit 1
fi

CONFIG="DedicatedServer/Bannerlord/DedicatedCustomServerConfig.xml"
# For ModKit, config should be in Modules/Native/
if [ -f "Modules/Native/ds_config_nordinvasion.txt" ]; then
  CONFIG="Modules/Native/ds_config_nordinvasion.txt"
fi

echo "Using exe: $DEDICATED_EXE"
echo "Using config: $CONFIG"
echo "Modules: Native*Multiplayer*NordInvasion"
echo ""

# Launch
if command -v wine &> /dev/null; then
  wine "$DEDICATED_EXE" _MODULES_*Native*Multiplayer*NordInvasion*_MODULES_ /dedicatedcustomserverconfigfile "$CONFIG"
else
  "$DEDICATED_EXE" _MODULES_*Native*Multiplayer*NordInvasion*_MODULES_ /dedicatedcustomserverconfigfile "$CONFIG"
fi
