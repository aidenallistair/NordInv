# Nord Invasion Better Edition - План под Bannerlord

Нативный мод для Bannerlord на C# с 15 механиками.

## Стек

- **Bannerlord 1.2.10+**
- **.NET Framework 4.7.2** / .NET 6 для новых версий
- **Harmony 2.2.2**
- **ButterLib + UIExtenderEx + Mod Configuration Menu v5** - база
- **Backend:** FastAPI из `src/backend/main.py`

Два режима:
1. **Co-op Campaign (рекомендуемый для старта):** Мод `Bannerlord Co-op` - 4-8 игроков в одной миссии. Самый стабильный.
2. **Dedicated MP:** Через `TaleWorlds.MountAndBlade.DedicatedCustomServer` + кастомный MP Mission.

## Структура проекта

```
Modules/NordInvasion/
  SubModule.xml
  ModuleData/
    Languages/RU/
    Missions/
    Scenes/mp_ni_* 
  bin/Win64_Shipping_Client/NordInvasion.dll
  src/
    SubModule.cs
    Behaviors/
      NordInvasionWaveManagerBehavior.cs - ядро волн
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
      NI_HUD_VM.cs
      NI_Shop_VM.cs
      NI_PerkChoice_VM.cs
      NI_BuildMenu_VM.cs
      NI_CampaignMap_VM.cs
    Models/
      WaveDefinition.cs
      MutatorDefinition.cs
      PerkDefinition.cs
      VillageDefinition.cs
```

