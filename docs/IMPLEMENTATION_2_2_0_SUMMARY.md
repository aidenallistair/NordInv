# Nord Invasion 2.2.0 — переход на встроенный MP: что сделано

## Задача
В доках указан Co-op мод (Bannerlord Coop / Together), но он нестабилен. Почему бы не использовать встроенный мультиплеер? Изучить возможности, найти лучшую.

## Исследование (docs/MULTIPLAYER_ANALYSIS_RU.md)

### Текущий путь: Co-op мод
- Синхронизирует кампанию через Harmony-патчи десятков классов
- Держит отдельный сервер/сессию, переносит бои в MP миссию
- Проблемы: десинк сейва, краш при реконнекте, 4-8 игроков максимум, ломается каждым патчем Bannerlord, требует одинаковый порядок модов и ручную разблокировку DLL
- Для wave-defense кампания не нужна — нужен только бой

### Встроенный путь: DedicatedCustomServer
- Официальный инструмент TaleWorlds (AppID 1058080), UDP 7210, токен-авторизация `customserver.gettoken`, браузер серверов, web-панель
- Поддержка модов: `_MODULES_*Native*Multiplayer*ModName*_MODULES_`
- Кастомный GameType: мод регистрирует режим через `Module.CurrentModule.AddMultiplayerGameMode(new MyGameMode("MyGameType"))`
- GameMode: `MissionBasedMultiplayerGameMode` → `StartMultiplayerGame(scene)` → список `MissionBehavior`
- Серверная логика: `MissionMultiplayerGameModeBase` — команды, `HandleNewClientAfterSynchronized` → `AddComponent<MissionRepresentative>`, спавн ботов через `Mission.SpawnAgent` (синхронизируется движком)
- Клиентская логика: `MissionMultiplayerGameModeBaseClient` — HUD, звуки
- Представитель: `MissionRepresentativeBase` — золото/ресурсы на сервере
- Спавн: `SpawnComponent` + `SpawnFrameBehaviorBase` (где спавнить) + `SpawningBehaviorBase` (когда)
- Сетевые сообщения: `GameNetworkMessage` + `GameNetwork.BeginBroadcastModuleEvent() / WriteMessage() / EndBroadcastModuleEvent()`
- Проверенный пример: **Full Invasion 3** — 120 игроков PvE, волны, прокачка, использует именно этот путь

### Сравнение

| Критерий | Co-op | Dedicated MP |
|---|---|---|
| Стабильность | Низкая | Высокая (официальный неткод) |
| Игроков | 4-8 | 32-120 |
| Установка | Co-op мод + порядок + DLL unblock | Подписаться на мод + зайти на сервер |
| Персистенция | Сейв хоста | PHP backend (уже есть) |
| Стройка | Не синхронизируется | Сервер-авторитетно + сообщения |
| Совместимость | Ломается патчами | 1.2.10-1.4.8 без изменений |

**Вывод:** Лучшая — встроенный DedicatedCustomServer с GameType `NordInvasion`, как FI3. Co-op оставить fallback.

## Реализация 2.2.0

### SubModule.xml
- `ModuleCategory=Multiplayer` (был без категории)
- `DedicatedServerType=Battle` (был none) — сервер рендерит AI
- Добавлена зависимость `Multiplayer`
- Версия 2.2.0

### SubModule.cs
- `OnSubModuleLoad`: `AddMultiplayerGameMode(new NordInvasionGameMode("NordInvasion"))`
- `OnGameStart`: загрузка `multiplayer_strings.xml` для имени режима в браузере

### ModuleData/multiplayer_strings.xml
- `str_multiplayer_official_game_type_name.NordInvasion`, `str_multiplayer_game_type.NordInvasion`, `desc`

### DedicatedCustomServerConfig.xml
- `GameType=NordInvasion` (был Multiplayer)
- Добавлен `<Module Id="Multiplayer"/>`
- Комментарий про автопереключение карт

### Multiplayer/ (6 новых файлов)

**NordInvasionGameMode.cs**
- `MissionBasedMultiplayerGameMode`, `StartMultiplayerGame` — разные behaviors для сервера и клиента
- Сервер: Lobby, MP NordInvasion (server+client), Timer, SpawnComponent (NI), LobbyEquipment, TeamSelect, Border, Poll, Admin, Notifications, Options, Scoreboard (NI), Panic, HumanAI, LeaveLogic, Preload, + наши Wave/Director/Weather/Build/Persist/Squad/HUD
- Клиент: Lobby, MP Client, Achievement, Timer, VisualSpawn, LobbyEquipment, TeamSelect, Border, Poll, Admin, Notifications, Options, Scoreboard (NI), MatchHistory, LeaveLogic, RecentPlayers, Preload, HUD

