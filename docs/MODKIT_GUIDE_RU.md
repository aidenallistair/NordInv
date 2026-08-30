# Nord Invasion — сборка под ModKit и нативный MP

Этот гайд — как собрать мод **прямо под ModKit** (без NuGet `Bannerlord.ReferenceAssemblies`) и как правильно подключить нативный мультиплеер.

## 1. Что такое ModKit и зачем он

**ModKit** = официальные инструменты Bannerlord (Steam → Библиотека → Инструменты → `Mount & Blade II: Bannerlord - Modding Kit`). Включает:
- Scene Editor, Resource Browser, Shader Editor
- Исходники Native модулей как пример
- DLL игры в `bin/Win64_Shipping_Client/` — на них и компилится мод

Наш старый `NordInvasion.csproj` использует NuGet-пакет `Bannerlord.ReferenceAssemblies` — удобно для CI, но не для ModKit (там нет реальных `TaleWorlds.*.dll`). Поэтому добавлен второй проект `NordInvasion.ModKit.csproj`, который берёт DLL напрямую из установленной игры.

## 2. Структура для ModKit

```
Mount & Blade II Bannerlord/
  Modules/
    NordInvasion/
      SubModule.xml              # ModuleCategory=Multiplayer, DedicatedServerType=Battle
      bin/
        Win64_Shipping_Client/NordInvasion.dll   # клиент (и сингл)
        Win64_Shipping_Server/NordInvasion.dll   # сервер (копия клиента)
      ModuleData/
        Characters.xml, Items.xml, SceneProps.xml, Missions.xml
        MultiplayerScenes.xml, MultiplayerMaps.xml
        multiplayer_strings.xml   # имя режима в браузере
        Scenes/mp_ni_*/           # 4 карты (scene.xscene + terrain.bin после prepare)
        Languages/RU/...
      ...

BannerlordModule/                # исходники в репо
  NordInvasion.csproj            # для CI (NuGet)
  NordInvasion.ModKit.csproj     # для ModKit (HintPath)
  Modules/NordInvasion/...       # то что копируется в игру
  src/NordInvasion/
    SubModule.cs                 # регистрация GameType
    Multiplayer/
      NordInvasionGameMode.cs
      MissionMultiplayerNordInvasion.cs
      MissionMultiplayerNordInvasionClient.cs
      NIMissionRepresentative.cs
      NISpawnBehaviors.cs
      NINetworkMessages.cs
    Behaviors/, Managers/, ...
```

## 3. Быстрый старт под ModKit (Windows)

### 3.1 Требования
- Bannerlord 1.4.8 (Steam), без War Sails DLC
- ModKit (Steam → Tools)
- .NET SDK 4.7.2 + .NET 6 (для `dotnet build`) или Visual Studio 2022
- Без внешних модов (ButterLib/UIExtenderEx/MCM не требуются для v2.3.0)

### 3.2 Клонирование прямо в Modules

```bat
cd "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules"
git clone https://github.com/aidenallistair/NordInv.git NordInvasion_temp
xcopy /E /I NordInvasion_temp\BannerlordModule\Modules\NordInvasion NordInvasion
xcopy /E /I NordInvasion_temp\BannerlordModule\src src_build
```

Или просто скопируй `BannerlordModule/Modules/NordInvasion` в `.../Bannerlord/Modules/NordInvasion`.

### 3.3 Сборка DLL

#### Вариант A: dotnet CLI (рекомендуется)

```bat
cd "C:\...\Bannerlord\Modules\NordInvasion"
REM Укажи путь к игре если не стандартный
dotnet build ..\..\BannerlordModule\NordInvasion.ModKit.csproj -c Release -p:BannerlordPath="C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord"

REM DLL появится в:
REM Modules/NordInvasion/bin/Win64_Shipping_Client/NordInvasion.dll
REM и копия в Win64_Shipping_Server/
```

Скрипт `build_module.bat` уже умеет:
- Если находит `BANNERLORD_PATH` — собирает ModKit версию
- Если нет — собирает NuGet версию

```bat
set BANNERLORD_PATH=C:\...\Mount & Blade II Bannerlord
build_module.bat
```

#### Вариант B: Visual Studio / Rider

1. Открой `BannerlordModule/NordInvasion.ModKit.csproj`
2. В свойствах проекта проверь `BannerlordPath` (или задай env `BANNERLORD_PATH`)
3. Build → Release
4. DLL скопируется в `Modules/NordInvasion/bin/.../Client` и `Server`

