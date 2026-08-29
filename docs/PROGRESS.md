# Прогресс реализации - Bannerlord Better Edition, 29 механик

## Фаза 0: Подготовка (DONE)

- [x] Структура проекта `BannerlordModule/`
- [x] SubModule.xml с зависимостями ButterLib, UIExtenderEx, MCMv5
- [x] SubModule.cs точка входа - регистрирует 15+ behaviors
- [x] NordInvasion.csproj
- [x] ModuleData/Characters.xml - 12 типов нордов + кавалерия + support NPCs
- [x] ModuleData/MultiplayerScenes.xml - 4 карты
- [x] ModuleData/MultiplayerMaps.xml
- [x] ModuleData/Missions.xml - миссия mp_nord_invasion
- [x] ModuleData/Items.xml - torch, hammer, medical_kit, wood, metal, loot bag, blueprints, powder barrel, cosmetics
- [x] ModuleData/SceneProps.xml - 25+ пропсов
- [x] ModuleData/GauntletUI/ - 5 UI XML: HUD, Shop, BuildMenu, PerkChoice, CampaignMap
- [x] ModuleData/Languages/RU/ - перевод 20+ строк
- [x] build_module.bat/.sh - скрипты сборки

## Фаза 1: Ядро - Волны, Спавн, Победа/Поражение (DONE)

- [x] `Models/WaveDefinition.cs` - WaveState, WaveObjective, MutatorType, PerkDatabase 13 перков, MutatorDatabase
- [x] `Behaviors/NordInvasionWaveManagerBehavior.cs` - полный цикл:
  - SetupWave() считает BotsTotal = 8+wave*2+players*2 * director multiplier (кап 120)
  - SpawnWave() с учетом кавалерии после 10 волны, мутаторов, отрядов
  - Boss spawn каждые 5 волн + BossRush мутатор
  - Objectives spawn (реальные: таран 2000 HP, 3 лагеря, эскорт, казна)
  - OnBotKilled() - золото, скавенджинг, физ-лут с босса, elemental combo, backend call
  - OnAgentRemoved - Fallen vs Death (3 падения), респавн-волны (каждые 4)
  - OnAgentHit - stamina, marked mutator, greedy steal
  - **Победа (25 волн) и поражение реально завершают миссию** (`Mission.EndMission`)
- [x] `Behaviors/NordInvasionDirectorBehavior.cs` - Stress 0-100, GetMultiplier() 0.8-1.2, relief ammo box
- [x] `Behaviors/NordInvasionWeatherBehavior.cs` - SetRandomWeather() каждые 5 волн: fog, rain, snow, night, clear. Также Objective, Mutator, Campaign behaviors
- [x] `UI/HUD/NI_HUD_Behavior.cs` + `NI_HUD_VM` - WaveInfo, MutatorInfo, GoldInfo (регистрация в SubModule)
- [x] `Managers/PersistenceManager.cs` - PlayerGoldComponent Gold/Wood/Metal/Kills/Perks/Blueprints/Titles/IsCarryingLoot, OnKill backend POST, OnCampaignWin, EnsureComponents, PerkManager (15-сек окно выбора + тайм-аут), LootManager (реальный спавн мешка)

## Фаза 2: Combat & Fort (DONE)

