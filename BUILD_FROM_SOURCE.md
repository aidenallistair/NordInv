# Сборка NordInvasion из исходников (ModKit)

Этот пакет — source-релиз: `NordInvasion.dll` собирается из исходников, т.к. требует DLL игры.

С 2.2.0 есть два проекта:
- `NordInvasion.csproj` — для CI (NuGet `Bannerlord.ReferenceAssemblies`, без игры)
- `NordInvasion.ModKit.csproj` — для ModKit (берёт DLL из установленной игры, копирует в Client и Server bin)

## Быстрый старт (ModKit)

### 1. Требования
- **Bannerlord 1.4.8** (build 1193) + **Modding Kit** (Steam → Library → Tools → `Mount & Blade II: Bannerlord - Modding Kit`)
- .NET SDK 8.0+ (`dotnet --version`) или Visual Studio 2022 / Rider
- **Без War Sails / морского DLC**: модуль не использует War Sails и не требует его.
- **Без внешних модов**: ButterLib/UIExtenderEx/MCM в коде не используются — жёстких
  зависимостей Nexus нет, модуль работает на чистой 1.4.8.

### 2. Копирование модуля
```bat
REM Скопируй модуль в игру
xcopy /E /I BannerlordModule\Modules\NordInvasion "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\NordInvasion"
```

### 3. Сборка DLL (ModKit)

#### dotnet CLI (рекомендуется)
```bat
cd BannerlordModule
set BANNERLORD_PATH=C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord
dotnet build NordInvasion.ModKit.csproj -c Release -p:BannerlordPath="%BANNERLORD_PATH%"

REM DLL появится в:
REM Modules/NordInvasion/bin/Win64_Shipping_Client/NordInvasion.dll
REM и копия в Win64_Shipping_Server/
```

Или через скрипт (сам найдёт Bannerlord):
```bash
# Linux
BANNERLORD_PATH=~/.steam/steamapps/common/Mount\ \&\ Blade\ II\ Bannerlord ./build_module.sh

# Windows
set BANNERLORD_PATH=C:\...\Bannerlord
build_module.bat
```

#### Visual Studio / Rider
1. Открой `NordInvasion.ModKit.csproj`
2. Проверь `BannerlordPath` в свойствах или задай env `BANNERLORD_PATH`
3. Build → Release

### 4. Террейн для карт (обязательно, один раз)
```bash
python3 tools/prepare_scenes.py
# копирует terrain.bin/flora.bin/ShaderCache из vanilla сцены в mp_ni_*
```
Или открой каждую сцену в Scene Editor (Launcher → Tools → Editor) и Save.

### 5. Проверка
- Launcher → включи ButterLib, UIExtenderEx, MCMv5, NordInvasion (ModuleCategory=Multiplayer)
- **Singleplayer:** Custom Battle → Map `mp_ni_bridge_01` → Mission `mp_nord_invasion` → Start → "Wave 1 preparing..."
- **Dedicated MP (основной путь с 2.2.0):** см. `DedicatedServer/Bannerlord/README.md` и `docs/MODKIT_GUIDE_RU.md`
  - Сгенерировать токен: MP лобби → Alt+~ → `customserver.gettoken`
  - Запустить: `DedicatedCustomServer.Starter.exe _MODULES_*Native*Multiplayer*NordInvasion*_MODULES_ /dedicatedcustomserverconfigfile DedicatedCustomServerConfig.xml`
  - Клиенты: Multiplayer → Custom Servers → `NordInvasion`

Логи: `Documents/Mount and Blade II Bannerlord/Logs/rgl_log.txt`

## Сборка без игры (CI, NuGet)

Если игры нет (CI, песочница), используется NuGet:

```bash
cd BannerlordModule
dotnet restore NordInvasion.csproj
dotnet build NordInvasion.csproj -c Release
# DLL в Modules/NordInvasion/bin/Win64_Shipping_Client/
```

Этот путь не требует ModKit, но DLL всё равно надо тестировать в игре.

## Нативный мультиплеер (2.2.0)

С 2.2.0 NI регистрирует кастомный GameType `NordInvasion` (как Full Invasion 3):

- `SubModule.cs`: `AddMultiplayerGameMode(new NordInvasionGameMode("NordInvasion"))`
- `Multiplayer/NordInvasionGameMode.cs`: `StartMultiplayerGame` — сервер и клиент behaviors
- `MissionMultiplayerNordInvasion` (server): команды Defender/Attacker, WaveManager, `NIMissionRepresentative`
- `MissionMultiplayerNordInvasionClient` (client): HUD, `BuildPlacedMessage`
- `NINetworkMessages.cs`: `RequestBuildMessage` (клиент→сервер), `BuildPlacedMessage` (сервер→все)

Подробно: `docs/MODKIT_GUIDE_RU.md` (раздел 4) и `docs/MULTIPLAYER_ANALYSIS_RU.md`.

## Dedicated Server

См. `docs/LAUNCH_GUIDE.md` раздел "Dedicated Server" и `DedicatedServer/Bannerlord/README.md`.

## Диагностика

- `cannot load scene prop` — пропс без меша (fallback сработает, список в `docs/ART_TASKS.md`)
- `cannot load sound event` — поправь ID в `Audio/NISound.cs`
- `CS####` — проверь версию игры, API меняется между патчами
- `Could not find TaleWorlds.Core.dll` — проверь `BANNERLORD_PATH`
