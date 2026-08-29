# Анализ мультиплеера для Nord Invasion: Co-op мод vs Встроенный MP

Дата: 2026-08-29

## Проблема

Сейчас в `BANNERLORD_PLAN_RU.md`, `LAUNCH_GUIDE.md` и `README.md` основной путь для игры с друзьями — **Bannerlord Co-op / Bannerlord Together**. Это моды, которые синхронизируют кампанию:

- Патчат десятки классов через Harmony
- Синхронизируют движение партий, сейвы, бои
- Держат отдельный сервер/сессию

**Почему они нестабильны:**
1. Кампания Bannerlord не проектировалась под сеть — каждый патч ломает Co-op
2. Синхронизация сейва: один клиент отстал → десинк → краш
3. Бои: Co-op переносит бой в отдельную миссию, но AI-логика кампании остаётся на хосте → рассинхрон
4. Моды поверх модов: ButterLib + UIExtenderEx + Co-op + NI = 4 слоя Harmony-патчей → конфликты
5. Ограничение 4-8 игроков, 400 юнитов максимум, иначе лаги/краши
6. Требует одинаковый порядок модов, одинаковые версии, ручную разблокировку DLL

Для **PvE wave-defense** (оборона форта от ботов) кампания вообще не нужна — нужен только бой.

## Встроенный мультиплеер Bannerlord

TaleWorlds даёт официальный путь для кастомных MP режимов:

### Что это
- **DedicatedCustomServer** (`Mount & Blade II Dedicated Server`, AppID 1058080 / 1863440)
- Сервер на UDP 7210, токен-авторизация (`customserver.gettoken`), видимость в браузере серверов
- Поддержка модов: `_MODULES_*Native*Multiplayer*NordInvasion*_MODULES_`
- Конфиг: `Modules/Native/ds_config_*.txt` или `DedicatedCustomServerConfig.xml`

### Как мод добавляет свой режим
По документации https://moddocs.bannerlord.com/multiplayer/custom_game_mode/ :

1. `SubModule.xml`:
```xml
<ModuleCategory value="Multiplayer" />
```
2. `SubModule.cs`:
```csharp
public override void OnSubModuleLoad() {
    Module.CurrentModule.AddMultiplayerGameMode(new NordInvasionGameMode("NordInvasion"));
}
```
3. `NordInvasionGameMode : MissionBasedMultiplayerGameMode`:
```csharp
public override void StartMultiplayerGame(string scene) {
    MissionState.OpenNew("NordInvasion", new MissionInitializerRecord(scene), missionController => new MissionBehavior[] {
        MissionLobbyComponent.CreateBehavior(),
        new MissionMultiplayerNordInvasion(), // Server
        new MissionMultiplayerNordInvasionClient(), // Client
        new MultiplayerTimerComponent(),
        new SpawnComponent(new NISpawnFrameBehavior(), new NISpawningBehavior()),
        // ... остальные стандартные компоненты
    });
}
```
4. Сервер-логика `MissionMultiplayerNordInvasion : MissionMultiplayerGameModeBase`:
- `AfterStart()`: создаёт 2 команды (Defender = игроки, Attacker = норды), ставит культуры/баннеры
- `HandleNewClientAfterSynchronized(peer)`: `peer.AddComponent<NIMissionRepresentative>()`
- `GetScoreForKill`, `CheckForMatchEnd`, `GetWinnerTeam`
- Интегрирует `NordInvasionWaveManagerBehavior`, `Director`, `Supply`, `FortressBuildManager`
- Спавн ботов через `Mission.SpawnAgent` на стороне сервера — движок сам синхронизирует агентов на клиентов

5. Клиент-логика `MissionMultiplayerNordInvasionClient : MissionMultiplayerGameModeBaseClient`
- HUD, звуки, UI

6. `NIMissionRepresentative : MissionRepresentativeBase` — хранит золото/ресурсы/перки игрока на сервере

Это ровно то, как работает **Full Invasion 3** — самый популярный PvE мод Bannerlord (120 игроков, волны, прокачка). FI3 не использует Co-op мод, а использует DedicatedCustomServer + кастомный GameType.