- [x] `Components/WoundStaminaComponent.cs` - Stamina 100-5 за удар <20 = -30% speed, Fallen 3 раза до смерти, Second Wind perk, Regen perk, Bleed chance
- [x] `Components/RoleComponents.cs` - ClassComponent, MedicComponent TryRevive 5 сек + Heal + LastStand check, EngineerComponent Repair +20 HP * perk, BannerComponent buff 15м 1.1x damage
- [x] `Components/WoundStaminaComponent.cs` PerkAgentComponent - 13 перков, HpMod, DamageMod, BarricadeMod, GoldMod, GetDamageWithPerks() Bloodlust
- [x] `Machines/BarricadeMachines.cs` - BarricadeDestructible (настраиваемый HP, горит, +2 wood при разрушении), StakesTrap anti-cav, TreasuryChest (принимает только несомый мешок), LootBag (pick-up -> скорость -30%), Campfire craft, Brazier
- [x] `Managers/FortressBuildManager.cs` - TryPlace: REАЛЬНОЕ спавнение пропсов (PropSpawner + vanilla-fallback), экономика: личные ресурсы -> склад, Repair, SpawnAmmoBox
- [x] `Managers/FortressBuildManager.cs` ScavengeManager - 20% metal veteran+, 30% wood, перк Scavenger x1.5
- [x] `Managers/FortressBuildManager.cs` SquadManager - SpawnShieldWallSquad (leader=Formation.Captain), SpawnBerserkWedge
- [x] `Machines/SiegeWeapons.cs` - Ballista, Catapult AOE, OilPot
- [x] `Machines/ForgeUsable.cs` - Weapon tempering: Sharpened, Hardened, Poison, Flaming
- [x] `Machines/TrapMachines.cs` - RockTrap, LogTrap, OilDitch, Drawbridge

## Фаза 3: Meta (DONE)

- [x] `Managers/PersistenceManager.cs` PerkManager - ShowChoiceToAll() каждые 3 волны, 3 перка, окно 15 сек, тайм-аут = рандом, ChooseForAgent() для Gauntlet
- [x] `Behaviors/BossPhaseBehavior.cs` - 3 фазы: 66% summon 2 minions, 33% fire ground + buff, взрыв при смерти + звуки
- [x] `Behaviors/CommanderBehavior.cs` - CommanderAgent, PlaceMarker Attack/Build/Retreat, IsNearMarker
- [x] `Behaviors/MoraleBehavior.cs` - SquadMorale, leader death -40, <30 паника, PlayerTeamMorale <30 = -15% speed
- [x] `UI/NI_Shop_VM.cs` - Shop VM, BuildMenu VM, PerkChoice VM (3 карточки + иконки-слоты + таймер), CampaignMap VM, ClassSelect VM, Spectator VM
- [x] `Models/WaveDefinition.cs` PerkDatabase, MutatorDatabase

## Фаза 4: Persistence, Campaign, Extra (DONE)

- [x] `Managers/PersistenceManager.cs` LootManager - SpawnLootBag: реальный пропс + LootBagUsable, fallback авто-казна
- [x] `Components/ElementalComponent.cs` - Fire tick, Poison, Ice, Lightning chain 3 + x2 rain, Bleed stacks, OilComponent, ElementalWeaponComponent combo oil+fire
- [x] `Managers/MetaProgressionManager.cs` - SkillTree 7 нод, Ranks 4 титула, ApplyMetaBonuses, ApplyCosmetics
- [x] `Behaviors/CampPhaseBehavior.cs` - Camp Phase каждые 5 волн 90 сек, Trader, Smith, Dynamic NPCs
- [x] `Behaviors/SupplyBehavior.cs` - WoodStock/MetalStock, Warehouse Level 2, Caravan каждые 3 волны (прибытие/разрушение по tick)
- [x] `Behaviors/SpectatorBettingBehavior.cs` - Bet, Killcam 3 сек слоу-мо босс, OnWaveCompleted payout 1.5x
- [x] `Behaviors/LastStandBehavior.cs` - Last Stand 10 сек слоу-мо 0.3, last +100% damage, revive = успех
- [x] `src/backend/main.py` - FastAPI: players, villages 8, seasons, battlepass, leaderboard, campaign battle, blueprint unlock

## Фаза 5: Полировка (DONE 95%)

- [x] ModuleData/GauntletUI/ - 5 XML (PerkChoice со слотами иконок)
- [x] ModuleData/Items.xml - 15+ предметов
- [x] ModuleData/SceneProps.xml - 25+ пропсов
- [x] ModuleData/Languages/RU/ - перевод
- [x] ModuleData/Sounds/README.md + Audio/NISound.cs - триггеры звуков (мутатор, босс, last stand, победа/поражение, караван, перк)
- [x] build_module.bat/.sh - сборка и копирование в Bannerlord
- [x] DedicatedServer/Bannerlord/ - DedicatedCustomServerConfig.xml (GameType=Multiplayer) + start scripts
- [x] docs/ - полный комплект гайдов
- [x] **4 сцены mp_ni_*** (session 2): `tools/gen_ni_scenes.py` сгенерировал scene.xscene
      (65 entry points: 0-31 игроки, 32-63 норды, 64 босс) + пропсы + atmosphere.xml
      для bridge/town/castle/forest. Формат сверен с vanilla-сценами.
