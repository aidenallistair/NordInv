# Nord Invasion Better Edition - Bannerlord

Кооперативный PvE мод для Mount & Blade II: Bannerlord. Оборона Свадии от волн нордов с прокачкой, строительством и мета-прогрессией.

## Что это

Команда 4-32 игроков держит форт против волн:

- Волны усиливаются, каждые 3 волны - спец-цель (таран, эскорт, поджог лагерей)
- Роли: Медик поднимает, Инженер строит/чинит, Знаменосец бафает, Пехота/Лучник дамажат
- Модульный форт: фундамент -> стены -> ворота -> колья против кавалерии -> масляный котел
- Roguelite перки каждые 3 волны, мутаторы богов каждые 4, AI-Директор как в L4D
- Погода влияет: туман слепит лучников, дождь тушит огненные стрелы, ночь требует факелов
- Физ-лут с боссов надо донести до казны, скавенджинг ресурсов с трупов и обломков
- Персистенция по SteamID: золото, чертежи, сезоны, BattlePass, глобальная кампания 8 деревень

## Структура проекта

```
BannerlordModule/ - C# мод
  Modules/NordInvasion/SubModule.xml - описание модуля
  src/NordInvasion/
    SubModule.cs - точка входа
    Behaviors/ - логика волн, директора, погоды, целей, мутаторов
    Components/ - медик, ранения/стамина, перки, лут
    Machines/ - баррикады, колья, котел, казна, таран
    Managers/ - стройка, отряды с формациями, скавенджинг, персистенция
    UI/ - HUD, магазин, выбор перков, стройка, карта кампании
    Models/ - WaveDefinition, Mutator, Perk, Village

DedicatedServer/Bannerlord/ - конфиг и скрипты для выделенного сервера
src/backend-php/ - PHP+MySQL бекенд персистенции (продакшн, на dedicated-хосте)
src/backend/ - FastAPI бекенд (dev-фоллбэк, тот же API)

docs/BANNERLORD_PLAN_RU.md - полный план реализации 15 механик
```

## Быстрый старт для разработки

1. Установи Bannerlord + Modding Kit (Steam -> Tools)
2. Установи зависимости с Nexus: ButterLib, UIExtenderEx, ModConfigurationMenu v5
3. Склонируй репо в `.../Mount & Blade II Bannerlord/Modules/NordInvasion/`
4. Открой `BannerlordModule/NordInvasion.csproj` в Rider/VS, пропиши пути к TaleWorlds.*.dll
5. Скомпилируй -> `Modules/NordInvasion/bin/Win64_Shipping_Client/NordInvasion.dll`
6. В лаунчере включи NordInvasion
7. Custom Battle -> Scene `mp_ni_bridge_01` -> Mission `mp_nord_invasion` или через Co-op мод

## Dedicated Server

```bash
# Скачать сервер через SteamCMD
steamcmd +login anonymous +app_update 1058080 validate +quit

# Запуск
TaleWorlds.MountAndBlade.DedicatedCustomServer.exe /dedicatedcustomserverconfig DedicatedServer/Bannerlord/DedicatedCustomServerConfig.xml
```

Подробнее в `DedicatedServer/Bannerlord/README.md`

## Backend (персистенция, кампания, сезоны)

**PHP 7.4+ + MySQL** — на хосте выделенного сервера. Гайд: `docs/BACKEND_PHP.md`,
пошаговая установка: `src/backend-php/README.md`.

```bash
mysql -e "CREATE DATABASE nordinv CHARACTER SET utf8mb4;"
cp -r src/backend-php/. /var/www/nordinv/
# правим /var/www/nordinv/config.php (DB_*, API_SECRET)
php /var/www/nordinv/install.php          # схема + деревни/сезон/battlepass/nodes
bash /var/www/nordinv/tests/smoke.sh http://host <API_SECRET>
```

Подключение мода: `PersistenceManager.BackendUrl` + `PersistenceManager.ApiSecret`.

API (form-encoded → JSON):
- `POST /api/player/login` - логин (steam_id / name), создаёт профиль
- `POST /api/kill` - убийство: золото, ресурсы, XP, босс
- `POST /api/wave/complete` - награды волны + best_wave
- `POST /api/perk/record` / `POST /api/meta/unlock` - перки и мета-дерево
- `POST /api/run/save` - победа/поражение забега
- `GET /api/campaign/villages` / `POST /api/campaign/battle` / `POST /api/campaign/vote`
- `GET /api/leaderboard` - топ-20 по season_points

