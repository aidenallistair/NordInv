# Bannerlord Dedicated Server для Nord Invasion

## Как скачать Dedicated Server

### SteamCMD (рекомендуется)

```bash
sudo apt install steamcmd
steamcmd +login anonymous +app_update 1058080 validate +quit
# Путь: ~/.steam/steam/steamapps/common/Mount & Blade II Dedicated Server/
```

Или сам Bannerlord (там есть DedicatedCustomServer.exe):
```bash
steamcmd +login anonymous +app_update 261550 validate +quit
```

### Steam клиент

Library -> Tools -> Mount & Blade II Dedicated Server -> Install

## Запуск сервера Nord Invasion

1. Скопируй модуль `Modules/NordInvasion` в папку сервера `Modules/`

2. Конфиг `DedicatedCustomServerConfig.xml` уже готов в этой папке:

```xml
<GameType>NordInvasion</GameType>
<Map>mp_ni_bridge_01</Map>
<MaxPlayers>32</MaxPlayers>
<Port>7240</Port>
```

3. Запуск:

```bash
# Windows
TaleWorlds.MountAndBlade.DedicatedCustomServer.exe /dedicatedcustomserverconfig DedicatedServer/Bannerlord/DedicatedCustomServerConfig.xml

# Linux via Wine
wine DedicatedCustomServer.exe /dedicatedcustomserverconfig DedicatedServer/Bannerlord/DedicatedCustomServerConfig.xml
```

Или через скрипт `start_bannerlord_server.bat/.sh` в этой папке.

## Примечание

Dedicated Server бинарники не включены в репо из-за размера (~5GB). Скачай через SteamCMD и положи рядом. Если хочешь добавить в репо - используй Git LFS.

## Персистентность (MySQL + PHP) на том же хосте

Сохранение золота/прокачки между забегами — бэкенд из `src/backend-php/`
(PHP 7.4+ + MySQL). Ставится на эту же машину (гайд: `src/backend-php/README.md`):

1. `mysql -e "CREATE DATABASE nordinv CHARACTER SET utf8mb4;"` + юзер БД
2. скопировать `src/backend-php/` в веб-контент (nginx :80 или IIS :8080)
3. `config.php`: DB_* + `API_SECRET`
4. `php install.php` → схема + начальные данные
5. `bash tests/smoke.sh http://<host> <API_SECRET>`
6. в моде: `PersistenceManager.BackendUrl = "http://127.0.0.1:8080";`
   `PersistenceManager.ApiSecret = "<API_SECRET>";` → пересобрать dll

Полное описание: `docs/BACKEND_PHP.md`.