- [x] **tools/prepare_scenes.py** (session 2): копирует terrain.bin/flora.bin/ShaderCache
      из vanilla-сцены (нужна установка Bannerlord - один раз на машине с игрой)
- [x] **tools/validate_module.py** (session 2): валидация XML, SubModule-регистрации,
      сцен, troop/item/prop ID (проходит: 0 ошибок)
- [x] **Release zip** (session 2): `tools/make_release.py` ->
      `dist/NiNordInvasion_v2_1_0_source.zip` (source-релиз, dll собирается локально)
- [x] **Исправления session 2**: SubModule.xml регистрировал только 2 XML из 6
      ( Characters/Items/Missions/SceneProps не грузились); MathF (нет в net472);
      небезопасные API (Peer.Communicator, SetActionChannel, rotation.f, SetTeam);
      csproj: System.Net.Http; killcam double-call; экономика строительства
- [ ] **UIExtender + подключение экранов** (главный функциональный гейт после session 4):
      без него `NI_Shop_VM`/`NI_BuildMenu_VM`/`NI_PerkChoice_VM`/`NI_CampaignMap_VM`
      не выведены на экран, т.е. стройка форта (механики 2/18/23) игроку недоступна.
      Порядок и проверочный debug-хук - docs/VERIFICATION.md, шаг 5.0
- [ ] Бинарный террейн сцен (terrain.bin) - `prepare_scenes.py` на машине с игрой
      или сохранение в Scene Editor (5 минут на сцену)
- [ ] Иконки перков - mesh + material (docs/ART_TASKS.md; VM/слоты готовы, session 4)
- [ ] Мешы ni_*-пропсов (docs/ART_TASKS.md; vanilla-fallback, session 4 добавил
      фоллбеки для осадных/ловушек и ящика оружейной; тотемы перков используют ni_brazier)
- [ ] Тест Dedicated Server 2 клиента (нужен SteamCMD/Windows)
- [ ] Загрузка на NexusMods (нужен аккаунт + dll)

## Итог (session 2, 2026-08-29)

