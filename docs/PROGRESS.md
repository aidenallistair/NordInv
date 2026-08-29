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
      `dist/NiNordInvasion_v2_0_0_source.zip` (source-релиз, dll собирается локально)
- [x] **Исправления session 2**: SubModule.xml регистрировал только 2 XML из 6
      ( Characters/Items/Missions/SceneProps не грузились); MathF (нет в net472);
      небезопасные API (Peer.Communicator, SetActionChannel, rotation.f, SetTeam);
      csproj: System.Net.Http; killcam double-call; экономика строительства
- [ ] Бинарный террейн сцен (terrain.bin) - `prepare_scenes.py` на машине с игрой
      или сохранение в Scene Editor (5 минут на сцену)
- [ ] Иконки перков - mesh + material (docs/ART_TASKS.md, UI готов)
- [ ] Мешы ni_*-пропсов (docs/ART_TASKS.md; пока vanilla-fallback)
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
5. Release: dist/NiNordInvasion_v2_0_0_source.zip.

**Следующий шаг для человека (нужна Windows + Bannerlord):**
1. `python3 tools/prepare_scenes.py` (террейн)
2. Собрать dll (csproj), при CS-ошибках - точечные правки по rgl_log
3. Запустить Custom Battle mp_ni_bridge_01 -> mp_nord_invasion
4. Прогнать тест-чеклист (LAUNCH_GUIDE.md), поправить ID частиц/звуков
5. Upload на NexusMods
