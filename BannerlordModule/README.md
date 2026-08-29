# Nord Invasion Better Edition - Bannerlord Module

Кооперативный PvE мод для Bannerlord. Оборона от волн нордов с 29 механиками.
С 2.2.0 основной путь — встроенный Dedicated MP (GameType NordInvasion), как Full Invasion 3.

## Установка для разработки (ModKit, рекомендуется)

1. Установи Bannerlord + Modding Kit (Steam → Tools)
2. Установи зависимости: ButterLib, UIExtenderEx, ModConfigurationMenu v5 (Nexus)
3. Клонируй репо
4. **ModKit сборка (с игрой):**
   ```bat
   set BANNERLORD_PATH=C:\...\Mount & Blade II Bannerlord
   dotnet build NordInvasion.ModKit.csproj -c Release
   ```
   DLL появится в `Modules/NordInvasion/bin/Win64_Shipping_Client/` и `Server/`
   (см. `docs/MODKIT_GUIDE_RU.md`)
5. **CI сборка (без игры):**
   ```bat
   dotnet build NordInvasion.csproj -c Release
   ```
   (NuGet `Bannerlord.ReferenceAssemblies` подтянет TaleWorlds сборки)
6. Скопируй `Modules/NordInvasion/` в папку игры `Mount & Blade II Bannerlord/Modules/`
7. Террейн карт (один раз): `python tools/prepare_scenes.py`

## Структура

- `src/NordInvasion/SubModule.cs` - точка входа, регистрация GameType `NordInvasion`
- `Multiplayer/` - нативный MP: GameMode, Server/Client behaviors, Representative, Spawn, NetworkMessages
- `Behaviors/` - волны, директор, погода, цели, мутаторы
- `Components/` - медик, ранения/стамина, перки
- `Machines/` - баррикады, колья, котел
- `Managers/` - стройка (с MP поддержкой TryPlaceMP), отряды, лут, персистенция
- `UI/` - HUD, магазин, перки, стройка, кампания
- `Models/` - WaveDefinition, Mutator, Perk, Village

## Тест

- **Dedicated MP (основной, стабильный):** `docs/MODKIT_GUIDE_RU.md` раздел 5
  - Сгенерировать токен: MP лобби → Alt+~ → `customserver.gettoken`
  - Запустить: `DedicatedCustomServer.Starter.exe _MODULES_*Native*Multiplayer*NordInvasion*_MODULES_ /dedicatedcustomserverconfigfile DedicatedCustomServerConfig.xml`
  - Клиенты: Multiplayer → Custom Servers → `NordInvasion`
- **Singleplayer:** Custom Battle -> Scene `mp_ni_bridge_01` -> Mission `mp_nord_invasion`
- **Co-op мод (fallback, нестабильный):** Host -> NordInvasion mission через Bannerlord Co-op

Подробно про нативный MP: `docs/MULTIPLAYER_ANALYSIS_RU.md` и `docs/MODKIT_GUIDE_RU.md` (раздел 4)

## Backend

Бекенд в `src/backend-php/` (PHP+MySQL, продакшн) и `src/backend/` (dev fallback).

```bash
# PHP
php src/backend-php/install.php
bash src/backend-php/tests/smoke.sh http://host SECRET

# Dev (без PHP/MySQL)
python3 src/backend/dev_server.py --port 8080 --reset
```