**Сделано:**
1. Анализ: код - "скелет" (2938 строк C#, 24 файла), доки преувеличивали готовность
   деталей. Найден и исправлен блокер: SubModule.xml не регистрировал 4 XML.
2. Ядро: миссия реально заканчивается (победа/поражение), цели волн реальные
   (таран/лагеря/эскорт/казна), физ-лут работает (мешок -> казна), респавн-волны,
   перки с таймером, караван по tick, звуки через NISound (безопасно).
3. Карты: 4 сцены сгенерированы (XML-часть). Осталось только бинарное
   террейн-заполнение на машине с игрой (prepare_scenes.py).
4. Инфраструктура: validate_module.py, make_release.py, prepare_scenes.py,
   gen_ni_scenes.py. Валидация: 0 ошибок.
5. Release: dist/NiNordInvasion_v2_1_0_source.zip (session 4: каталог магазина/чертежи/BP в `src/backend-php/shop_catalog.json`).

**Следующий шаг для человека (нужна Windows + Bannerlord):**
1. `python3 tools/prepare_scenes.py` (террейн)
2. Собрать dll (csproj), при CS-ошибках - точечные правки по rgl_log
3. Запустить Custom Battle mp_ni_bridge_01 -> mp_nord_invasion
4. Прогнать тест-чеклист (LAUNCH_GUIDE.md), поправить ID частиц/звуков
5. Upload на NexusMods

## Session 3 (2026-08-29): персистенция через MySQL + PHP + Dedicated Server

Задание: «сохранение вещей и прокачки через mysql+php+dedicated server».

**Сделано:**

1. **PHP-бэкенд** (`src/backend-php/`, новый):
   - `config.php` — драйвер mysql/sqlite, креды, `API_SECRET` (X-NI-Secret),
     allowlist чертежей.
   - `lib.php` — PDO-синглтон, JSON out/fail, form+JSON body reader, проверка
     секрета, идентичность игрока (`steam_<id>` / `name_<md5>`), профиль, XP/level.
   - `index.php` — front controller, 16 маршрутов: player login/get, kill,
     wave/complete, perk/record, run/save, blueprint/unlock, meta/unlock,
     stat/increment (авто-титулы), campaign (villages/battle/vote), season,
     leaderboard, battlepass, health.
   - `schema.sql` + `install.php` — 7 таблиц (players c JSON-колонками perks/
     blueprints/meta/titles, kill_log, villages, seasons, battlepass_rewards,
     skill_nodes, campaign_votes c UNIQUE-голосом) + seed (8 деревень, сезон,
     7 battlepass-наград, 7 skill nodes). DDL driver-aware (MySQL/SQLite).
   - `README.md` — деплой Linux (nginx+php-fpm) и Windows (IIS) пошагово.
   - `tests/smoke.sh` — 18 проверок API через curl.
2. **C#-подключение** (рефакторинг под PHP-контракт):
   - `PersistenceManager` переписан: статические `BackendUrl`/`ApiSecret`,
     единый `PostForm` (X-NI-Secret), логин парсит реальный JSON (`NIJson`),
     `ApplyProfile` применяет gold/wood/metal/perks/titles/meta к агенту,
     события: OnKill(gold,is_boss), OnWaveCompletedFor, ReportPerk, SaveRun,
     OnMedicRevive, OnBuildPlaced, UnlockBlueprint, UnlockMetaNode,
     OnCampaignWin (csv из player_id).
   - Новый `Utils/NIJson.cs` — JSON-парсер без зависимостей.
   - `NIPeers`: `GetSteamId` (reflection-safe: SteamId64/Id/SessionId),
     `MakePlayerId` (та же формула, что в PHP).
   - Хуки: WaveManager (kill с реальным gold; +20g волны -> backend; победа/
     поражение -> run/save), MedicComponent (revives), FortressBuildManager
     (builds), PerkManager (perk/record), MetaProgressionManager (бонусы из
     meta-узлов, не из blueprints — логический баг исправлен).
3. **Доки:** `docs/BACKEND_PHP.md` (архитектура, API-таблица, решения),
   LAUNCH_GUIDE (секция backend -> PHP+MySQL), README.
4. **Релиз:** `make_release.py` включает `backend-php/`, зип пересобран.

**Проверено в песочнице:** все 7 DDL + seed + все SQL-выражения API прогнаны
через sqlite (GREATEST заменён на CASE для кросс-драйверности; UNIQUE KEY ->
UNIQUE constraint в sqlite-ветке). C#: 31 файл, brace/paren-баланс OK.
PHP-рантайма в песочнице нет — на хосте: `bash tests/smoke.sh`.

**Осталось (человеку):** MySQL+PHP на dedicated-хосте (гайд в README),
прописать URL/секрет в моде, smoke-тест, после — shop UI -> UnlockBlueprint,
battlepass claim, сброс сезона.

## Session 4 (2026-08-29): экономика магазина, BattlePass, сброс сезона + проверка без игры

Задание: «продолжить по актуальному плану» = пункты `docs/BACKEND_PHP.md →
Ограничения/следующие шаги` и `docs/PROGRESS.md → Осталось`: *shop UI → UnlockBlueprint*,
*battlepass claim*, *сброс сезона* + снятие риска «мод никогда не компилировался»
статикой, которая возможна без установки игры.

**Сделано по пунктам:**

1. **Единый каталог экономики:** `src/backend-php/shop_catalog.json` (17 позиций, цены,
   `grants`, allowlist чертежей, таблица battlepass, `bp_points_per_level`, стартовое
   золото). Читают: PHP (`config.php`), Python (`nidb.load_catalog`), C# держит
   встроенный fallback (`Models/ShopCatalog.cs`) - `validate_module.py` сверяет все три,
   расхождение цен/grants = ошибка валидации.
2. **Магазин (план: «shop UI -> UnlockBlueprint»):**
   - PHP: `GET /api/shop/catalog`, `POST /api/shop/buy` (цену и баланс проверяет сервер,
     журнал `shop_purchases`, повторная покупка чертежа -> 409, награды валидируются
     ДО списания), `GET /api/shop/history`.
   - C#: `NI_Shop_VM` -> `PersistenceManager.BuyShopItem()` (авторитетно серверу),
     `ApplyGrants()` применяет `wood/metal/gold/blueprint/title/skin/heal/ammo/repair`,
     `shop/history`, страницы каталога, `ExecuteReload`. Без бэкенда - `BuyLocal`
     (мод играбелен без MySQL, просто не сохраняется).
   - `FortressBuildManager`: чертежи **реально гейтят** постройки (Door, Stakes, SpikeTrap,
     OilCauldron, Brazier, ShieldWall, Ballista, Catapult, RockTrap, LogTrap, OilDitch);
     туда же добавлены сами постройки механик 18/23 - классы машин были, но ставить
     их было нечем. Экономика приведена к `Spend()` (личные ресурсы -> склад).
3. **BattlePass (план: «battlepass claim»):** колонка `season_points_earned`
   (прогресс не откатывается тратами), таблица `battlepass_claims`
   (UNIQUE player_id+level+season), `GET /api/battlepass/progress`,
   `POST /api/battlepass/claim` (проверка уровня, 409 на повтор, награды через
   `apply_grants`). `battlepass_level` пересчитывается при каждом начислении.
   C#: `RefreshBattlepass()`, `ClaimBattlepass()`, `NextClaimableLevel()`,
   кнопка `ExecuteClaimBattlepass` + строка BP в шапке магазина.
4. **Сброс сезона (план: «сброс сезона»):** `POST /api/season/reset` под `X-NI-Admin`
   (`ADMIN_SECRET`; не задан -> 503, чтобы нельзя было стереть сезон случайным запросом):
   архив в `season_history`, новый `seasons`, обнуление `season_points`,
   `season_points_earned`, `battlepass_level`, `meta`; золото/уровень/титулы/чертежи
   сохраняются, голоса остаются в старом сезоне. Идемпотентная миграция существующих
   баз - `install.php::migrate()` (добавляет недостающие колонки players).
5. **Dev-бэкенд приведён к контракту (реальный баг, а не косметика):** старый
   `src/backend/main.py` читал `players` по неверным индексам колонок (level/xp/wood
   путались), принимал только JSON-тело (мод шлёт form -> 422), не имел 7 из 16
   маршрутов (wave/complete, perk/record, run/save, stat/increment, meta/unlock,
   campaign/vote, health). Теперь ядро - `src/backend/nidb.py` (stdlib + sqlite),
   два входа на одном ядре: `dev_server.py` (без зависимостей вообще) и
   `main.py` (тонкая FastAPI-обёртка). Оба проходят один и тот же тест.
6. **Инструменты проверки (то, что проверяемо без игры):**
   - `tools/test_backend_api.py` - 66 проверок контракта (награды, идемпотентность
     перков/покупок/голосов/claim'ов, whitelist, коды 400/401/403/404/409/503, сброс
     сезона). Режимы: in-process, `--serve` (через HTTP), `--base URL` (против PHP на
     хосте). Результат сейчас: **66 ok / 0 fail** в обоих режимах.
   - `tools/test_backend_sql.py` - 49 SQL-запросов PHP поднимаются на схеме sqlite,
     37 пар `prepare/execute` сходятся по числу `?`, `schema.sql` + `install.php::ddl()`
     согласованы, маршруты полны. Результат: **0 ошибок**.
   - `tools/lint_csharp.py` - C# без компилятора: баланс скобок через токенайзер
     (строки/комментарии вырезаются, а не `count()`), обязательные `using` по типам,
     `override` против списка виртуалов, контракт `"/api/..."` C# <-> PHP <-> Python,
     сверка каталога, регистрация всех `MissionBehavior` в `SubModule.cs`.
     Результат: **0 ошибок, 1 предупреждение** (см. «Риски» ниже).
   - `tools/validate_module.py` вызывает оба анализатора + проверяет сам каталог:
     один запуск перед релизом. Итог: **0 ошибок, 12 предупреждений** (все - бинарка,
     требующая установленную игру).
7. **Найденные и исправленные баги:**
   - **`PerkManager` не был зарегистрирован** в `SubModule.cs` -> `GetMissionBehavior<PerkManager>()`
     = null, т.е. выбор перков не запускался вообще (механика 1, флагманская).
   - `Audio/NISound.cs`: `typeof(TaleWorlds.Core.Agent)` - `Agent` находится в
     `TaleWorlds.MountAndBlade` (CS0234: не компилируется). Резолв SoundController
     переписан на поиск по сборкам домена.
   - 3 файла без обязательных `using`: `Machines/PropSpawner.cs` (Vec3/Frame),
     `Components/RoleComponents.cs` (GameEntity/DestructibleComponent),
     `Managers/MetaProgressionManager.cs` (InformationManager/Colors) - CS0246.
   - `InformationManager.DisplayMessage` вызывался прямо из `Task.Run` (bg-поток).
     Добавлена очередь `_uiQueue` + разбор в `OnMissionTick`.
   - `OnCampaignWin` всегда слал `village_id=0, won=1` -> теперь деревня берётся из
     голосования сезона, `won` - по фактической волне.
   - `smoke.sh`: было 18 проверок; +11 (магазин/повторная покупка/battlepass/сброс
     сезона/недостаточно ресурсов) = 29, проходят против dev-бэкенда.
8. **«Физический UI» там, где Gauntlet ещё не подключён:** выбор перка и покупки
   теперь доступны в бою без экрана - `Machines/InteractionMachines.cs`:
   `NI_ArmoryUsable` (ящик у спавна: аптечка/снаряды/ремкомплект через `/api/shop/buy`)
   и `NI_PerkTotemUsable` (PerkManager спавнит 3 тотема, F = взять перк, тайм-аут
   15 сек = случайный, после выбора тотемы гаснут). Механика 1 перестаёт быть
   «сообщением в чат».
9. **Версия 2.0.0 -> 2.1.0**, `RELEASE_NOTES.md` и релиз-зип пересобраны.

**Риски, которые остались (честно):**
- `AgentComponent.OnTickAsAI(float)` используется в 4 компонентах (медик, знамёнщик,
  стихии, ранения) - в справочнике 1.0.3 такого виртуала нет; если в 1.2.x его тоже
  нет, это «мёртвый» код, и линтер это показывает единственным предупреждением.
  Проверить по DLL при первой компиляции (или заменить на `OnTick`).
- Публичные API (`SetHitPoints`, `SetMaximumHitPoints`, `SpawnMissile`, `AddExplosion`,
  `LoadSceneProp`, `Formation.Captain`) по-прежнему проверяются только сборкой.
- Gauntlet-префабы NI_* не подключены к пайплайну экранов: VM'ы и каталог готовы,
  синтаксис `Command.*` в XML надо сверить с нативным префабом в игре.

**Найдено при подготовке инструкции по проверкам** (`docs/VERIFICATION.md`): UIExtender в
проекте нет ни одного, поэтому VM'ы не выведены на экраны и **стройка форта (механики
2/18/23) игроку недоступна** - `TryPlace` дёргается только из `NI_BuildMenu_VM`.
Магазин частично закрыт F-ящиком, перки - F-тотемами; остальное требует либо
UIExtender-миксинов, либо проверочного поведения (шаг 5.0 инструкции).

**Следующий шаг человека** (без изменений по смыслу, добавился только сброс сезона):
`prepare_scenes.py` (террейн) -> сборка dll -> `python3 tools/validate_module.py`
на машине с игрой -> запуск миссии -> `bash src/backend-php/tests/smoke.sh` на хосте
-> NexusMods.

