@echo off
echo Starting Fianna Nord Invasion Server...
echo Checking WSE...

if not exist WSE\WSELoaderServer.exe (
  echo ERROR: WSE not found! Download WSE from forums.taleworlds.com
  echo Place WSELoaderServer.exe in WSE folder
  pause
  exit /b
)

:loop
echo.
echo [%date% %time%] Starting server...
WSE\WSELoaderServer.exe -r nordinvasion.cfg -m Fianna_NordInvasion -p mb_warband_dedicated.exe

echo.
echo Server stopped or crashed. Restarting in 5 seconds... Press Ctrl+C to stop.
timeout /t 5
goto loop
