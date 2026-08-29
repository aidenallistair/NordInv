#!/bin/bash
# Bannerlord Dedicated Server start script for Linux

SERVER_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SERVER_DIR/../.."

echo "Starting Bannerlord Nord Invasion Dedicated Server"

# Check if dedicated server exists
if [ ! -f "bin/Win64_Shipping_Client/TaleWorlds.MountAndBlade.DedicatedCustomServer.exe" ]; then
  echo "Dedicated server exe not found! Download via SteamCMD:"
  echo "steamcmd +login anonymous +app_update 1058080 validate +quit"
  exit 1
fi

# Start with Wine if on Linux
if command -v wine &> /dev/null; then
  wine bin/Win64_Shipping_Client/TaleWorlds.MountAndBlade.DedicatedCustomServer.exe /dedicatedcustomserverconfig DedicatedServer/Bannerlord/DedicatedCustomServerConfig.xml
else
  ./bin/Win64_Shipping_Client/TaleWorlds.MountAndBlade.DedicatedCustomServer.exe /dedicatedcustomserverconfig DedicatedServer/Bannerlord/DedicatedCustomServerConfig.xml
fi
