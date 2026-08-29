# Инструкция по запуску Nord Invasion Bannerlord

## Для игрока (самый простой)

### Требования:
- Mount & Blade II: Bannerlord 1.2.10+ (Steam)
- 8GB RAM, 4 ядра

### Установка:

1. Скачай зависимости с NexusMods:
   - ButterLib https://www.nexusmods.com/mountandblade2bannerlord/mods/201
   - UIExtenderEx https://www.nexusmods.com/mountandblade2bannerlord/mods/210
   - Mod Configuration Menu v5 https://www.nexusmods.com/mountandblade2bannerlord/mods/612

2. Скачай NordInvasion Better Edition (релиз zip)

3. Распакуй в:
   ```
   C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\NordInvasion\
   ```
   Должно быть:
   ```
   Modules/NordInvasion/
     SubModule.xml
     bin/Win64_Shipping_Client/NordInvasion.dll
     ModuleData/
   ```

4. Запусти `Bannerlord Launcher`, включи галочки:
   - ButterLib
   - UIExtenderEx
   - ModConfigurationMenu v5
   - NordInvasion

5. Запусти игру -> Custom Battle -> 
   - Map: `mp_ni_bridge_01` (или town/castle/forest)
   - Mission: `mp_nord_invasion`
   - Start!

6. Управление:
   - B - Build Menu (стройка)
   - N - Shop (магазин)
   - M - Medic/Engineer действия
   - R - Commander (если ты командир)
   - L - Supply info
   - F - Use (подобрать лут, починить, поджечь)

## Для Co-op с друзьями (4-8 игроков)

### Требования:
- Мод Bannerlord Co-op https://www.nexusmods.com/mountandblade2bannerlord/mods/1080

### Установка:

1. Установи Bannerlord Co-op мод (по инструкции с Nexus)

2. Установи NordInvasion как выше

3. Host:
   - Запусти Bannerlord через Co-op Launcher
   - Co-op -> Host Game -> Custom Battle -> mp_ni_bridge_01
   - Пригласи друзей через Steam

4. Client:
   - Co-op -> Join Game -> IP друга

Все 29 механик работают в коопе.

## Для Dedicated Server 32 игрока

### Требования:
- Windows 10/Server или Linux с Wine
- 4 ядра, 8GB RAM, 10 Mbps upload

### Установка сервера:

#### Windows:

1. Скачай Dedicated Server через SteamCMD:
   ```bat
   steamcmd.exe +login anonymous +app_update 1058080 validate +quit
   ```
   Путь: `C:\steamcmd\steamapps\common\Mount & Blade II Dedicated Server\`

2. Скопируй модуль:
   ```
   xcopy /E /I Modules\NordInvasion "C:\steamcmd\...\Dedicated Server\Modules\NordInvasion"
   ```

3. Скопируй конфиг:
   ```
   copy DedicatedServer\Bannerlord\DedicatedCustomServerConfig.xml "C:\...\Dedicated Server\"
   ```

4. Отредактируй конфиг - поменяй `ServerName`, `MaxPlayers`, `Port`

5. Запусти:
   ```bat
   DedicatedServer\Bannerlord\start_bannerlord_server.bat
   ```
   Должно написать:
   ```
   Dedicated Server started on port 7240
   Nord Invasion Better Edition v2.0 Loaded!
   ```

6. Открой порт 7240 UDP в файрволе и роутере (Port Forwarding)

#### Linux:

```bash
sudo apt install steamcmd wine64
steamcmd +login anonymous +app_update 1058080 validate +quit
cd "~/.steam/steam/steamapps/common/Mount & Blade II Dedicated Server"
cp -r ~/NordInv/BannerlordModule/Modules/NordInvasion Modules/
wine bin/Win64_Shipping_Client/TaleWorlds.MountAndBlade.DedicatedCustomServer.exe /dedicatedcustomserverconfig ../../NordInv/DedicatedServer/Bannerlord/DedicatedCustomServerConfig.xml
```

### Подключение игроков к Dedicated:

- В Bannerlord: Multiplayer -> Add Server to Favorites -> IP:7240
- Или прямое подключение: консоль `connect_to_server IP 7240`

## Для Backend (персистенция, кампания, сезоны)

**PHP 7.4+ + MySQL 8**, крутится на хосте выделенного сервера. Полный гайд:
[`docs/BACKEND_PHP.md`](BACKEND_PHP.md), детали установки — `src/backend-php/README.md`.

### Требования:
- PHP 7.4+ с `pdo_mysql`
- MySQL 5.7+ / 8.x (или MariaDB)
- nginx (Linux) или IIS (Windows)

### Установка:

```bash
# 1. База
mysql -u root -p -e "CREATE DATABASE nordinv CHARACTER SET utf8mb4;"

# 2. Код
mkdir -p /var/www/nordinv && cp -r src/backend-php/. /var/www/nordinv/

# 3. Конфиг: /var/www/nordinv/config.php
#    DB_NAME/DB_USER/DB_PASS, API_SECRET (совместный с модом)

# 4. Схема + начальные данные (деревни, сезон, battlepass, skill nodes)
php /var/www/nordinv/install.php

# 5. nginx-сайт (пример в src/backend-php/README.md) + reload

# 6. Smoke-тест (должно быть "29 ok, 0 fail": профиль, награды, магазин,
#    battlepass-claim, голоса кампании, сброс сезона)
bash /var/www/nordinv/tests/smoke.sh http://nordinv.example.com <API_SECRET>

