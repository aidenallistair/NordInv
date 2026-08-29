# Пошаговая инструкция дальнейших проверок (QA)

Порядок действий от «песочницы без игры» до выкладки на NexusMods. Шаги идут по
зависимостям: результат шага N — предпосылка шага N+1, поэтому «сначала в игру,
потом разберёмся» не работает (шаги 1-3 ловят то, что в игре выглядит как
«мод молчит», а на самом деле является CS0115/незапущенным поведением).

Правила:
- **Gate.** Шаг закрыт, только когда команда выдала ожидаемую строку *дословно*. Ниже
  они приведены как `ожидается:`.
- **Не правь вслепую.** Если что-то из шагов 3-5 не совпало — зафиксируй вывод
  в «Журнал проверок» (Приложение Б) и не «чини» Gauntlet-XML/сигнатуры API до
  подтверждения по DLL/игре: слепая правка стоит дороже, чем задокументированный риск.
- **После любой правки C# или PHP** — вернуться на шаг 1 (30 секунд) и только потом
  продолжать.

## Шаг 0. Что вообще где проверяется

| Проверка | Нужна игра | Нужен PHP/MySQL | Команда |
|---|---|---|---|
| XML модуля, сцены, пропсы, каталог | нет | нет | `python3 tools/validate_module.py` |
| C#: скобки, usings, override, контракт маршрутов, каталог | нет | нет | `python3 tools/lint_csharp.py` |
| SQL PHP: схема + все запросы + число плейсхолдеров | нет | нет (sqlite) | `python3 tools/test_backend_sql.py` |
| Контракт API (магазин, BP, голоса, сброс сезона) | нет | нет | `python3 tools/test_backend_api.py` |
| Те же проверки curl'ом | нет | да (или dev_server) | `bash src/backend-php/tests/smoke.sh <URL> [SECRET]` |
| Террейн сцен | **да** | нет | `python3 tools/prepare_scenes.py` |
| Компиляция и все публичные API игры | **да** | нет | Build `BannerlordModule/NordInvasion.csproj` |
| Экраны Gauntlet, F на пропсах, балансировка волн | **да** | нет | Custom Battle `mp_ni_bridge_01` |
| `OnTickAsAI` в 4 компонентах | да (или DLL) | нет | шаг 3 |

---

## Шаг 1. Статика без игры (Linux / macOS / Windows, Python 3.8+)

```bash
cd NordInv
python3 tools/validate_module.py        # 1) всё вместе, включает 2) и 3)
python3 tools/lint_csharp.py            # 2) только C#
python3 tools/test_backend_sql.py       # 3) только PHP-SQL
python3 tools/test_backend_api.py       # 4) контракт бэкенда in-process
```

```
ожидается:  Валидация: 0 ошибок, 12 предупреждений
ожидается:  Линтер: 0 ошибок, 1 предупреждений
ожидается:  Итог: 0 ошибок (схема собирается, SQL поднимается, плейсхолдеры сходятся)
ожидается:  Итог: 66 ok, 0 fail
```

Как читать:
- **12 предупреждений валидатора** — это `terrain.bin` / `flora.bin` / `ShaderCache`
  по 4 сценам. Они исчезнут после шага 2 (когда станет 0 ошибок / 0 предупреждений).
  Любое *другое* предупреждение или любая *ошибка* = стоп, чини до шага 2.
- **1 предупреждение линтера** — `OnTickAsAI`. Оно закрывается только шагом 3
  (сверка с DLL), в песочнице его снять нельзя. Если после шага 3 этот override
  заменён — удали запись из `UNCERTAIN_VIRTUALS` в `tools/lint_csharp.py`,
  иначе предупреждение будет вечно мешать читать вывод.
- `INFO:`-строки линтера — это не результат, а площадь покрытия (сколько маршрутов
  и позиций каталога реально сверено). `маршрутов, вызываемых из C#: 15` и
  `каталог магазина сверен: 17 позиций` — текущие значения; если после правок
  число маршрутов упало — значит вызовы из C# потерялись (механика отвалилась).

Прогон контракта через HTTP (ловит баги, видимые только по-настоящему через сокет:
разбор form-тела, коды, заголовки):