### 3.4 Террейн для карт (один раз)

Сцены `mp_ni_*` сгенерированы в XML, но без бинарного террейна:

```bat
python tools/prepare_scenes.py
REM или
python tools/prepare_scenes.py --source mp_ye_battle_01
```

Скрипт копирует `terrain.bin`, `flora.bin`, `ShaderCache` из ванильной сцены в наши 4 карты. Без этого сцены не загрузятся.

Альтернатива: открой каждую сцену в Scene Editor (Launcher → Tools → Editor) и нажми Save — редактор сгенерит террейн и navmesh.

### 3.5 Проверка в игре

- Launcher → включи ButterLib, UIExtenderEx, MCMv5, NordInvasion
- **Singleplayer тест:** Custom Battle → Map `mp_ni_bridge_01` → Mission `mp_nord_invasion` → Start → должна появиться "Wave 1 preparing..."
- **MP тест (локально):** запусти Dedicated сервер (см. раздел 5) и подключись через Multiplayer → Custom Servers

Логи: `Documents/Mount and Blade II Bannerlord/Logs/rgl_log.txt`

## 4. Как устроен нативный MP в Bannerlord (правильный путь)

Дока: https://moddocs.bannerlord.com/multiplayer/custom_game_mode/

### 4.1 Идея

TaleWorlds даёт API для кастомных режимов, как TeamDeathmatch, Siege и т.д. Мод регистрирует свой `GameType`, сервер запускает миссию с твоими `MissionBehavior`.

Это использует **Full Invasion 3** — самый популярный PvE мод (32-120 игроков).

### 4.2 Минимальный пример

**SubModule.xml:**
```xml
<Module>
  <Id>NordInvasion</Id>
  <ModuleCategory value="Multiplayer"/> <!-- важно: грузится и на клиенте и на сервере -->
  <SubModules>
    <SubModule>
      <DLLName>NordInvasion.dll</DLLName>
      <SubModuleClassType>NordInvasion.SubModule</SubModuleClassType>
      <Tags>
        <Tag key="DedicatedServerType" value="Battle"/> <!-- сервер с AI -->
        <Tag key="IsNoRenderOnlyAIs" value="false"/>
      </Tags>
    </SubModule>
  </SubModules>
</Module>
```

**SubModule.cs:**
```csharp
protected override void OnSubModuleLoad() {
    Module.CurrentModule.AddMultiplayerGameMode(new NordInvasionGameMode("NordInvasion"));
}
protected override void OnGameStart(Game game, IGameStarter starter) {
    game.GameTextManager.LoadGameTexts(ModuleHelper.GetModuleFullPath("NordInvasion") + "ModuleData/multiplayer_strings.xml");
}
```

**multiplayer_strings.xml:**
```xml
<strings>
  <string id="str_multiplayer_official_game_type_name.NordInvasion" text="{=*}Nord Invasion"/>
  <string id="str_multiplayer_game_type.NordInvasion" text="{=*}Nord Invasion - Defend Swadia"/>
</strings>
```

**GameMode:**
```csharp
public class NordInvasionGameMode : MissionBasedMultiplayerGameMode {
    public NordInvasionGameMode(string name) : base(name) {}
    public override void StartMultiplayerGame(string scene) {
        bool isServer = GameNetwork.IsServerOrRecorder;
        if (isServer) {
            MissionState.OpenNew(Name, new MissionInitializerRecord(scene), controller => new MissionBehavior[] {
                MissionLobbyComponent.CreateBehavior(),
                new MissionMultiplayerNordInvasion(),        // сервер
                new MissionMultiplayerNordInvasionClient(),  // клиент часть
                new MultiplayerTimerComponent(),
                new SpawnComponent(new NISpawnFrameBehavior(), new NISpawningBehavior()),
                new MissionLobbyEquipmentNetworkComponent(),
                new MultiplayerTeamSelectComponent(),
                new MissionHardBorderPlacer(),
                new MissionBoundaryPlacer(),
                new MissionBoundaryCrossingHandler(),
                new MultiplayerPollComponent(),
                new MultiplayerAdminComponent(),
                new MultiplayerGameNotificationsComponent(),
                new MissionOptionsComponent(),
                new MissionScoreboardComponent(new NIScoreboardData()),
                new MissionAgentPanicHandler(),
                new AgentHumanAILogic(),
                new EquipmentControllerLeaveLogic(),
                new MultiplayerPreloadHelper(),
                // наши
                new NordInvasionWaveManagerBehavior(),
                new NordInvasionDirectorBehavior(),
                new FortressBuildManager(),
                new PersistenceManager(),
            });
        } else {
            MissionState.OpenNew(Name, new MissionInitializerRecord(scene), controller => new MissionBehavior[] {
                MissionLobbyComponent.CreateBehavior(),
                new MissionMultiplayerNordInvasionClient(),
                new MultiplayerAchievementComponent(),
                new MultiplayerTimerComponent(),
                new MultiplayerMissionAgentVisualSpawnComponent(),
                // ...
                new MissionScoreboardComponent(new NIScoreboardData()),
            });
        }
    }
}
```

