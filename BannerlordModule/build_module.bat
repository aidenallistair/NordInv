@echo off
echo Building Nord Invasion Better Edition for Bannerlord...

REM Set Bannerlord path - change to your path or set env BANNERLORD_PATH
if "%BANNERLORD_PATH%"=="" set BANNERLORD_PATH=C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord

echo BannerlordPath=%BANNERLORD_PATH%

if exist "%BANNERLORD_PATH%\bin\Win64_Shipping_Client\TaleWorlds.Core.dll" (
  echo Found Bannerlord at %BANNERLORD_PATH% -> building ModKit version
  dotnet build NordInvasion.ModKit.csproj -c Release -p:BannerlordPath="%BANNERLORD_PATH%"
) else (
  echo Bannerlord not found at %BANNERLORD_PATH% -> building NuGet version
  echo To build ModKit version, set BANNERLORD_PATH env or edit this bat
  dotnet restore NordInvasion.csproj
  dotnet build NordInvasion.csproj -c Release
)

if %errorlevel% neq 0 (
  echo Build failed!
  pause
  exit /b
)

echo Copying to Modules...
if exist "%BANNERLORD_PATH%\Modules\NordInvasion" (
  if not exist "%BANNERLORD_PATH%\Modules\NordInvasion\bin\Win64_Shipping_Client" mkdir "%BANNERLORD_PATH%\Modules\NordInvasion\bin\Win64_Shipping_Client"
  if not exist "%BANNERLORD_PATH%\Modules\NordInvasion\bin\Win64_Shipping_Server" mkdir "%BANNERLORD_PATH%\Modules\NordInvasion\bin\Win64_Shipping_Server"
  xcopy /Y /I Modules\NordInvasion\bin\Win64_Shipping_Client\NordInvasion.dll "%BANNERLORD_PATH%\Modules\NordInvasion\bin\Win64_Shipping_Client\NordInvasion.dll" 2>nul
  xcopy /Y /I Modules\NordInvasion\bin\Win64_Shipping_Server\NordInvasion.dll "%BANNERLORD_PATH%\Modules\NordInvasion\bin\Win64_Shipping_Server\NordInvasion.dll" 2>nul
  if not exist "%BANNERLORD_PATH%\Modules\NordInvasion\bin\Win64_Shipping_Server\NordInvasion.dll" (
    xcopy /Y /I Modules\NordInvasion\bin\Win64_Shipping_Client\NordInvasion.dll "%BANNERLORD_PATH%\Modules\NordInvasion\bin\Win64_Shipping_Server\NordInvasion.dll" 2>nul
  )
  xcopy /Y /I /E Modules\NordInvasion\ModuleData "%BANNERLORD_PATH%\Modules\NordInvasion\ModuleData" 2>nul
  xcopy /Y Modules\NordInvasion\SubModule.xml "%BANNERLORD_PATH%\Modules\NordInvasion\SubModule.xml" 2>nul
  echo Copied to %BANNERLORD_PATH%\Modules\NordInvasion
) else (
  echo Bannerlord Modules folder not found, DLL in Modules\NordInvasion\bin\
)

echo Build complete! For ModKit see docs\MODKIT_GUIDE_RU.md
echo Enable mod in Launcher. For Dedicated MP see docs\MULTIPLAYER_ANALYSIS_RU.md
pause