```bash
python3 tools/test_backend_api.py --serve
```

```
ожидается:  Итог: 66 ok, 0 fail          (и в логе "dev_server на :<порт>")
```

### 1.5. Smoke против дев-бэкенда (curl, как это делает игра)

```bash
python3 src/backend/dev_server.py --db /tmp/ni_qa.db --reset --port 8080 &
NI_ADMIN_SECRET=test-admin-key python3 src/backend/dev_server.py --db /tmp/ni_qa.db --port 8080 &  # если нужен сброс сезона
bash src/backend-php/tests/smoke.sh http://127.0.0.1:8080
python3 tools/test_backend_api.py --base http://127.0.0.1:8080 --admin-key test-admin-key
kill %1
```

```
ожидается:  === Итог: 29 ok, 0 fail ===
ожидается:  Итог: 64 ok, 0 fail          (--base: 2 проверки in-process не применимы)
```

`http 000` в smoke = сервер не отвечает (не поднялся / другой порт / файрвол),
а не «бэкенд сломан».

---

## Шаг 2. Террейн и превью сцен (Windows + установленный Bannerlord)

```powershell
cd C:\path\to\NordInv
python tools\prepare_scenes.py
```

1. Скрипт копирует бинарные данные из vanilla-сцены в
   `BannerlordModule/Modules/NordInvasion/ModuleData/Scenes/mp_ni_*`:
   `python3 tools/prepare_scenes.py` (корень игры ищет сам; иначе явно:
   `--bannerlord "D:\Games\Bannerlord" --source mp_ye_battle_01`).
2. Ожидаем в каждой из 4 папок: `terrain.bin`, `flora.bin`, `references.txt`,
   `ShaderCache/`; в конце скрипт печатает чек-лист следующих шагов.
3. Перепрогнать шаг 1: `ожидается: Валидация: 0 ошибок, 0 предупреждений`.

| Симптом | Причина | Действие |
|---|---|---|
| `ModuleData/Scenes/...` пустые, скрипт молчит | не найден путь установки игры | указать путь явно (см. `--help` / константу в начале скрипта) |
| terrain.bin есть, в игре карта чёрная/без коллизий | не та vanilla-сцена-источник (`--source`) или не доехал `references.txt` | пересобрать с `--source mp_ye_battle_01`, сравнить с эталоном |
| Игра падает на загрузке сцены | несовпадение `MultiplayerScenes.xml` и папок | `python3 tools/validate_module.py` (он это и сверяет) |

---

## Шаг 3. Первая сборка dll — главный фильтр API

Мод **ни разу не компилировался** (в песочнице нет компилятора), поэтому шаг 3 —
самая вероятная точка отказа. Всё, что статически проверяемо, уже снято линтером:
`CS0234/CS0246` (usings), разъехавшиеся строковые контракты маршрутов, регистрация
поведений. Остаётся то, что видит только компилятор/декомпилятор.

1. Открыть/собрать: `dotnet build BannerlordModule/NordInvasion.csproj` или
   VS 2022 (net472). В `csproj` прописаны `HintPath` на
   `..\..\..\..\..\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client\*.dll` —
   поправь под свою установку (ссылки: Core, MountAndBlade, Library, ObjectSystem,
   GauntletUI, 0Harmony).
2. Декомпилятор обязателен для пунктов 3.1-3.3: **ILSpy** или **dnSpy**,
   открыть `bin/Win64_Shipping_Client/TaleWorlds.Core.dll` и
   `TaleWorlds.MountAndBlade.dll`.

### 3.1. `OnTickAsAI` (известный открытый риск, 4 файла)

`BannerComponent`, `ElementalComponent`, `MedicComponent`, `WoundStaminaComponent`
объявляют `public override void OnTickAsAI(float dt) : AgentComponent`. В справочнике
1.0.3 такого виртуала нет.