### Плюсы встроенного MP для NI
| Критерий | Co-op мод | Dedicated MP (встроенный) |
|----------|-----------|---------------------------|
| Стабильность | Низкая (десинки кампании) | Высокая (официальный неткод TaleWorlds) |
| Игроков | 4-8 (рекоменд.) | 32-120 (как FI3) |
| Производительность | Хост тянет всю кампанию | Сервер только бой, оптимизирован |
| Установка | Co-op мод + порядок + разблокировка DLL | Подписаться на мод в Workshop + зайти на сервер |
| Персистенция | Через сейв хоста | Через наш PHP backend (уже есть) |
| Строительство | Не синхронизируется | Сервер-авторитетно + кастомные net-сообщения |
| Совместимость | Ломается каждым патчем | Работает с 1.2.10 до 1.4.8 без изменений API |
| Админка | Нет | Встроенная web-панель + команды `list` |

### Минусы встроенного MP
- Нет кампании (но для NI она и не нужна — у нас своя мета-прогрессия через backend)
- Стройка требует кастомных сетевых сообщений (GameNetwork)
- Нужен токен для публичного сервера (генерируется раз в 3 месяца)
- Сцены должны быть в `SceneObj` Multiplayer модуля для авто-скачивания

### Что уже есть в репо под MP
- `SubModule.xml`: `MultiplayerModule=true`, 4 сцены `mp_ni_*`
- `Missions.xml`: `mp_nord_invasion`
- `DedicatedServer/Bannerlord/DedicatedCustomServerConfig.xml`: уже настроен под 32 игрока
- `DedicatedServer/Bannerlord/start_*.bat/.sh`: скрипты запуска
- `PersistenceManager`: backend для золота/прокачки (работает и на dedicated)
- `FortressBuildManager`: ставит пропсы через `Scene.LoadSceneProp` — в MP надо сделать сервер-авторитетным

Чего не хватало:
- Регистрации `AddMultiplayerGameMode`
- `MissionMultiplayerGameModeBase` / `MissionMultiplayerGameModeBaseClient`
- `MissionRepresentative`
- `SpawnComponent` + кастомные `SpawnFrameBehavior` / `SpawningBehavior`
- Синхронизации стройки

## Рекомендованное решение

**Перейти на встроенный MP как основной путь, Co-op оставить как опциональный fallback.**

### Архитектура

```
Client (Bannerlord) <--UDP 7210--> DedicatedCustomServer (NordInvasion GameType)
                                          |
                                          +--> WaveManager (server authoritative)
                                          +--> Director, Supply, Morale
                                          +--> FortressBuildManager (server spawns, broadcast)
                                          +--> PersistenceManager -> PHP backend
                                          +--> MissionRepresentative (gold per peer)

Scenes: mp_ni_bridge_01, mp_ni_town_01, mp_ni_castle_01, mp_ni_forest_01
GameType: NordInvasion (вместо Multiplayer)
MaxPlayers: 32 (настраивается)
```

### Поток матча
1. Админ запускает `DedicatedCustomServer.Starter.exe _MODULES_*Native*Multiplayer*NordInvasion*_MODULES_ /dedicatedcustomserverconfigfile DedicatedCustomServerConfig.xml`
2. Сервер: `ServerName`, `GameType NordInvasion`, `start_game_and_mission`
3. Игроки: Multiplayer -> Custom Servers -> находят сервер (или Direct IP)
4. Лобби: все в Defender, выбирают класс (Medic/Engineer и т.д.)
5. Сервер: `AfterStart()` → `SetupWave(1)` → спавн ботов
6. Волны: сервер считает `BotsAlive`, клиенты получают HUD через `MissionRepresentative`
7. Стройка: клиент жмёт B → запрос на сервер → сервер проверяет ресурсы → спавнит пропс → broadcast всем
8. Победа/поражение: `Mission.EndMission()` → `enable_automated_battle_switching` → следующая карта

### Сетевая синхронизация стройки (важно)

В Bannerlord MP движок синхронизирует только агентов. Динамические `GameEntity` (баррикады) надо синхронизировать вручную:

