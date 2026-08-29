@echo off
echo Starting Bannerlord Nord Invasion Dedicated Server (GameType NordInvasion)...
echo This uses native MP, not Co-op mod - stable 32+ players like Full Invasion 3
echo Make sure token is generated: Bannerlord MP lobby -> Alt+~ -> customserver.gettoken
echo.

REM Path to Dedicated Server exe - adjust if needed
set DEDICATED_EXE=bin\Win64_Shipping_Server\DedicatedCustomServer.Starter.exe
if not exist "%DEDICATED_EXE%" set DEDICATED_EXE=bin\Win64_Shipping_Client\TaleWorlds.MountAndBlade.DedicatedCustomServer.exe
if not exist "%DEDICATED_EXE%" set DEDICATED_EXE=TaleWorlds.MountAndBlade.DedicatedCustomServer.exe

REM Config file - should be in Modules/Native/ or same folder
REM Copy DedicatedCustomServerConfig.xml to Modules/Native/ds_config_nordinvasion.txt for auto-detection
set CONFIG=DedicatedCustomServerConfig.xml
if not exist "%CONFIG%" set CONFIG=Modules\Native\ds_config_nordinvasion.txt

:loop
echo Launching: %DEDICATED_EXE% _MODULES_*Native*Multiplayer*NordInvasion*_MODULES_ /dedicatedcustomserverconfigfile %CONFIG%
%DEDICATED_EXE% _MODULES_*Native*Multiplayer*NordInvasion*_MODULES_ /dedicatedcustomserverconfigfile %CONFIG%
echo Server stopped or crashed, restarting in 5 sec... Check logs in %%programdata%%\Mount and Blade II Bannerlord\logs\
timeout 5
goto loop