| Если в DLL... | Действие |
|---|---|
| метод есть | риск закрыт: удали запись из `UNCERTAIN_VIRTUALS` (`tools/lint_csharp.py`), прогнать шаг 1, предупреждение уйдёт |
| метода нет (CS0115 «no suitable method found to override») | заменить на `public override void OnTick(float dt)` **и** добавить собственный троттлинг (тик каждый кадр ≠ раз в N секунд: у медика кулдаун, у знамёнщика — интервал баффа, у стихий — интервал урона), иначе механика станет в 60 раз агрессивнее |
| метода нет и `OnTick` в `AgentComponent` тоже нет | перенести логику в `MissionBehavior.OnMissionTick` (шаблон: `PerkManager` в `Managers/PersistenceManager.cs`) с обходом `Mission.PlayerTeam.ActiveAgents` |

### 3.2. Публичные API, которые линтер проверить не может

Сверяются тем же декомпилятором; справа — запасной вариант, который точно есть в игре:

| Вызов в коде | Если отсутствует |
|---|---|
| `agent.SetHitPoints(int)` / `SetMaximumHitPoints` (медик, перки, BP) | присваивание `agent.Health` (климпом по `agent.HealthLimit`) — так делают нативные VM/байты; строка-идиома в репо: `agent.SetHitPoints((int)Math.Min(agent.Health + n, agent.HealthLimit))` |
| `Mission.AddExplosion(...)` | урон по радиусу вручную: обход `Mission.PlayerTeam.ActiveAgents` + `Vector2.Distance` (шаблон в репо: `Machines/TrapMachines.cs`) |
| `Scene.LoadSceneProp(string)` / `GameEntity.AddComponent` | пропс не поднимется → `PropSpawner` уже возвращает null и вызывает fallback; в логе будет `Could not spawn <id> (asset missing?)` — это штатно до артов |
| `agent.SpawnMissile(...)` (баллиста) | искать в DLL семейство `SpawnMissile`/`MissionSpawnHelpers`; в крайнем случае - прямой урон по цели без снаряда |
| `Formation.Captain` (знамёнщик) | сверить с `Formation.UnitLeaderAgent` (есть ли в 1.2.10) - оба варианта только по DLL |
| `UsableMachine.OnUse(Agent, int)` | проверить точную сигнатуру `UseableInventoriedObject`/`OnUseBegin/OnUseEnd` — это вход для F-пропсов магазина и тотемов |

### 3.3. Регистрация и загрузка

- `SubModule.cs` должен регистрировать **21** `MissionBehavior` (все объявленные классы:
  проверка есть в линтере; при `NullReferenceException` на механике — смотри,
  не исчез ли `AddMissionBehavior` при слиянии).
- Первый запуск: в чате обязательно
  `ожидается: Nord Invasion Better Edition v2.1 - 29 mechanics Loaded!`
  Нет строки = SubModule не загрузился (имя папки мода, `SubModule.xml`,
  `ModuleData/SubModule.dll` не на месте — см. `docs/LAUNCH_GUIDE.md`).

### 3.4. После любой правки C#

```bash
python3 tools/validate_module.py
```
```
ожидается:  Валидация: 0 ошибок, 0 предупреждений     (после шага 2)
ожидается:  Линтер: 0 ошибок, 0 предупреждений          (после закрытия 3.1)
```

---

## Шаг 4. Бэкенд: поднять и убедиться, что мод его достучался

Молчаливый fallback — особенности мода: без бэкенда он **не падает и не ругается**,
просто работает в локальном режиме (`BackendReady == false`, `BuyLocal`). Поэтому
смотреть надо на сервере, а не в игре.

```bash
# вариант A: PHP+sqlite (без MySQL)
php src/backend-php/install.php
php -S 0.0.0.0:8080 -t src/backend-php src/backend-php/router.php
# вариант B: python-дев (тот же контракт, sqlite)
python3 src/backend/dev_server.py --port 8080 --reset
```

```
ожидается:  curl -s http://127.0.0.1:8080/api/health
            {"ok": true, "db": "sqlite", ...}      # у PHP-варианта "db": "mysql"
```

1. Секреты должны совпадать в двух местах: `config.php: API_SECRET` ↔
   `PersistenceManager.ApiSecret` (или env `NI_API_SECRET` для игрового процесса;
   `NI_BACKEND_URL` — адрес). Проверка: `curl` **без** заголовка `X-NI-Secret`
   при непустом секрете → `ожидается: 401`.
