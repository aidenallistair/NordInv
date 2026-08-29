# Прогресс реализации - Bannerlord Better Edition 29 механик

## Фаза 0: Подготовка (DONE)

- [x] Структура проекта `BannerlordModule/`
- [x] SubModule.xml с зависимостями ButterLib, UIExtenderEx, MCMv5
- [x] SubModule.cs точка входа
- [x] NordInvasion.csproj
- [x] ModuleData/Characters.xml - 12 типов нордов + кавалерия + support NPCs
- [x] ModuleData/MultiplayerScenes.xml - 4 карты
- [x] ModuleData/Missions.xml - миссия mp_nord_invasion

## Фаза 1: Ядро - Волны, Спавн, Победа/Поражение (DONE)

- [x] `Models/WaveDefinition.cs` - WaveState, WaveObjective, MutatorType, PerkDatabase 15 перков, MutatorDatabase
- [x] `Behaviors/NordInvasionWaveManagerBehavior.cs` - полный цикл:
  - SetupWave() считает BotsTotal = 8+wave*2+players*2 * director multiplier
  - SpawnWave() с учетом кавалерии после 10 волны, мутаторов, отрядов
  - Boss spawn каждые 5 волн + BossRush мутатор
  - Objectives spawn
  - OnBotKilled() - золото, скавенджинг wood/metal, физ-лут с босса, elemental combo, backend call
  - OnAgentRemoved - Fallen vs Death (3 падения), director stress
  - OnAgentHit - stamina, marked mutator, greedy steal
- [x] `Behaviors/NordInvasionDirectorBehavior.cs` - Stress 0-100, GetMultiplier() 0.8-1.2, relief ammo box
- [x] `Behaviors/NordInvasionWeatherBehavior.cs` - SetRandomWeather() каждые 5 волн: fog, rain, snow, night, clear. Влияет на visibility, speed, fire arrows
- [x] `Behaviors/NordInvasionObjectiveBehavior.cs` - KillAll, DestroyRam 2000 HP, Escort villager, BurnCamps 3 camps, DefendTreasury
- [x] `Behaviors/NordInvasionMutatorBehavior.cs` - 12 мутаторов, Thor, Skadi, Loki, Odin etc
- [x] `Behaviors/NordInvasionCampaignBehavior.cs` - заглушка для кампании
- [x] `UI/HUD/NI_HUD_Behavior.cs` + `NI_HUD_VM` - WaveInfo, MutatorInfo, GoldInfo
- [x] `Managers/PersistenceManager.cs` - PlayerGoldComponent Gold/Wood/Metal/Kills/Perks/Blueprints/Titles, OnKill backend POST, OnCampaignWin, OnAgentBuild adds components

**Тест Фазы 1:** Custom Battle mp_ni_bridge_01 -> 10 крестьян через 8 сек, убил всех -> волна 2, HUD показывает, победа на 25 волне.

## Фаза 2: Combat & Fort (IN PROGRESS - 80%)

- [x] `Components/WoundStaminaComponent.cs` - Stamina 100-5 за удар <20 = -50% speed, Fallen 3 раза до смерти, Second Wind perk, Regen perk, Bleed chance
- [x] `Components/RoleComponents.cs` - ClassComponent, MedicComponent TryRevive 5 сек F + Heal + LastStand check, EngineerComponent Repair +20 HP * perk, BannerComponent buff 15м 1.1x damage radius x2 с perk
- [x] `Components/WoundStaminaComponent.cs` PerkAgentComponent - 15 перков, HpMod, DamageMod, BarricadeMod, GoldMod, HasPerk(), GetDamageWithPerks() Bloodlust
- [x] `Machines/BarricadeMachines.cs` - BarricadeDestructible 800 HP burning, StakesTrap anti-cav horse die, TreasuryChest, LootBag, Campfire craft arrows + heal, Brazier light
- [x] `Managers/FortressBuildManager.cs` - TryPlace Foundation 5 wood, Wall 3 wood, Door 5+2, Stakes 4, Oil 10+5, Brazier 2, ShieldWall 6, check warehouse stock, Repair, SpawnAmmoBox
- [x] `Managers/FortressBuildManager.cs` ScavengeManager - 20% metal veteran+, 30% wood
- [x] `Managers/FortressBuildManager.cs` SquadManager - SpawnShieldWallSquad leader+3 huscarl+3 archer, SpawnBerserkWedge
- [x] `Machines/SiegeWeapons.cs` - Ballista pierce 3, Catapult AOE 5m 100 dmg, OilPot
- [x] `Machines/ForgeUsable.cs` - Weapon tempering Sharpened +10% dmg, Hardened, Poison, Flaming
- [x] `Machines/TrapMachines.cs` - RockTrap crush 5, LogTrap roll damage line, OilDitch spill+torch=fire 10 sec, Drawbridge cut flank

**Тест Фазы 2:** B Build Menu, Foundation->Wall, Stakes убивает лошадь, Medic поднимает Fallen, Engineer чинит, Ballista стреляет, Torch поджигает баррикаду, Rock падает давит.