**Server Behavior:**
```csharp
public class MissionMultiplayerNordInvasion : MissionMultiplayerGameModeBase {
    public override bool IsGameModeUsingOpposingTeams => true;
    public override MissionLobbyComponent.MultiplayerGameType GetMissionType() => MissionLobbyComponent.MultiplayerGameType.Battle;

    public override void AfterStart() {
        var defenderCulture = MBObjectManager.Instance.GetObject<BasicCultureObject>("vlandia");
        var attackerCulture = MBObjectManager.Instance.GetObject<BasicCultureObject>("sturgia");
        var defenderBanner = new Banner(defenderCulture.BannerKey, ...);
        var attackerBanner = new Banner(attackerCulture.BannerKey, ...);

        if (Mission.Teams.Count == 0) {
            Mission.Teams.Add(BattleSideEnum.Defender, ..., defenderBanner, true, false, true);
            Mission.Teams.Add(BattleSideEnum.Attacker, ..., attackerBanner, true, false, true);
        }
        Mission.GetMissionBehavior<NordInvasionWaveManagerBehavior>()?.SetupWave(1);
    }

    protected override void HandleNewClientAfterSynchronized(NetworkCommunicator peer) {
        if (peer.GetComponent<NIMissionRepresentative>() == null)
            peer.AddComponent<NIMissionRepresentative>();
    }

    public override int GetScoreForKill(Agent killed) => killed.Character.StringId.Contains("chieftain") ? 10 : 1;
    public override bool CheckForMatchEnd() => waveMgr.State == Failed || waveMgr.WaveNumber >= VictoryWave;
    public override Team GetWinnerTeam() => waveMgr.State == Failed ? Attacker : Defender;
}
```

**Representative:**
```csharp
public class NIMissionRepresentative : MissionRepresentativeBase {
    public int Wood, Metal;
    public NIMissionRepresentative() { Gold = 500; }
}
```

**Spawn:**
```csharp
public class NISpawnFrameBehavior : SpawnFrameBehaviorBase {
    public override MatrixFrame GetSpawnFrame(Team team, bool hasMount, bool isInitial) {
        // 0-31 defender, 32-63 attacker
        var points = team.Side == Defender ? 0..31 : 32..63;
        var ep = Mission.Current.GetEntryPoint(randomInRange);
        return ep.GetGlobalFrame();
    }
}
```

**Network Messages (стройка):**
```csharp
public class RequestBuildMessage : GameNetworkMessage {
    public BuildType BuildType; public Vec3 Position; public float Yaw;
    protected override void OnWrite() {
        WriteIntToPacket((int)BuildType, new CompressionInfo.Integer(0,32,true));
        WriteIntToPacket((int)(Position.x*10), new CompressionInfo.Integer(-100000,100000,true));
        // ...
    }
}
```
Клиент: `GameNetwork.BeginModuleEventAsClient() / WriteMessage() / EndModuleEventAsClient()`
Сервер: `BeginBroadcastModuleEvent() / WriteMessage() / EndBroadcastModuleEvent()`

Регистрация хендлеров через `AddRemoveMessageHandlers` контейнер (в разных версиях API отличается, поэтому в нашем коде есть reflection fallback).

### 4.3 Команды

В Bannerlord MP команды Defender (игроки) и Attacker (боты). Мы не даём игрокам переходить в Attacker (в `OnPeerChangedTeam` возвращаем в Defender). Боты спавнятся через `Mission.SpawnAgent(...Team(attackerTeam))` — движок сам синхронизирует их на клиентов.

Золото — через `NIMissionRepresentative.Gold`, wood/metal — свои поля. Покупки — сервер-авторитетно (проверка цены на сервере).