2. В моде: `setx NI_BACKEND_URL http://127.0.0.1:8080` (перезаложить Steam/игру
   после `setx`), либо задать статические поля до старта миссии.
3. Вход в миссию = `POST /api/player/login`: в access-логе `php -S` это видно
   строкой `POST /api/player/login 200`. Ни одной строки = игра не видит сервер
   (порт/файрвол/опечатка в URL), а не «бэкенд сломан».
4. Профиль после логина:
   ```bash
   curl -s -H "X-NI-Secret: СЕКРЕТ" http://127.0.0.1:8080/api/player/name_<md5-имени>
   ```
   ожидаем JSON с `gold/level/wood/metal/blueprints/perks/titles`.
   Игра должна показать те же числа в HUD (механика 12).

---

## Шаг 5. In-game: проверка механик session 4 (магазин, чертежи, BP, перки)

**Важно про UI:** `NI_Shop_VM` / `NI_BuildMenu_VM` / `NI_CampaignMap_VM` существуют и
полностью биндятся на сервер, но **UIExtender'а в проекте нет** — то есть на экран эти
VM'ы сегодня не выведены, а `ModuleData/GauntletUI/NI_*.xml` всё ещё биндят старые
имена команд. Рабочий путь ввода в бою — **F на пропсах**. Не считать багом, что
«магазина нет в меню»: это известный незакрытый пункт (docs/AUDIT.md, риск 3).

### 5.0. Что сегодня недоступно игроку (проверять не «в руке», а кодом)

В проекте **нет UIExtender'а** (`grep -rn "UIExtender" src/` = пусто), поэтому
`NI_Shop_VM`, `NI_BuildMenu_VM`, `NI_PerkChoice_VM`, `NI_CampaignMap_VM` ни на один
экран не выведены. Следствия, которые надо помнить при прогоне шага 5:

| Механика | Состояние в игре | Как проверить всё равно |
|---|---|---|
| 2 / 18 / 23 - стройка форта | `FortressBuildManager.TryPlace` вызывается **только** из `NI_BuildMenu_VM`, т.е. игрок построить ничего не может | временное debug-поведение (снизу); сам гейт чертежей статически сверяет `tools/lint_csharp.py` (blueprint → `Place`), а выдача чертежа - `tools/test_backend_api.py` (409 на повторной покупке) |
| 12 - магазин целиком | доступен лишь частично: 3 сервисные покупки через F на ящике (`NI_ArmoryUsable`) | `curl` + `shop_purchases`, как в 5.2-5.4 |
| 15 - голосование кампании | экран не открыт, но `PersistenceManager.VoteForVillage` и `NI_CampaignMap_VM` работают | `POST /api/campaign/vote`, `GET /api/campaign/villages` |
| 27 - ставки зрителя | `NI_Spectator_VM` без экрана | только код/API |

Debug-поведение для проверки стройки (создать `UI/NIDebugBuild.cs`, удалить после прогона):

```csharp
public class NIDebugBuild : MissionBehavior
{
    public override void OnMissionTick(float dt)
    {
        if (Mission.CurrentTime < 3f || Mission.CurrentTime > 4f) return;   // один раз, на 3-й секунде
        var fbm = Mission.GetMissionBehavior<NordInvasion.Managers.FortressBuildManager>();
        var agent = Mission.PlayerTeam?.ActiveAgents.FirstOrDefault();
        if (fbm == null || agent == null) return;
        foreach (var t in new[] { FortressBuildManager.BuildType.Foundation,
                                  FortressBuildManager.BuildType.Wall,
                                  FortressBuildManager.BuildType.Stakes })
            fbm.TryPlace(t, agent);
    }
}
```

Ожидаем на 3-й секунде: три `Placed ...!` в чате, `BuiltCount = 3`; для Stakes
без чертежа - сообщение про чертёж (это и есть проверка гейта 5.4 без UI).
**Не коммитить**: после прогона удалить файл и снять регистрацию из `SubModule.cs`.