**Вариант A (простой, как в FI3 v1):**
- Только сервер может ставить пропсы (`GameNetwork.IsServer`)
- Клиент шлёт `RequestBuildMessage(BuildType, Pos, Yaw)` через `GameNetwork.BeginModuleEventAsClient`
- Сервер валидирует, спавнит, шлёт `BuildPlacedMessage(propId, Pos, Yaw)` всем клиентам
- Клиенты спавнят у себя такой же пропс

**Вариант B (продвинутый):**
- Использовать `MissionNetwork` + `UsableMachine` sync (как осадные орудия в Native)
- Требует `DedicatedServerType` = `custom` и кастомных `NetworkMessages`

Для MVP — Вариант A, 100 строк кода.

### Персистенция

Уже реализовано:
- `PersistenceManager.BackendUrl / ApiSecret` читаются из env или `NISettings`
- `POST /api/kill`, `/api/wave/complete`, `/api/shop/buy` — работают на dedicated
- На dedicated сервере нет `MainAgent` в лобби — надо логин по `MissionPeer`

Изменение: в `PersistenceManager.OnAgentBuild` уже есть `NIPeers.GetSteamId` — он работает и в MP (через `Peer.Id`).

### Что сделать (план)

1. **SubModule.xml**: добавить `<ModuleCategory value="Multiplayer"/>`, `<SubModule><Tags><Tag key="DedicatedServerType" value="Battle"/>` (было none)
2. **SubModule.cs**: в `OnSubModuleLoad` добавить `Module.CurrentModule.AddMultiplayerGameMode(new NordInvasionGameMode("NordInvasion"))`
3. Создать папку `Multiplayer/`:
   - `NordInvasionGameMode.cs` — регистрация
   - `MissionMultiplayerNordInvasion.cs` — сервер, наследует `MissionMultiplayerGameModeBase`
   - `MissionMultiplayerNordInvasionClient.cs` — клиент
   - `NIMissionRepresentative.cs`
   - `NISpawnFrameBehavior.cs`, `NISpawningBehavior.cs`
   - `NINetworkMessages.cs` — `RequestBuildMessage`, `BuildPlacedMessage`, `GoldSyncMessage`
4. **DedicatedCustomServerConfig.xml**: `GameType` = `NordInvasion`
5. **ModuleData/multiplayer_strings.xml**: строки для имени режима
6. **FortressBuildManager**: добавить `TryPlaceMP(peer, type)` — сервер-авторитетно
7. Доки: обновить `README.md`, `LAUNCH_GUIDE.md` — секция Dedicated Server становится основной, Co-op — альтернативой

### Совместимость

- Старый путь Custom Battle (Singleplayer) остаётся — `OnMissionBehaviorInitialize` по `mp_ni_*` всё ещё работает
- Co-op мод остаётся как опция для 4 друзей без выделенного сервера
- Новый путь MP — основной для 8-32 игроков

## Вывод

**Лучшая возможность — встроенный DedicatedCustomServer с кастомным GameType `NordInvasion`, как у Full Invasion 3.**

Причины:
- Проверено 100k+ игроков FI3, стабильно 64+ игрока
- Не требует нестабильного Co-op мода
- Использует официальный неткод TaleWorlds (UDP, лаг-компенсация, анти-чит)
- Легко хостить: SteamCMD + токен + 1 конфиг
- Персистенция уже готова (PHP backend)
- Стройка решается 1 сетевым сообщением

Co-op мод стоит оставить только как fallback для тех, кто хочет играть без выделенного сервера (2-4 друга, P2P).

## Ссылки

- https://moddocs.bannerlord.com/multiplayer/hosting_server/
- https://moddocs.bannerlord.com/multiplayer/custom_game_mode/ (пример BountyMP)
- https://github.com/Bannerlord-Community-Mods/TestGameMode (пример PeaceGame)
- Full Invasion 3: https://www.moddb.com/mods/full-invasion-3 (архитектура: DedicatedCustomServer + custom GameMode)
- Bannerlord Coop vs Dedicated: https://www.redswitches.com/blog/bannerlord-dedicated-server-setup-guide/ — "Dedicated servers host custom multiplayer sessions only. They do not host campaign co-op"
