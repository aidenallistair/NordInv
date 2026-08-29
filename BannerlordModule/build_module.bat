@echo off
echo Building Nord Invasion Better Edition for Bannerlord...

REM Set Bannerlord path - change to your path
set BANNERLORD_PATH=C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord

echo Restoring NuGet packages...
dotnet restore NordInvasion.csproj

echo Building with dotnet...
dotnet build NordInvasion.csproj -c Release

if %errorlevel% neq 0 (
  echo Build failed!
  pause
  exit /b
)

echo Copying to Modules...
xcopy /Y /I Modules\NordInvasion\bin\Win64_Shipping_Client\NordInvasion.dll "%BANNERLORD_PATH%\Modules\NordInvasion\bin\Win64_Shipping_Client\NordInvasion.dll"
xcopy /Y /I /E Modules\NordInvasion\ModuleData "%BANNERLORD_PATH%\Modules\NordInvasion\ModuleData"
xcopy /Y Modules\NordInvasion\SubModule.xml "%BANNERLORD_PATH%\Modules\NordInvasion\SubModule.xml"

echo Build complete! Enable mod in Launcher.
pause