Правильное закрытие гэпа (отдельная задача, не «проверка»): `using TaleWorlds...`;
в `OnSubModuleLoad` - `var e = new UIExtender("NordInvasion"); e.Register(typeof(SubModule).Assembly); e.Enable();`,
для каждого VM - `[ViewModelMixin]` + `[PrefabExtension("multiplayer", "descendant::Widget[@Id='...']")]`,
и **только тогда** имеет смысл переписывать `ModuleData/GauntletUI/NI_*.xml`
(синтаксис атрибутов команд сверяется с нативным префабом в игре - docs/AUDIT.md, риск 3).

| # | Что | Как | Ожидаем в игре | Как проверить на сервере |
|---|---|---|---|---|
| 5.1 | каталог/цены | `curl -s .../api/shop/catalog` | — | 17 позиций, `version` совпадает с `ShopCatalog.CatalogVersion` |
| 5.2 | оружейная | у спавна ящик (`vlandia_chest_c`), **F** | чат: `Armory: 1) Medkit ...` + факт покупки | `SELECT item_id,gold,wood,metal FROM shop_purchases ORDER BY id DESC LIMIT 5;` |
| 5.3 | не хватает золота | F при `gold < price` | красное сообщение, баланс не изменился | в `shop_purchases` новой строки нет (400 на сервере) |
| 5.4 | чертёж открывает постройку | купить `ballista` (curl/магазин) → пост. баллисты | без чертежа — текст `LockedInfo`/«нужен чертёж», постройка не ставится; с чертежом — ставится | `blueprints` у игрока содержит `ballista`; повторная покупка того же чертежа → **409**, «уже открыт» |
| 5.5 | BattlePass claim | `curl -X POST -d "steam_id=...&level=1" .../api/battlepass/claim` | награда выдана, строка BP в HUD сдвинулась | `SELECT level,season,reward FROM battlepass_claims;`; второй claim того же уровня → 409; `level = floor(season_points_earned/25)` |
| 5.6 | трата ≠ откат BP | купить что-то за `season_points` | уровень BP **не** упал | `players.battlepass_level` тот же, `season_points_earned` не изменился |
| 5.7 | выбор перка (3 волны) | волна 3 → у игрока 3 жаровни, **F** на нужной | перк применён мгновенно, сообщение `Perk applied: ...` | `players.perks` содержит id; запрос `POST /api/perk/record` в логе |
| 5.8 | тайм-аут 15 сек | ничего не нажимать | случайный перк из тройки + жёлтое сообщение | тот же `perks`, +1 запись |
| 5.9 | кооп-перки | два игрока в одном окне | **оба** берут свой перк, тотемы гаснут только когда выбрали оба; «No perk choice pending for you» тому, кто уже выбрал | — |
| 5.10 | кампания | голос за деревню (`/api/campaign/vote`), победа забега | — | `SELECT village_id,wave_reached,won FROM campaign_battles ORDER BY id DESC LIMIT 1;` — **не** `0/25` (старый баг), `votes` только текущего `season_id`, повторный голос того же игрока → 409 |
| 5.11 | волны 1-25 | пройти/проиграть забег | best_wave растёт, defeat не считается победой | `players.best_wave/wins/losses` |

Полезные curl'ы (form-encoded, как присылает мод — JSON тоже принимается):

```bash
S='X-NI-Secret: СЕКРЕТ'; B=http://127.0.0.1:8080
curl -s -X POST -H "$S" -d "steam_id=76561198000000001" $B/api/player/login
curl -s -X POST -H "$S" -d "steam_id=76561198000000001&item_id=ballista&qty=1" $B/api/shop/buy
curl -s -X POST -H "$S" -d "steam_id=76561198000000001&item_id=no_such_item" $B/api/shop/buy   # 400
curl -s -H "$S" $B/api/battlepass/progress
curl -s -X POST -H "X-NI-Admin: ADMIN_SECRET" $B/api/season/reset              # после сброса: BP-уровень обнулён, gold/чертежи остались
```

SQLite (dev): `sqlite3 src/backend-php/ni_local.db "SELECT id,gold,season_points_earned,battlepass_level FROM players;"`
(у python-дева база там, где указан `--db`, по умолчанию `src/backend/ni_dev.db`).
MySQL: `mysql nordinv -e "..."`.

---

## Шаг 6. Отказоустойчивость (самое частое «мод упал» в ревью)

