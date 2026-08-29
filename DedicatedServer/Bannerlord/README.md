# Bannerlord Dedicated Server для Nord Invasion (основной путь с 2.2.0)

> С 2.2.0 NI использует встроенный MP с кастомным GameType `NordInvasion`, как Full Invasion 3.
> Это стабильнее Co-op мода (см. `docs/MULTIPLAYER_ANALYSIS_RU.md`).

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

### Токен (обязательно для публичного сервера)

Токен действует 3 месяца, привязан к аккаунту:

1. Запусти Bannerlord Multiplayer -> войди в лобби
2. Консоль Alt+~ -> `customserver.gettoken` -> Enter
3. Файл в `Documents\Mount & Blade II Bannerlord\Tokens\`
4. Если хостишь на другой машине — скопируй туда же

Без токена сервер не появится в браузере (но по Direct IP зайти можно).

## Запуск сервера Nord Invasion

1. Скопируй модуль `Modules/NordInvasion` в папку сервера `Modules/`

2. Конфиг `DedicatedCustomServerConfig.xml` уже готов в этой папке (2.2.0):

```xml
<GameType>NordInvasion</GameType> <!-- наш кастомный режим, регистрируется в SubModule.cs -->
<Map>mp_ni_bridge_01</Map>
<MaxPlayers>32</MaxPlayers>
<Port>7240</Port>
<Modules>
  <Module Id="Native"/>
  <Module Id="SandBoxCore"/>
  <Module Id="CustomBattle"/>
  <Module Id="Multiplayer"/>
  <Module Id="NordInvasion"/>
</Modules>
```

3. Запуск:

```bash
# Windows - с указанием модулей (рекомендуется)
bin/Win64_Shipping_Server/DedicatedCustomServer.Starter.exe _MODULES_*Native*Multiplayer*NordInvasion*_MODULES_ /dedicatedcustomserverconfigfile DedicatedCustomServerConfig.xml

# Или старый путь
TaleWorlds.MountAndBlade.DedicatedCustomServer.exe /dedicatedcustomserverconfig DedicatedServer/Bannerlord/DedicatedCustomServerConfig.xml

# Linux via Wine
wine DedicatedCustomServer.exe _MODULES_*Native*Multiplayer*NordInvasion*_MODULES_ /dedicatedcustomserverconfig DedicatedServer/Bannerlord/DedicatedCustomServerConfig.xml
```

Или через скрипт `start_bannerlord_server.bat/.sh` в этой папке.

Должно написать:
```
ServerName Fianna NordInvasion...
GameType NordInvasion
Nord Invasion MP GameType 'NordInvasion' registered
Dedicated Server started on port 7240
```

4. Порты: UDP 7240 для игроков, TCP 7210 для web-панели (AdminPassword из конфига)

5. Подключение игроков: Multiplayer -> Custom Servers -> фильтр NordInvasion -> ваш сервер
   Или консоль: `connect_to_server IP 7240`

## Что внутри MP режима (2.2.0)

- `SubModule.cs`: `Module.CurrentModule.AddMultiplayerGameMode(new NordInvasionGameMode("NordInvasion"))`
- `Multiplayer/NordInvasionGameMode.cs`: `StartMultiplayerGame` — сервер и клиент behaviors
- `MissionMultiplayerNordInvasion` (server): команды Defender/Attacker, WaveManager, золото через `NIMissionRepresentative`
- `MissionMultiplayerNordInvasionClient` (client): HUD, BuildPlacedMessage
- `NINetworkMessages.cs`: RequestBuildMessage (клиент->сервер), BuildPlacedMessage (сервер->все)
- `FortressBuildManager.TryPlaceMP`: сервер-авторитетная стройка

Архитектура как у Full Invasion 3: сервер спавнит ботов, синхронизирует агентов, стройка через кастомные сообщения.

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
   Или через env: `NI_BACKEND_URL`, `NI_API_SECRET`

Полное описание: `docs/BACKEND_PHP.md`, сравнение с Co-op: `docs/MULTIPLAYER_ANALYSIS_RU.md`.
