@echo off
echo Starting Bannerlord Nord Invasion Dedicated Server...
:loop
TaleWorlds.MountAndBlade.DedicatedCustomServer.exe /dedicatedcustomserverconfig DedicatedCustomServerConfig.xml
echo Server crashed, restarting in 5 sec...
timeout 5
goto loop
