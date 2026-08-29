# Nord Invasion Better Edition - План под Bannerlord

Переквалификация с Warband (Python Module System) на Bannerlord (C# + Harmony). Это не порт, это ремейк с учетом нового движка.

## Почему Bannerlord лучше для NI

| Было в Warband | Стало в Bannerlord |
|----------------|--------------------|
| Лимит 100-150 ботов, костыль через WSE | До 800+ агентов нативно, `Mission.SpawnAgent` |
| Тупой AI, бежит толпой | Нормальные формации, `FormationAI`, `TacticComponent` |
| Нет разрушаемости | Есть `DestructibleComponent`, `SiegeWeapon`, огонь |
| Нет физики строительства | Есть `UsableMachine`, `Construction` как в осадах |
| Презентации - костыль | `Gauntlet UI`, `ViewModel`, нормальный UI |
| Слоты игроков - хак | `AgentComponent`, `MissionBehavior`, `NetworkCommunicator` |
| WSE нужен для HTTP | `HttpClient` в C# из коробки |

## Стек

- **Bannerlord 1.2.10+** (последний стабильный)
- **.NET Framework 4.7.2** / .NET 6 для новых версий
- **Harmony 2.2.2** - патчи
- **ButterLib + UIExtenderEx + Mod Configuration Menu v5** - база для всех модов
- **Bannerlord.MP** или **Bannerlord Co-op** как референс для мультиплеера
- **Backend:** тот же FastAPI из `src/backend/main.py` (уже готов)

Два режима запуска:
1. **Co-op Campaign (рекомендуемый для старта):** Используешь мод `Bannerlord Co-op` / `Bannerlord Online Coop` - 4-8 игроков в сингл-миссии. Самый стабильный путь.
2. **Dedicated MP:** Через `TaleWorlds.MountAndBlade.DedicatedCustomServer` + кастомный `Multiplayer Mission`. Сложнее, но возможно (как в MP модах `Full Invasion: Bannerlord`).

Начнем с (1), потом добавим (2).

## Структура проекта Bannerlord

```
Modules/NordInvasion/
  SubModule.xml
  ModuleData/
    Languages/RU/
    Missions/ - кастомные миссии
    Scenes/mp_ni_* - карты
  bin/Win64_Shipping_Client/NordInvasion.dll
  src/
    SubModule.cs
    Behaviors/
      NordInvasionWaveManagerBehavior.cs - ядро волн (механика 0)
      NordInvasionDirectorBehavior.cs - AI-Директор (5)
      NordInvasionWeatherBehavior.cs - погода (6)
      NordInvasionObjectiveBehavior.cs - цели (4)
      NordInvasionMutatorBehavior.cs - мутаторы (10)
      NordInvasionCampaignBehavior.cs - кампания (15)
    Components/
      MedicAgentComponent.cs - медик (3)
      EngineerAgentComponent.cs - инженер (3)
      BannerAgentComponent.cs - знаменосец (3)
      WoundStaminaComponent.cs - ранения (13)
      PerkAgentComponent.cs - перки (1)
      LootBagComponent.cs - лут (8)
    Machines/
      BarricadeDestructible.cs - баррикады (2,14)
      StakesTrap.cs - колья (7)
      OilCauldronUsable.cs - котел (2)
      TreasuryChestUsable.cs - казна (8)
      RamSiegeMachine.cs - таран (4)
      CampfireUsable.cs - костер крафт (9)
    Managers/
      FortressBuildManager.cs - строительство (2)
      ScavengeManager.cs - лут с трупов (9)
      PerkManager.cs - выбор перков (1)
      PersistenceManager.cs - SteamID + backend (12)
      SquadManager.cs - отряды с формациями (11)
    UI/
      NI_HUD_VM.cs - HUD волны/мутатора/погоды
      NI_Shop_VM.cs - магазин
      NI_PerkChoice_VM.cs - выбор перка
      NI_BuildMenu_VM.cs - стройка
      NI_CampaignMap_VM.cs - карта кампании
    Models/
      WaveDefinition.cs
      MutatorDefinition.cs
      PerkDefinition.cs
      VillageDefinition.cs
```

## Ядро - WaveManager (Layer 0)

В Warband это были `mission_templates` триггеры. В Bannerlord - `MissionBehavior`.

```csharp
public class NordInvasionWaveManagerBehavior : MissionBehavior
{
    public int WaveNumber = 1;
    public int BotsAlive = 0;
    public int BotsTotal = 0;
    public WaveState State = WaveState.Preparing;
    public WaveObjective Objective = WaveObjective.KillAll;
    public MutatorType Mutator = MutatorType.None;
    
    public override void OnMissionTick(float dt)
    {
        if (State == Preparing && Mission.CurrentTime > NextWaveTime)
            SpawnWave();
        if (BotsAlive <= 0 && State == InProgress)
            OnWaveCompleted();
    }

    void SpawnWave()
    {
        // Director влияет на количество
        int playerCount = Mission.Current.PlayerTeam.ActiveAgents.Count;
        int baseCount = 8 + WaveNumber * 2 + playerCount * 2;
        baseCount = (int)(baseCount * Director.GetMultiplier());

        // Выбираем тиры по волне
        var tier = GetTiersForWave(WaveNumber);

        // Мутатор Berserk -> все берсерки
        if (Mutator == Berserk) tier = AllBerserk;

        // Спавн отрядов (механика 11)
        if (WaveNumber % 3 == 0) SquadManager.SpawnShieldWallSquad(entry: 32);

        // Спавн кавалерии (7) после 10 волны
        for (int i=0; i<baseCount; i++)
        {
            var troop = tier.GetRandom();
            if (WaveNumber >=10 && Rand() < 0.2f) troop = NordRaiderMounted;
            Mission.Current.SpawnAgent(new AgentBuildData(troop).Team(NordTeam).InitialPosition(GetNordEntry(i)));
        }

        // Босс
        if (WaveNumber %5==0) SpawnBoss();
        if (Mutator == BossRush) SpawnBoss(3);

        // Цель (4)
        if (Objective == DestroyRam) SpawnRam();
    }
}
```

Bannerlord умеет `Mission.SpawnAgent` динамически без лимитов - то что в Warband требовало WSE.

## Реализация 15 механик в терминах Bannerlord

### 1. Roguelite перки
- `PerkDefinition` ScriptableObject с 30 перками.
- При `OnWaveCompleted` если `wave%3==0` -> `PerkManager.ShowChoice(player)` -> Gauntlet UI 3 карточки.
- `PerkAgentComponent` навешивается на агента, меняет `AgentDrivenProperties`: `MaxHitPoints`, `DamageMultiplier`.
- Сохраняется в `MissionBehavior` до конца забега.

### 2. Модульный форт
- Используем систему осадных машин как базу. `BarricadeDestructible : DestructibleComponent`.
- `FortressBuildManager` - игрок открывает BuildMenu (B), выбирает `Foundation`, тратит ресурсы (wood/metal из `PersistenceManager`).
- Размещение: `Mission.Current.GetMousePosition()` + raycast на землю, проверка коллизий.
- Апгрейд: `Foundation` -> `Wall` -> `WallWithDoor`. Каждый - отдельный `SceneProp` с `DestructibleComponent`.
- Ресурсы падают с баррикад и трупов (9).

### 3. Роли
- `MedicAgentComponent`: при `OnAgentHit` если цель `Fallen` (а не Dead) и дистанция <2м, удержание F 5 сек -> `Revive()`. Дает XP.
- `EngineerAgentComponent`: `OnUse` на баррикаде -> `Repair(20 HP)`, может ставить Tier2.
- `BannerAgentComponent`: `OnTick` ищет союзников в радиусе 15м, дает `DamageBuff = 1.1f` через `AgentDrivenProperties`.
- Выбор класса через UI при спавне, сохраняется в `Team`.

### 4. Цели волн
- `ObjectiveBehavior`:
  - `DestroyRam`: спавнит `RamSiegeMachine` который едет к воротам. Игроки должны нанести 2000 урона.
  - `Escort`: спавнит `VillagerAgent` с `EscortBehavior` (идет по точкам), игроки защищают.
  - `BurnCamps`: 3 `CampDestructible` за картой, надо поджечь факелом (`OnHit` с torch).
- Если цель провалена - поражение.

### 5. AI-Директор (Left 4 Dead)
- `DirectorBehavior` считает каждую секунду: `K/D`, `GoldPerMinute`, `AliveRatio`.
- `Stress 0-100`: <30 - команда вайпается, >80 - тащит.
- Если stress <20: спавнит `AmmoBox`, респавнит 1 игрока, включает туман.
- Если stress >80: `BotsTotal *=1.2`, спавнит фланговую атаку, 2 босса.
- Влияет на `WaveManager`.

### 6. Погода и время
- `WeatherBehavior`: каждые 5 волн меняет `Mission.Scene.SetRainDensity()`, `SetFog()`, `SetTimeOfDay()`.
- Туман: `Agent.SetVisibilityMultiplier(0.5f)` для лучников, боты агрятся только в 20м.
- Дождь: `FireArrows` не работают (`OnAgentShoot` проверка).
- Ночь: выключает солнце, включает `Torch` у агентов, игроки ставят `Brazier`.
- Снег: `Agent.SetSpeed(0.9f)`.

### 7. Кавалерия
- Добавляем `CharacterObject` `ni_nord_raider` с лошадью.
- `StakesTrap : MissionBehavior` - при `OnAgentHit` если агент на лошади и дистанция <2м -> `Horse.Die()`, `Agent.Die()`.
- Спавн с фланговых entry (56-63), AI `ChargeTactic`.

### 8. Физический лут
- При смерти босса `LootBagComponent` спавнит `UsableMachine` мешок.
- Игрок `OnUse` -> `Agent.AttachProp(bag)`, `SpeedModifier 0.7`.
- Донести до `TreasuryChestUsable` -> `OnUse` -> +500 золота, `DetachProp`.
- Если норд-бот подходит к мешку - крадет (уничтожает мешок).

### 9. Скавенджинг
- `ScavengeManager`: при `OnAgentKilled` (норд-ветеран) 20% шанс `SpawnItem(scrap_metal)`.
- При `OnDestructibleDestroyed` (баррикада) -> `SpawnItem(wood_plank x2)`.
- У костра `CampfireUsable`: `OnUse` если есть 3 wood -> `GiveAmmo(arrows)`.

### 10. Мутаторы Богов
- `MutatorDefinition` с 12 мутаторами.
- Каждую 4 волну `MutatorManager.PickRandom()`.
- Применяет глобальные модификаторы: `Thor: AllAgents.SetSpeed(1.5f) + NoBlock`, `Loki: Gold x2 but OnHit StealGold`, `Odin: MarkedPlayer = random, AllBots.SetTarget(Marked)`.
- HUD иконка.

### 11. Отряды с формациями
- Bannerlord уже имеет `Formation`. Используем.
- `SquadManager.SpawnShieldWallSquad()`:
  - Лидер `ShieldLeader` с `Banner`
  - 3 `Huscarl` с `FormationClass.Infantry` + `ShieldWall` tactic
  - 3 `Archer` с `FormationClass.Ranged` позади
- Лидер - `Formation.Captain`, остальные `Follow`.

### 12. Персистенция 2.0
- `PersistenceManager` использует `HttpClient` -> наш `src/backend/main.py` (уже готов, поддерживает blueprints, seasons).
- При `OnPlayerConnect` -> `SteamId = NetworkCommunicator.GetSteamId()` -> `POST /api/player/login`.
- Сохраняет `gold, wood, metal, blueprints, season_points`.
- Локальный кэш `json` в `Documents/Mount and Blade II Bannerlord/Configs/NordInvasion/`.
- Сезоны и BattlePass как в backend.

### 13. Ранения и усталость
- `WoundStaminaComponent : AgentComponent`:
  - `Stamina 0-100`, каждый удар -5, <20 -> `DamageMultiplier 0.5`.
  - `Wounds`: при 0 HP не смерть, а `Fallen` (лежит, анимация wounded). 3 падения -> смерть.
  - Медик может поднять.
- `OnAgentHit` -> `Stamina -= damage/10`.

### 14. Разрушаемость и огонь
- Bannerlord уже имеет `DestructibleComponent` и `FireManager`.
- `TreeDestructible`: при уничтожении спавнит `FallenTree` блокирующий проход.
- `PowderBarrelDestructible`: при уроне взрывается, `Mission.Current.AddExplosion()`, урон в радиусе.
- Факел: `Torch` item, `OnHit` на деревянный проп -> `SetBurning(true)`, particle fire, тик урона.

### 15. Глобальная кампания
- `CampaignBehavior : CampaignBehaviorBase` (для сингла) или `VillageManager` для MP.
- Карта 8 деревень (как в backend). Игроки голосуют в `CampaignMap_VM` (Gauntlet UI с картой).
- После битвы `POST /api/campaign/battle` -> деревня меняет владельца.
- Открывает новые карты.

## Фазы реализации под Bannerlord

**Фаза 1 (неделя 1): Core + MP база**
- Создать проект `NordInvasion`, SubModule, WaveManager, спавн ботов, победа/поражение
- Карта `mp_ni_bridge_01` порт из Warband (через Bannerlord Scene Editor)
- Тест с 1 волной, 10 ботами

**Фаза 2 (неделя 2): Combat & Fort (3,7,11,13,2,14)**
- Роли, кавалерия, отряды, ранения, баррикады, огонь
- Это то, что в Bannerlord делается в 2 раза проще чем в Warband

**Фаза 3 (неделя 3): Meta (1,4,6,10,5)**
- Перки UI, цели, погода, мутаторы, директор
- Gauntlet UI для всех

**Фаза 4 (неделя 4): Persistence & Campaign (12,15,8,9)**
- Backend интеграция, лут-мешки, скавенджинг, карта кампании, сезоны

## Dedicated Server для Bannerlord

Для Bannerlord MP мода нужен Dedicated Server:

- Через SteamCMD: `app_update 1058080` (Bannerlord Dedicated Server) или `steamcmd +login anonymous +app_update 440900` (сам Bannerlord, там есть `bin/Win64_Shipping_Client/TaleWorlds.MountAndBlade.DedicatedCustomServer.exe`)
- Запуск: `DedicatedCustomServer.exe /mpModule NordInvasion /mission mp_ni_bridge_01 /port 7240`
- Конфиг в `DedicatedCustomServerConfig.xml`

В репо добавим папку `DedicatedServer/Bannerlord/` с примером конфига и скриптом `steamcmd` для скачки.

## Что нужно от тебя

1. Скачай Bannerlord через Steam, установи `Modding Kit` (в Tools в Steam)
2. Создай проект по структуре выше (я уже создам скелет в `BannerlordModule/`)
3. Скомпилируй `NordInvasion.dll` и закинь в `Modules/NordInvasion/bin/`
4. Для теста коопа - поставь мод `Bannerlord Coop` и запусти миссию через Custom Battle

Готов сделать скелет C# проекта прямо в этом репо?