> Dev-фоллбэк без MySQL: FastAPI `src/backend/` (тот же API) или
> `config.php` с `DB_DRIVER="sqlite"`.

## 15 механик Better Edition

1. **Roguelite перки** - выбор 1 из 3 каждые 3 волны
2. **Модульный форт** - стройка из фундамента в стены, ворота, колья, котел
3. **Роли** - Медик, Инженер, Знаменосец, Пехота, Лучник
4. **Цели волн** - таран, эскорт, поджог лагерей, защита казны
5. **AI-Директор** - адаптирует сложность под команду как в L4D2
6. **Погода/время** - туман, дождь, снег, ночь влияют на бой
7. **Кавалерия нордов** - фланговые рейды, контрмеры кольями
8. **Физ-лут** - мешок с босса надо донести до казны
9. **Скавенджинг** - ресурсы с трупов и обломков, крафт у костра
10. **Мутаторы богов** - 12 проклятий от Тора, Локи, Одина и т.д.
11. **Отряды с формациями** - стена щитов, клин берсерков, лучники под прикрытием
12. **Персистенция 2.0** - SteamID, чертежи, скины, сезоны, BattlePass
13. **Ранения/усталость** - 3 падения до смерти, стамина влияет на урон
14. **Разрушаемость/огонь** - деревья падают, бочки взрываются, поджоги
15. **Глобальная кампания** - 8 деревень, голосование, захват карты

Детали в `docs/BANNERLORD_PLAN_RU.md`

## Следующие шаги (актуальный план, см. docs/PROGRESS.md)

0. **Готово (session 4, 2026-08-29):** боевой магазин (покупка золота/ресурсов/чертежей
   через бэкенд, `POST /api/shop/buy` + `grants`), BattlePass-claim
   (`/api/battlepass/claim`, `season_points_earned`), сброс сезона
   (`/api/season/reset` под `X-NI-Admin`), выбор перка и сервисные покупки через F на
   пропсах (`NI_ArmoryUsable`, `NI_PerkTotemUsable`); статические проверки C#/SQL/API
   (`tools/lint_csharp.py`, `tools/test_backend_sql.py`, `tools/test_backend_api.py`).
   Зарегистрированные баги, закрытые попутно: `PerkManager` не был зарегистрирован в
   `SubModule` (механика 1 не запускалась), `typeof(TaleWorlds.Core.Agent)` в
   `NISound` (не компилировалось), сообщения из `Task.Run` вне UI-потока.
1. **Террейн карт (Windows + Bannerlord):** `python3 tools/prepare_scenes.py`
   - сцены `mp_ni_*` уже сгенерированы (XML: 65 entry points + пропсы,
     `tools/gen_ni_scenes.py`); скрипт дополнит их бинарным террейном из vanilla
2. **Собрать dll:** открыть `BannerlordModule/NordInvasion.csproj`, прописать
   HintPath, Build. При ошибках компиляции - точечные правки (API меняется
   между патчами Bannerlord; см. BUILD_FROM_SOURCE.md)
3. **Тест:** Custom Battle -> `mp_ni_bridge_01` -> `mp_nord_invasion`,
   пройти чеклист из `docs/LAUNCH_GUIDE.md`
4. **Backend персистентности (MySQL + PHP, на хосте dedicated-сервера):**
   `src/backend-php/` готов: `config.php` -> `php install.php` -> nginx/IIS ->
   `bash tests/smoke.sh`. Гайд: `docs/BACKEND_PHP.md`. Мод подключается
   строками `PersistenceManager.BackendUrl / ApiSecret` (URL + секрет).
5. **Арт-задачи** (docs/ART_TASKS.md): иконки перков, меши ni_*-пропсов,
   кастомные звуки (UI и код уже готовы, ждут ассеты)
6. **Тест Dedicated Server** (2 клиента, SteamCMD) -> upload на NexusMods
   (source-зип пересобран: `dist/NiNordInvasion_v2_1_0_source.zip`)

Полезные инструменты:
- `tools/validate_module.py` — проверка модуля перед релизом; дополнительно запускает
  `lint_csharp.py` (скобки/usings/override-ы/контракт маршрутов/сверка каталога) и
  `test_backend_sql.py` (схема + все SQL-запросы PHP на sqlite)
- `tools/test_backend_api.py` — контракт бэкенда (in-process / `--serve` / `--base URL`)
- `tools/make_release.py` — сборка релиз-зипа
- `docs/VERIFICATION.md` — пошаговая инструкция дальнейших проверок (песочница →
  террейн → сборка dll → игра → dedicated → релиз), с ожидаемым выводом команд и
  разбором отказов