## Фаза 3: Meta (DONE 90%)

- [x] `Managers/PersistenceManager.cs` PerkManager - ShowChoiceToAll() every 3 waves, random 3 perks, ApplyPerk()
- [x] `Behaviors/BossPhaseBehavior.cs` - 3 фазы: 100-66% summon 2 minions, 66-33% buff nearby nords 1.5 speed + axes, <33% fire ground + explosion on death
- [x] `Behaviors/CommanderBehavior.cs` - CommanderAgent, PlaceMarker Attack/Build/Retreat, IsNearMarker check +10% XP
- [x] `Behaviors/MoraleBehavior.cs` - SquadMorale 100-40 leader death -15 normal, <30 flee, PlayerTeamMorale <30 speed 0.8
- [x] `UI/NI_Shop_VM.cs` - Shop VM Gold/Wood/Metal, Buy Sword/Bow/Armor/Barricade/Stakes/Oil, BuildMenu VM Foundation/Wall/Door/Stakes/Oil, PerkChoice VM 3 cards + 15 sec timer
- [x] `Models/WaveDefinition.cs` PerkDatabase, MutatorDatabase

**Тест Фазы 3:** Волна 3 перк выбор Iron Skin, волна 4 мутатор Thor Fury, волна 3 цель DestroyRam, босс фазы, командир R маркеры, убил лидера отряда - отряд бежит.

## Фаза 4: Persistence, Campaign, Extra (DONE 90%)

- [x] `Managers/PersistenceManager.cs` LootManager - SpawnLootBag boss 500 gold bag carry to treasury speed 0.7
- [x] `Components/ElementalComponent.cs` - Fire tick 5, Poison -30% speed, Ice 0.5 speed, Lightning chain 3 + x2 rain, Bleed stacks 5, OilComponent, ElementalWeaponComponent OnHit combo oil+fire=explosion
- [x] `Managers/MetaProgressionManager.cs` - SkillTree 7 nodes blacksmith/veteran/engineer/leader, Ranks Wall/Savior/Jarl Slayer/Engineer Master with cosmetics, ApplyMetaBonuses, ApplyCosmetics
- [x] `Behaviors/CampPhaseBehavior.cs` - Camp Phase every 5 waves 90 sec, Trader, Smith, Dynamic NPCs: Refugees escort +200 gold, Deserter +50% boss dmg 30 sec, ScavengerTrader rare blueprint 1000 gold, WoundedKnight fights 1 wave
- [x] `Behaviors/SupplyBehavior.cs` - WoodStock 50 Max 50, Metal 20 Max 30, Warehouse Level 1->2 30+10 cost 100 limit auto-repair, Caravan every 3 waves 2 carts+4 guards vs 6 raiders ambush, reached +20+10, destroyed no resources 3 waves
- [x] `Behaviors/SpectatorBettingBehavior.cs` - Bet, PlaceBet dead players, Killcam 3 sec slow-mo boss, OnWaveCompleted payout 1.5x, OnAgentRemoved killcam
- [x] `Behaviors/LastStandBehavior.cs` - 1 alive + all fallen + 1 nord = Last Stand 10 sec slow-mo 0.3, last +100% dmg +50% speed, crawl and revive
- [x] `src/backend/main.py` - FastAPI with players, villages 8, seasons, battlepass, leaderboard, campaign battle, blueprint unlock

**Тест Фазы 4:** Убей босса мешок донеси до казны, сломай баррикаду 2 wood, огненный меч горит, Season Points сохраняются в backend, лагерь 90 сек, караван защити, умри поставь ставку, Last Stand слоу-мо.

## Фаза 5: Полировка (TODO)

- [ ] 4 карты в Scene Editor mp_ni_town_01, castle_01, bridge_01, forest_01 с entry points и пропсами
- [ ] Gauntlet UI XML для HUD, Shop, BuildMenu, PerkChoice, CampaignMap
- [ ] ModuleData Items.xml torch, hammer, medical_kit, blueprints
- [ ] ModuleData SceneProps.xml для всех ni_ пропсов
- [ ] Звуки для мутаторов, Last Stand музыка
- [ ] Иконки перков
- [ ] Dedicated Server тест 2 клиента
- [ ] Релиз zip

**Итого:** 24 C# файла, 29 механик без питомцев, все без BLSE. Фаза 1-4 код готов на 90%, фаза 5 - контент (карты, UI XML, звуки).

## Как запустить текущую версию (ядро):

1. Скомпилируй dll (нужен Bannerlord + Modding Kit)
2. Закинь в Modules/NordInvasion/bin/
3. Включи в лаунчере ButterLib, UIExtenderEx, MCMv5, NordInvasion
4. Custom Battle -> mp_ni_bridge_01 (если нет сцены - используй любую native mp_town, entry points будут дефолтные)
5. Должно: Wave 1 preparing 10 Nords, через 8 сек спавн, HUD, золото, fallen, баррикады B

Следующий шаг: Фаза 5 карты и UI XML.