# 7. (опция) контрактный тест против этого же PHP: 64-66 проверок, идемпотентность
python3 tools/test_backend_api.py --base http://nordinv.example.com --secret <API_SECRET>
```

> Dev-режим без MySQL: в `config.php` `DB_DRIVER = "sqlite"`, затем
> `php -S 0.0.0.0:8080 -t src/backend-php src/backend-php/router.php`
> (роутер обязателен: без него `php -S` отдаёт 404 на `/api/*`, т.к. ищет файл, а не фронтовик).
> Совсем без PHP: `python3 src/backend/dev_server.py --port 8080 --reset` —
> тот же контракт на stdlib+sqlite (ядро `src/backend/nidb.py`), удобно для
> локальной проверки магазина/балансов.

### Настройка в моде:

В `BannerlordModule/src/NordInvasion/Managers/PersistenceManager.cs`
(статические поля, задаются при старте миссии):

```csharp
PersistenceManager.BackendUrl = "http://127.0.0.1:8080";
PersistenceManager.ApiSecret  = "тот-же-секрет-что-в-config.php";
```

Скомпилируй заново.

### Что хранит backend (server-authoritative):

- Игроки: gold, wood, metal, level/xp, season_points, best_wave, wins/losses,
  perks, blueprints, meta-дерево, titles (ранги), revives/builds/boss_kills
- Деревни: 8 деревень, owner, defense, счётчики сражений, голоса (1 на сезон)
- Сезоны, BattlePass-награды, лидерборд (топ-20 по season_points)
- Kill log (кто/что/когда за сколько золота)

Без backend мод работает, но прогресс только внутри забега (ошибки HTTP
логируются и не роняют игру).

## Для разработчика

### Компиляция:

1. Открой `BannerlordModule/NordInvasion.csproj` в Rider/VS

2. Пропиши пути к Bannerlord DLL:
   ```xml
   <HintPath>C:\...\Bannerlord\bin\Win64_Shipping_Client\TaleWorlds.Core.dll</HintPath>
   ```

3. Build -> `Modules/NordInvasion/bin/Win64_Shipping_Client/NordInvasion.dll`

4. Копируй в игру:
   ```bat
   xcopy /Y BannerlordModule\Modules\NordInvasion\bin\Win64_Shipping_Client\NordInvasion.dll "C:\...\Bannerlord\Modules\NordInvasion\bin\Win64_Shipping_Client\"
   ```

### Карты (4 сцены уже сгенерированы):

Сцены `mp_ni_bridge_01`, `mp_ni_town_01`, `mp_ni_castle_01`, `mp_ni_forest_01`
созданы в `ModuleData/Scenes/` (scene.xscene + atmosphere.xml):
- entry points: 0-31 игроки (западный форт), 32-63 норды (кольцо), 64 босс
- пропсы: факелы, казна, костер, бочки, стены/ворота/деревья (vanilla prefab'ы)

Осталось только **бинарное террейн-заполнение** (один раз, на машине с игрой):

```bash
python3 tools/prepare_scenes.py            # копирует terrain.bin/flora.bin/ShaderCache
python3 tools/prepare_scenes.py --source mp_ye_battle_01
```

либо открой сцену в Scene Editor (Launcher -> Tools -> Editor) и сохрани -
редактор перегенерирует террейн и navmesh сам.

Если хочешь свою карту: `python3 tools/gen_ni_scenes.py` как шаблон,
разметка пропсов в `map_*()` внутри скрипта.

### Тестирование:

Полный пошаговый прогон (с ожидаемыми строками вывода и разбором отказов) -
**`docs/VERIFICATION.md`**. Короткий список для быстрого запуска:

- 1 игрок: Custom Battle 1 волна 10 ботов - работает?
- Роли: умри -> Fallen -> медик поднимает?
- Форт: меню стройки (B) **пока не подключено** - `NI_BuildMenu_VM` существует, но
  экранов мода UIExtender не поднимает; строить можно проверочным поведением из
  `docs/VERIFICATION.md` (шаг 5.0). Проверяется так: чертёж куплен -> `TryPlace`
  ставит постройку, без чертежа -> сообщение про чертёж
- Оружейная: F на ящике у спавна -> аптечка/снаряды/ремонт (покупка уходит на бэкенд)
- Перки: волна 3 -> три жаровни, F = выбор, 15 сек бездействия = случайный
- Кавалерия: волна 10 -> колья убивают лошадь?
- Лут: убей босса -> мешок -> донеси до казны?
- Last Stand: 1 живой vs 1 норд -> слоу-мо?
- Снабжение: караван каждые 3 волны?

## Частые проблемы

**Мод не загружается:**
- Проверь `rgl_log.txt` в `Documents/Mount and Blade II Bannerlord/Logs/`
- Убедись что ButterLib, UIExtenderEx, MCMv5 включены и нужной версии

**Боты не спавнятся:**
- Проверь entry points в сцене, должны быть 32-64
- Проверь Characters.xml - troop id должны совпадать

**Баррикады не ставятся:**
- Проверь wood/metal в `PlayerGoldComponent`, нужно 5 wood для foundation
- Raycast на землю - ставь на ровной поверхности

**Backend не подключается:**
- Проверь что `uvicorn` запущен на 0.0.0.0:8000
- Проверь файрвол, открой 8000 TCP
- В моде `_backendUrl` должен быть доступный IP, не localhost если сервер на другой машине

**Dedicated Server крашится:**
- Проверь что все 4 зависимости (Native, SandBoxCore, CustomBattle, NordInvasion) в Modules/
- Проверь `DedicatedCustomServerConfig.xml` - все теги закрыты?
- Запусти с логами: `DedicatedCustomServer.exe /dedicatedcustomserverconfig config.xml /log`

## Готово!

Теперь ты можешь играть в Nord Invasion Better Edition на Bannerlord с 29 механиками.