1. **Бэкенд лёг посреди забега** (`kill` сервера на волне 5):
   ожидаем — игра продолжает, покупки идут локально, в чате нет исключений;
   при возврате сервера логин повторяется и `BackendReady` снова `true`
   (`SELECT last_seen FROM players;` обновился).
2. **Неверный секрет**: `ожидается: 401` на сервере и **отсутствие** спама в чате;
   проверь, что мод не долбит `login` каждый тик (в логе сервера не тысячи строк
   за минуту).
3. **Двойная покупка** (быстро дважды F): в `shop_purchases` — столько строк, сколько
   нажатий, но баланс/`blueprints` не ушли в минус и чертёж не выдан дважды (409).
4. **Оффлайн-старт**: запустить игру без поднятого бэкенда → миссия стартует, HUD,
   перки и стройка работают (сохранение — нет). Это штатный режим, не баг.
5. **UI-поток**: сообщения из фоновых `Task.Run` обязаны приходить **пачкой на следующем
   тике** (очередь `_uiQueue`), без зависаний; признак проблемы — «вылет» в момент
   ответа сервера.

---

## Шаг 7. Кооп и dedicated

- `docs/LAUNCH_GUIDE.md` → Dedicated Server (SteamCMD, 2 клиента для начала).
- На сервере: `NI_BACKEND_URL`/`NI_API_SECRET` задаются **для процесса сервера**,
  не клиента; в кооперативе каждый игрок платит и получает **на свой** профиль
  (проверить: два разных `steam_id` → разные строки в `shop_purchases`).
- Тотемы перков: в коопе окно per-agent (шаг 5.9) — на dedicated это единственная
  проверка, где видно, что `ConsumeTotems()` не гасит чужие тотемы раньше времени.
- 32 слота: волна 10 (кавалерия) + осадные — смотреть на `MaxBuildings = 40`
  (дальше стройка отказывает с сообщением, а не падает) и на то, что `BuiltCount`
  синхронно у всех.
- Нет SteamID (пиратка/локальный тест) → идентичность из имени (`name_<md5>`),
  это ожидаемо: `SELECT id FROM players;` покажет `name_...` вместо `steam_...`.

---

## Шаг 8. Арт-заменители: что штатно, а что проблема

До импорта мешей (docs/ART_TASKS.md) всё спавнится vanilla-фоллбеками.

| Видимое в игре | Вердикт |
|---|---|
| «ящик» вместо оружейного сундука, забор вместо кольев, бочка вместо котла, факел вместо жаровни/тотемов | **норма** (PropSpawner fallback), на проверку механик не влияет |
| `Could not spawn <id> (asset missing?)` | фоллбек тоже не поднялся: проверить, что `SceneProps.xml` читается и ID есть в нём (`python3 tools/validate_module.py` это сверяет) |
| Пропс есть, но взаимодействия нет (F не подсвечивается) | не создан компонент: `NI_ArmoryUsable`/`NI_PerkTotemUsable` вешаются в `PersistenceManager.SpawnArmoryChest` / `PerkManager.SpawnTotems` — проверить, что `Spawn` вернул не null |
| Меши ni_* появились, но выглядят как фиолетовые | не проставлен `material=` в `SceneProps.xml` (пункт 2 ART_TASKS) |

---

## Шаг 9. Релиз-артефакт и выкладка

```bash
python3 tools/validate_module.py            # 0 ошибок, предупреждений 0
python3 tools/make_release.py
ls -la dist/
unzip -l dist/NiNordInvasion_v2_1_0_source.zip | grep -ci "pycache\|\.pdb\|\.db\|_patch"
unzip -l dist/NiNordInvasion_v2_1_0_source.zip | tail -1
unzip -l dist/NiNordInvasion_v2_1_0_source.zip | grep -cE "shop_catalog.json|SubModule.xml"
```

```
ожидается:  validate: 0 ошибок
ожидается:  grep -ci = 0                    (мусора и одноразовых скриптов в зипе нет)
ожидается:  ... 104 files, ~240 КБ          (число файлов растёт с репозиторием -
                                             значимы именно 0 и 2 ниже/выше, а не это)
ожидается:  2                               (каталог экономики и SubModule на месте)
```

