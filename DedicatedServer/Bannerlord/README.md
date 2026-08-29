# Bannerlord Dedicated Server для Nord Invasion

Для Bannerlord MP мода нужен Dedicated Server.

## Как скачать Dedicated Server (не удалось скачать в sandbox из-за блокировки TaleWorlds CDN)

### Вариант 1: SteamCMD (рекомендуется)

```bash
# Установи steamcmd
sudo apt install steamcmd

# Скачай Bannerlord Dedicated Server
steamcmd +login anonymous +app_update 1058080 validate +quit
# Или сам Bannerlord (там есть DedicatedCustomServer.exe)
steamcmd +login anonymous +app_update 261550 validate +quit
```

Путь после скачки:
- `~/.steam/steam/steamapps/common/Mount & Blade II Dedicated Server/`
- Или `.../common/Mount & Blade II Bannerlord/bin/Win64_Shipping_Client/TaleWorlds.MountAndBlade.DedicatedCustomServer.exe`

### Вариант 2: Steam клиент

В Steam: Library -> Tools -> Mount & Blade II Dedicated Server -> Install

### Вариант 3: Скачать вручную

Ссылки (могут не работать из-за CDN):
- https://download.taleworlds.com/mb_warband_dedicated_1174.zip (Warband, для старого мода)
- Для Bannerlord: https://store.steampowered.com/app/1058080/Mount__Blade_II_Dedicated_Server/

Если ссылки не работают - попроси меня, я дам инструкцию как загрузить твои файлы в репо.

## Запуск сервера Bannerlord Nord Invasion

1. Скопируй модуль `Modules/NordInvasion` в папку сервера `Modules/`

2. Создай конфиг `DedicatedCustomServerConfig.xml`:

```xml
<Config>
  <GameType>mp_ni_bridge_01</GameType>
  <Map>mp_ni_bridge_01</Map>
  <GameMode>NordInvasion</GameMode>
  <MaxPlayers>32</MaxPlayers>
  <Port>7240</Port>
  <Modules>
    <Module Id="Native"/>
    <Module Id="SandBoxCore"/>
    <Module Id="CustomBattle"/>
    <Module Id="NordInvasion"/>
  </Modules>
</Config>
```

3. Запуск:

```bash
# Windows
TaleWorlds.MountAndBlade.DedicatedCustomServer.exe /dedicatedcustomserverconfig DedicatedCustomServerConfig.xml

# Linux via Wine
wine DedicatedCustomServer.exe /dedicatedcustomserverconfig DedicatedCustomServerConfig.xml
```

4. Или через скрипт `start_bannerlord_server.bat/.sh` в этой папке.

## Warband Dedicated Server (для старого мода)

Если хочешь поднять старый Warband Fianna сервер:

- Скачай `mb_warband_dedicated_1174.zip` с https://www.taleworlds.com/en/Games/Warband/Download (Other Downloads -> Dedicated Server)
- Распакуй, добавь WSE как в `ServerConfig/`
- Используй `nordinvasion.cfg` из `ServerConfig/`

Я не смог скачать его в этом sandbox - TaleWorlds CDN блокирует запросы из этого окружения (SSL_ERROR_SYSCALL). Если ты скачаешь файл локально, загрузи его в папку `DedicatedServer/Warband/` и я добавлю его в репо.

## Что загрузить в репо

Пользователь может загрузить:

- `DedicatedServer/Warband/mb_warband_dedicated.exe`
- `DedicatedServer/Warband/Modules/Native/` (минимум)
- `DedicatedServer/Bannerlord/TaleWorlds.MountAndBlade.DedicatedCustomServer.exe`

Но не загружай весь Bannerlord (30GB) - только Dedicated Server (~5GB).

Для GitHub лучше использовать Git LFS для больших бинарников.