### 4.4 Сцены

Сцены должны быть в `ModuleData/Scenes/mp_ni_*` и зарегистрированы в `MultiplayerScenes.xml` + `MultiplayerMaps.xml`. Для авто-скачивания игроками — положить `SceneObj` в `Modules/NordInvasion/SceneObj/` (фича Native: игрокам предложат скачать недостающие карты).

Entry points:
- 0-31: игроки (западный форт)
- 32-63: норды (кольцо вокруг)
- 64: босс

## 5. Запуск Dedicated сервера (нативный MP)

### 5.1 Токен

1. Bannerlord → Multiplayer → лобби → консоль Alt+~ → `customserver.gettoken`
2. Файл в `Documents/Mount & Blade II Bannerlord/Tokens/` (действует 3 месяца)
3. Скопируй на сервер если хостишь на другой машине, или передай через `/dedicatedcustomserverauthtoken`

### 5.2 Конфиг

`DedicatedServer/Bannerlord/DedicatedCustomServerConfig.xml` уже готов:

```xml
<GameType>NordInvasion</GameType>
<Map>mp_ni_bridge_01</Map>
<Maps>
  <Map>mp_ni_town_01</Map>...
</Maps>
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

### 5.3 Запуск

```bat
# Windows
bin\Win64_Shipping_Server\DedicatedCustomServer.Starter.exe _MODULES_*Native*Multiplayer*NordInvasion*_MODULES_ /dedicatedcustomserverconfigfile DedicatedCustomServerConfig.xml

# Linux via Wine
wine .../DedicatedCustomServer.exe _MODULES_*Native*Multiplayer*NordInvasion*_MODULES_ /dedicatedcustomserverconfigfile DedicatedCustomServerConfig.xml
```

Скрипты `start_bannerlord_server.bat/.sh` уже делают это.

Должно написать:
```
Nord Invasion MP GameType 'NordInvasion' registered
Dedicated Server started on port 7240
```

Порты: UDP 7240 (игроки), TCP 7210 (web-панель, AdminPassword из конфига).

### 5.4 Подключение

- Launcher → включи NordInvasion
- Multiplayer → Custom Servers → фильтр `NordInvasion` → твой сервер
- Или консоль: `connect_to_server IP 7240`

## 6. Сборка под ModKit — чеклист

- [ ] Bannerlord + ModKit установлены
- [ ] `BANNERLORD_PATH` прописан или стандартный `C:\...\Bannerlord`
- [ ] `dotnet build NordInvasion.ModKit.csproj -c Release` → DLL в `bin/Client` и `bin/Server`
- [ ] `python tools/prepare_scenes.py` → terrain.bin
- [ ] Launcher → галочки ButterLib, UIExtenderEx, MCMv5, NordInvasion
- [ ] Custom Battle `mp_ni_bridge_01` → "Wave 1 preparing..."
- [ ] Dedicated сервер с токеном → виден в браузере
- [ ] 2 клиента подключаются, волны идут, стройка через B работает

## 7. Частые проблемы ModKit

**`Could not find TaleWorlds.Core.dll`** → проверь `BannerlordPath`, или задай `BANNERLORD_PATH` env.

**`The type or namespace 'MissionMultiplayerGameModeBase' could not be found`** → убедись что зависимость `Multiplayer` в `SubModule.xml` и что собираешь против Client DLL (там есть MP типы), а не Server (там часть типов отсутствует).

**Сцены не грузятся** → нет `terrain.bin` → `prepare_scenes.py`.

**Сервер не виден** → нет токена или UDP 7240 закрыт. Проверь `%programdata%\Mount and Blade II Bannerlord\logs\`.

**Стройка не синхронизируется** → в MVP клиент шлёт запрос, сервер ставит. Если хендлер не зарегистрировался (разные версии API) — стройка будет только на сервере. Смотри `MissionMultiplayerNordInvasion.AddRemoveMessageHandlers` — там reflection fallback.

## 8. Ссылки

- https://moddocs.bannerlord.com/multiplayer/hosting_server/
- https://moddocs.bannerlord.com/multiplayer/custom_game_mode/ (BountyMP пример)
- https://github.com/Bannerlord-Community-Mods/TestGameMode (PeaceGame)
- Full Invasion 3: https://www.moddb.com/mods/full-invasion-3 (архитектура Dedicated)
- Наш анализ: `docs/MULTIPLAYER_ANALYSIS_RU.md`