Чек-лист выкладки:
- [ ] `SubModule.xml` `<Version value="v2.1.0"/>` = заголовок `RELEASE_NOTES.md` = имя зипа;
- [ ] в зипе есть `Modules/NordInvasion/` (то, что кладут в Mods) **и** `backend-php/shop_catalog.json`;
- [ ] `BUILD_FROM_SOURCE.md` описывает те же `HintPath`, что в `csproj`;
- [ ] для чужих сборок: `docs/LAUNCH_GUIDE.md` начинается с «для игрока»;
- [ ] на NexusMods: тег v2.1.0, в описании — что бэкенд опционален (шаг 4) и что
      магазин/перки доступны через F на пропсах (шаг 5), а Gauntlet-экраны — в работе;
- [ ] changelog = раздел v2.1.0 в `RELEASE_NOTES.md`, PR обновлён (шаги 1-9 с журналом).

---

## Приложение А. Сообщение → куда смотреть

| Сообщение | Где | Причина и действие |
|---|---|---|
| `Валидация: N ошибок` | шаг 1 | читать строки с `ERROR:`; почти всегда ID в XML ≠ ID в коде (пропсы/troop/items) |
| `Линтер: N ошибок` | шаг 1 | разбаланс скобок = съеденная строка при правке; `нет using X -> CS0246` = сломанная сборка, чинить сразу |
| `Итог: ... N fail` в smoke | шаг 1.5 | `http 000` = сервер не отвечает; 401 = секрет; 404 = бэкенд без маршрута (дрейф PHP↔nidb → `tools/test_backend_api.py --base`) |
| `season reset disabled: set NI_ADMIN_SECRET` / 503 | шаг 5 | в `config.php`/env не задан `ADMIN_SECRET` — так и задумано |
| в игре нет строки `... v2.1 ... Loaded!` | шаг 3.3 | SubModule не загружается: структура папок/`SubModule.xml`/имя dll |
| `Could not spawn ...` | шаг 8 | арт-фоллбек не поднялся |
| `No choice in time - got: ...` | шаг 5.8 | это норма: тайм-аут выбора перка |
| `No perk choice pending for you` | шаг 5.9 | игрок нажал F после своего выбора — ожидаемо |

## Приложение Б. Журнал проверок (вставлять в PR/issue дословно)

```
[1] validate_module.py : Валидация: __ ошибок, __ предупреждений
[1] lint_csharp.py     : Линтер: __ ошибок, __ предупреждений
[1] test_backend_sql   : Итог: __
[1] test_backend_api   : Итог: __ ok, __ fail
[1.5] smoke.sh         : === Итог: __ ok, __ fail ===
[2] terrain.bin ×4      : есть / нет
[3] build dll          : успешно / (перечислить CS#### и как закрыл)
[3.1] OnTickAsAI по DLL: есть / нет → (что сделал)
[4] player/login в логе : 200 / нет
[5] 5.1-5.11           : PASS/FAIL по пунктам
[6] 1-5                : PASS/FAIL
[7] dedicated 2 клиента: PASS/FAIL
[9] zip                : файлов __, размер __ КБ, мусор: нет/список
Итог: релиз готов / блокирует: ...
```

## Приложение В. Что руками проверять не нужно

Покрыто инструментами шага 1 и падает красным, а не «на глаз»:
- баланс скобок/незакрытые строки после ручных правок C#;
- отсутствие `using` для `Vec3/Frame/GameEntity/ViewModel/DataSourceProperty/...`;
- `override` у `MissionBehavior`/`AgentComponent`-виртуалов из списка линтера;
- разъезд путей `"/api/..."` между C#, PHP и `nidb.py` (в т.ч. исчезнувшие вызовы);
- расхождение `shop_catalog.json` ↔ `Models/ShopCatalog.cs` (цены, grants, чертежи);
- незарегистрированный `MissionBehavior` (это и была причина «перки не работают»);
- `schema.sql` ↔ `install.php::ddl()`, число `?` в `prepare/execute` PHP;
- ссылки миссий на несуществующие сцены и troop/item ID из кода.
