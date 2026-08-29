#!/bin/bash
# Linux start script for Nord Invasion

SERVER_DIR="$(cd "$(dirname "$0")/.." && pwd)"
cd "$SERVER_DIR"

echo "Starting Fianna Nord Invasion Server (Linux/Wine)"

# Check wine
if ! command -v wineconsole &> /dev/null; then
  echo "Wine not found! Install: sudo apt install wine64"
  exit 1
fi

# Create screen session
SESSION="warband_ni"

if screen -list | grep -q "$SESSION"; then
  echo "Server already running in screen $SESSION"
  echo "Attach with: screen -r $SESSION"
  exit 0
fi

echo "Starting in screen session: $SESSION"
screen -dmS $SESSION bash -c "
  cd $SERVER_DIR
  wineconsole --backend=curses mb_warband_dedicated.exe -r nordinvasion.cfg -m Fianna_NordInvasion
"

echo "Server started in background screen: $SESSION"
echo "To view: screen -r $SESSION"
echo "To detach: Ctrl+A D"
echo "To stop: screen -X -S $SESSION quit"

# Alternative with WSE on Linux (if WSE Linux version available):
# screen -dmS $SESSION wine WSE/WSELoaderServer.exe -r nordinvasion.cfg -m Fianna_NordInvasion -p mb_warband_dedicated.exe