## Ядро - WaveManager

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
        int playerCount = Mission.Current.PlayerTeam.ActiveAgents.Count;
        int baseCount = 8 + WaveNumber * 2 + playerCount * 2;
        baseCount = (int)(baseCount * Director.GetMultiplier());

        var tier = GetTiersForWave(WaveNumber);
        if (Mutator == Berserk) tier = AllBerserk;

        if (WaveNumber % 3 == 0) SquadManager.SpawnShieldWallSquad(entry: 32);

        for (int i=0; i<baseCount; i++)
        {
            var troop = tier.GetRandom();
            if (WaveNumber >=10 && Rand() < 0.2f) troop = NordRaiderMounted;
            Mission.Current.SpawnAgent(new AgentBuildData(troop).Team(NordTeam).InitialPosition(GetNordEntry(i)));
        }

        if (WaveNumber %5==0) SpawnBoss();
        if (Mutator == BossRush) SpawnBoss(3);
        if (Objective == DestroyRam) SpawnRam();
    }
}
```

## Реализация 15 механик

### 1. Roguelite перки
- `PerkDefinition` с 30 перками, 3 ветки: Survivor, Berserk, Tactician.
- При `OnWaveCompleted` если `wave%3==0` -> `PerkManager.ShowChoice(player)` -> Gauntlet UI 3 карточки.
- `PerkAgentComponent` меняет `AgentDrivenProperties`: `MaxHitPoints`, `DamageMultiplier`.
- Хранится в `MissionBehavior` до конца забега.

### 2. Модульный форт
- `BarricadeDestructible : DestructibleComponent`.
- `FortressBuildManager` - игрок открывает BuildMenu (B), выбирает `Foundation`, тратит wood/metal.
- Размещение: raycast на землю, проверка коллизий.
- Апгрейд: `Foundation` -> `Wall` -> `WallWithDoor`. Каждый - отдельный `SceneProp` с `DestructibleComponent`.

### 3. Роли
- `MedicAgentComponent`: если цель `Fallen` и дистанция <2м, удержание F 5 сек -> `Revive()`.
- `EngineerAgentComponent`: `OnUse` на баррикаде -> `Repair(20 HP)`, может ставить Tier2.
- `BannerAgentComponent`: `OnTick` ищет союзников в радиусе 15м, дает `DamageBuff = 1.1f`.
- Выбор класса через UI при спавне.

### 4. Цели волн
- `ObjectiveBehavior`:
  - `DestroyRam`: спавнит `RamSiegeMachine` который едет к воротам, 2000 HP.
  - `Escort`: спавнит `VillagerAgent` с `EscortBehavior` по точкам.
  - `BurnCamps`: 3 `CampDestructible` за картой, поджечь факелом.
- Провал цели = поражение.

### 5. AI-Директор (L4D2)
- Считает `K/D`, `GoldPerMinute`, `AliveRatio` каждую секунду.
- `Stress 0-100`: <30 - команда проигрывает, >80 - выигрывает.
- <20: спавнит `AmmoBox`, респавнит 1 игрока, туман.
- >80: `BotsTotal *=1.2`, фланговая атака, 2 босса.

### 6. Погода и время
- `WeatherBehavior`: каждые 5 волн `SetRainDensity()`, `SetFog()`, `SetTimeOfDay()`.
- Туман: `SetVisibilityMultiplier(0.5f)`, боты агрятся в 20м.
- Дождь: огненные стрелы не работают.
- Ночь: выключает солнце, включает `Torch`, игроки ставят `Brazier`.
- Снег: `SetSpeed(0.9f)`.

### 7. Кавалерия
- `CharacterObject` `ni_nord_raider` с лошадью.
- `StakesTrap` - при контакте лошади `Horse.Die()`.
- Спавн с флангов (entry 56-63), AI `ChargeTactic`.

### 8. Физический лут
- При смерти босса спавнит `LootBag` UsableMachine.
- Игрок `OnUse` -> `AttachProp(bag)`, `Speed 0.7`.
- Донести до `TreasuryChest` -> +500 золота.
- Боты могут украсть мешок.

### 9. Скавенджинг
- При убийстве ветерана 20% `scrap_metal`.
- При разрушении баррикады `wood_plank x2`.
- У костра `Campfire`: 3 wood -> стрелы.

### 10. Мутаторы Богов
- 12 мутаторов, каждую 4 волну рандом.
- `Thor: Speed 1.5 + NoBlock`, `Loki: Gold x2 but steal on hit`, `Odin: Marked player, all bots chase him`.
- HUD иконка.

### 11. Отряды с формациями
- Используем нативный `Formation`.
- `SpawnShieldWallSquad()`: Лидер с баннером + 3 Huscarl ShieldWall + 3 Archer позади.
- Лидер - `Formation.Captain`.

### 12. Персистенция 2.0
- `PersistenceManager` + `HttpClient` -> `src/backend/main.py`.
- `SteamId` -> `POST /api/player/login`.
- Сохраняет `gold, wood, metal, blueprints, season_points`.
- Кэш json в `Documents/.../Configs/NordInvasion/`.
- Сезоны и BattlePass.

### 13. Ранения и усталость
- `WoundStaminaComponent`:
  - `Stamina 0-100`, удар -5, <20 -> Damage 0.5.
  - При 0 HP - `Fallen` (лежит), 3 падения = смерть.
  - Медик поднимает.

### 14. Разрушаемость и огонь
- `DestructibleComponent` + `FireManager`.
- `Tree`: при уничтожении спавнит `FallenTree` блокирующий проход.
- `PowderBarrel`: взрыв `AddExplosion()`, урон в радиусе.
- Факел поджигает деревянные пропсы.

### 15. Глобальная кампания
- `CampaignBehavior` + `VillageManager`.
- 8 деревень, игроки голосуют в `CampaignMap_VM`.
- После битвы `POST /api/campaign/battle` меняет владельца.

## Фазы реализации

**Фаза 1 (неделя 1): Core**
- Проект NordInvasion, WaveManager, спавн ботов, победа/поражение
- Карта `mp_ni_bridge_01` в Scene Editor
- Тест 1 волна 10 ботов

**Фаза 2 (неделя 2): Combat & Fort (3,7,11,13,2,14)**
- Роли, кавалерия, отряды, ранения, баррикады, огонь

**Фаза 3 (неделя 3): Meta (1,4,6,10,5)**
- Перки UI, цели, погода, мутаторы, директор

**Фаза 4 (неделя 4): Persistence & Campaign (12,15,8,9)**
- Backend, лут-мешки, скавенджинг, карта кампании, сезоны

## Dedicated Server

- Через SteamCMD: `app_update 1058080` (Bannerlord Dedicated Server)
- Запуск: `DedicatedCustomServer.exe /dedicatedcustomserverconfig DedicatedCustomServerConfig.xml`
- Конфиг в `DedicatedServer/Bannerlord/`

## Что нужно

1. Bannerlord + Modding Kit (Steam Tools)
2. Скелет уже в `BannerlordModule/` - скомпилируй в `Modules/NordInvasion/bin/`
3. Для коопа - мод Bannerlord Co-op