**NIMissionRepresentative.cs**
- `MissionRepresentativeBase` с Gold/Wood/Metal/Kills/Deaths/BestWave/PlayerId/SteamId
- Сервер-авторитетное золото

**NISpawnBehaviors.cs**
- `NISpawnFrameBehavior`: 0-31 defender, 32-64 attacker, 64 boss
- `NISpawningBehavior`: респавн 10 сек, проверка респавн-волны
- `NIScoreboardData`: имена и цвета команд

**MissionMultiplayerNordInvasion.cs**
- `MissionMultiplayerGameModeBase`
- `AfterStart`: культуры vlandia/sturgia, баннеры, команды, `SetupWave(1)`
- `HandleNewClientAfterSynchronized`: добавить `NIMissionRepresentative`, логин в backend
- `HandleNewClientAfterLoadingFinished`: можно отправить WaveState
- `OnPeerChangedTeam`: все в Defender
- `GetScoreForKill/Assist`: по типу норда
- `OnAgentRemoved`: очки в Scoreboard, золото в Representative, `OnBotKilled` в WaveManager
- `CheckForMatchEnd`: победа 25 волн или поражение или все мертвы
- `GetWinnerTeam`
- `AddRemoveMessageHandlers`: регистрация `RequestBuildMessage` через reflection + fallback `IUdpNetworkHandler`

**MissionMultiplayerNordInvasionClient.cs**
- `MissionMultiplayerGameModeBaseClient`
- Регистрация `BuildPlacedMessage`, `GoldSync`, `WaveState`
- `OnBuildPlaced`: спавн пропса на клиенте через PropSpawner

**NINetworkMessages.cs**
- `NICompression`: Integer компрессии для buildType, gold, wave, pos, yaw (без зависимости от конкретных полей CompressionBasic)
- `RequestBuildMessage`: клиент->сервер, BuildType + Pos (x*10,y*10,z*10) + Yaw*100
- `BuildPlacedMessage`: сервер->все, propId + fallback + pos + yaw
- `GoldSyncMessage`, `WaveStateMessage`

### FortressBuildManager.cs
- `TryPlace`: если `GameNetwork.IsClient` — шлёт `RequestBuildMessage`, иначе ставит локально
- `Place`: если `IsServer` — бродкаст `BuildPlacedMessage`
- `TryPlaceMP`: сервер-авторитетно, SpendMP через Representative, спавн + broadcast
- Вспомогательные `GetPropIdFor`, `GetFallbackFor`, `GetComponentFor`

### tools/validate_module.py
- Игнор `multiplayer_strings.xml` (грузится вручную)
- GameType разрешён `NordInvasion` или `Multiplayer`

### Доки
- `MULTIPLAYER_ANALYSIS_RU.md` — полный анализ
- `README.md` — Dedicated теперь основной, Co-op fallback, инструкция с токеном
- `LAUNCH_GUIDE.md` — Dedicated секция расширена (токен, _MODULES_, web-панель, что внутри MP), Co-op помечен как менее стабильный
- `DedicatedServer/Bannerlord/README.md` — токен, _MODULES_, что внутри
- `RELEASE_NOTES.md` — 2.2.0 секция

### Валидация
- `lint_csharp.py`: 0 ошибок, 1 предупреждение (OnTickAsAI — старый)
- `validate_module.py`: 0 ошибок, 12 варнингов (terrain.bin — нужен Bannerlord)
- `test_backend_api.py`: 66 ok
- `test_backend_sql.py`: 0 ошибок

## Как тестировать (нужен Bannerlord + Dedicated Server)

1. Сгенерить токен: MP лобби -> Alt+~ -> customserver.gettoken
2. Скопировать мод в Dedicated Server Modules/
3. Запустить: `DedicatedCustomServer.Starter.exe _MODULES_*Native*Multiplayer*NordInvasion*_MODULES_ /dedicatedcustomserverconfigfile DedicatedCustomServerConfig.xml`
4. Клиенты: включить NordInvasion в лаунчере, Multiplayer -> Custom Servers -> найти сервер
5. Проверить: волны спавнятся, золото даётся, стройка через B (клиент шлёт запрос, сервер ставит и бродкастит)

## Дальнейшие шаги

- Тест с 2 клиентами (нужен SteamCMD)
- Полная синхронизация стройки: сейчас MVP (запрос+бродкаст), можно добавить проверку коллизий на сервере
- Синхронизация перков/магазина через Representative + GoldSyncMessage
- Админ-команды: `!nextwave`, `!addgold` через ChatCommands
- Workshop: загрузить как Multiplayer mod

## Итог

Встроенный MP — лучший путь для NI: стабильно, 32+ игроков, официальный неткод, как у FI3. Co-op — только fallback.
