# Поэтапный план реализации Nord Invasion Bannerlord - 29 механик

Без питомцев, только Bannerlord, без BLSE для MVP.

## Фаза 0: Подготовка окружения (1 день)

### Что сделать:

1. Установи Bannerlord 1.4.8 через Steam (без War Sails DLC)
2. Установи Modding Kit: Steam -> Library -> Tools -> Mount & Blade II Bannerlord - Modding Kit
3. Внешние моды (ButterLib/UIExtenderEx/MCM) не требуются — в коде не используются;
   (опционально) Bannerlord Co-op 1.2.0+ для теста коопа

4. Склонируй репо:
   ```
   git clone <this repo> C:\BannerlordMods\NordInv
   ```

5. Создай структуру модуля:
   ```
   C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\NordInvasion\
     bin/Win64_Shipping_Client/ (сюда будет компилироваться dll)
     ModuleData/
     SceneObj/
     Languages/RU/
   ```

6. Скопируй из репо:
   - `BannerlordModule/Modules/NordInvasion/SubModule.xml` -> в `Modules/NordInvasion/`
   - `BannerlordModule/NordInvasion.csproj` -> открой в Rider/VS

7. Пропиши пути к DLL в csproj:
   - Правой кнопкой на проекте -> Edit -> HintPath поменяй на твой путь к Bannerlord/bin/
   - Или используй переменную `$(BannerlordPath)`

8. Скомпилируй пустой мод: Build -> должен появиться `NordInvasion.dll` в `Modules/NordInvasion/bin/`

9. Запусти Bannerlord Launcher, включи NordInvasion, запусти Custom Battle - должно написать "Nord Invasion Better Edition v2.0 Loaded!"

**Критерий готовности:** Мод загружается без краша, в логах `rgl_log.txt` нет ошибок.

---

## Фаза 1: Ядро - Волны, Спавн, Победа/Поражение (3 дня)

Цель: 1 карта, 1 волна 10 крестьян, убил всех - победа.

### День 1: WaveManager

1. Создай `Models/WaveDefinition.cs` уже есть
2. Допиши `Behaviors/NordInvasionWaveManagerBehavior.cs`:
   - `SetupWave(1)` - считает `BotsTotal = 8 + wave*2 + players*2`
   - `SpawnWave()` - цикл `Mission.Current.SpawnAgent(new AgentBuildData(troop).Team(attackerTeam).InitialPosition(entryPos))`
   - `OnAgentRemoved` - если attacker умер -> `BotsAlive--`, если 0 -> `OnWaveCompleted()`
   - `OnWaveCompleted()` - `WaveNumber++`, `SetupWave()`, если 25 - победа

3. Создай `ModuleData/Characters.xml`:
   ```xml
   <NPCCharacters>
     <NPCCharacter id="ni_nord_peasant" name="Nord Peasant" ...>
       <EquipmentSet> ... hatchet, shirt ... </EquipmentSet>
     </NPCCharacter>
   </NPCCharacters>
   ```

4. Создай сцену `mp_ni_bridge_01` в Bannerlord Scene Editor:
   - File -> New Scene, terrain plain
   - Place -> Entry Points: 0-31 в форте, 32-63 вокруг, 64 для босса
   - Save as `mp_ni_bridge_01`, скопируй `scene.xscene` в `ModuleData/Scenes/`

5. Тест: Custom Battle -> Scene mp_ni_bridge_01, Mission mp_nord_invasion (создай в ModuleData/Missions/ XML), запусти - должны заспавниться 10 крестьян через 8 сек

### День 2: Director + Weather + Objective + Mutator (база)

1. `DirectorBehavior.cs` уже есть - подключи в SubModule.cs
   - Считает alive ratio каждую секунду, меняет Stress
   - Влияет на BotsTotal в WaveManager

2. `WeatherBehavior.cs` - `SetFog()`, `SetRainDensity()`, `SetTimeOfDay()`
   - Каждые 5 волн `SetRandomWeather()`

3. `ObjectiveBehavior.cs` - заглушка: только KillAll пока, DestroyRam позже

4. `MutatorBehavior.cs` - заглушка: только None и Berserk

5. Тест: Волна 4 - должен выпасть мутатор Berserk (все берсерки, скорость 1.5)

### День 3: Победа/Поражение + HUD

1. Создай `UI/NI_HUD_VM.cs` Gauntlet ViewModel:
   ```csharp
   public class NI_HUD_VM : ViewModel
   {
       [DataSourceProperty] public string WaveInfo { get; set; } // "Wave 1 | Nords 10 | Alive 4"
       [DataSourceProperty] public string MutatorInfo { get; set; }
   }
   ```

2. Создай `ModuleData/GauntletUI/NI_HUD.xml` - 2 текста сверху по центру

3. В WaveManager `OnMissionTick` обновляй VM

4. Тест: HUD показывает волну, живых, мутатор. Если все игроки мертвы - Defeat, если 25 волн - Victory.

**Критерий фазы 1:** 1 карта, волны 1-5 работают, HUD, победа/поражение.

---

## Фаза 2: Combat & Fort - Роли, Кавалерия, Отряды, Ранения, Баррикады, Огонь (7 дней)

### День 4-5: Роли (3) + Ранения (13)

1. `Components/WoundStaminaComponent.cs` уже есть:
   - `Stamina 100`, каждый удар -5, <20 => -50% урона
   - `FallenCount`, при 0 HP не смерть а Fallen (лежит), 3 падения = смерть
   - `Revive()` - встает с 50 HP

2. `Components/MedicAgentComponent.cs` - создай:
   - `OnUse` на Fallen агенте, удержание F 5 сек (таймер), вызывает `Revive()`
   - Дает +5 золота

3. `Managers/FortressBuildManager.cs` - `Repair()` - удар молотом по баррикаде +20 HP

4. `BannerAgentComponent` - `OnTick` ищет союзников в 15м, дает `DamageBuff 1.1`

5. Выбор класса: `UI/NI_ClassSelect_VM` - 5 кнопок Infantry/Archer/Medic/Engineer/Banner, при спавне

6. Тест: Умри 1 раз - Fallen, медик поднимает. Инженер чинит баррикаду.

### День 6: Кавалерия (7) + Колья + Отряды (11)

1. Добавь в `Characters.xml` `ni_nord_raider_mounted` с лошадью

2. В WaveManager после волны 10: 20% шанс кавалерии

3. `Machines/StakesTrap.cs` - UsableMachine, при `OnAgentHit` если агент на лошади и дистанция <2м -> `Horse.Die()`, `Agent.Die()`

4. `Managers/SquadManager.cs` - `SpawnShieldWallSquad()`:
   - Лидер + 3 huscarl + 3 archer
   - Лидер - `Formation.Captain`, остальные Follow

5. Тест: Волна 10 - 2 кавалериста с фланга, поставь колья - лошадь умирает. Волна 3 - сквад щитоносцев стеной.

### День 7-8: Модульный форт (2) + Разрушаемость (14)

1. Создай `ModuleData/SceneProps/` XML для пропсов:
   - `ni_foundation_wood`, `ni_wall_wood`, `ni_wall_door`, `ni_stakes`, `ni_oil_cauldron`, `ni_brazier`

2. `Machines/BarricadeDestructible.cs` - наследуй от `DestructibleComponent`, 800 HP, `OnHit` -20 HP, при 0 - destroy + спавн 2 wood

3. `Managers/FortressBuildManager.cs`:
   - `TryPlace(BuildType)` - проверка wood/metal из `PersistenceManager.PlayerGoldComponent`
   - Raycast на землю, спавн пропса

4. `Machines/TrapMachines.cs` уже есть - Rock, Log, OilDitch, Drawbridge

5. `Machines/ForgeUsable.cs` - пока заглушка, просто +стрелы

6. Тест: B -> Build Menu, Foundation 5 wood, Wall 3 wood, Stakes убивает лошадь, Oil льет кипяток AOE.

### День 9-10: Огонь, Взрывы, Деревья

1. `Machines/PowderBarrel` - при уроне `AddExplosion()`, урон 80 в радиусе 5м

2. `TreeDestructible` - при уничтожении спавнит `FallenTree` блокирующий проход

3. Torch item - в `ModuleData/Items.xml` добавь `torch`, при ударе по дереву `SetBurning(true)`

4. Тест: Ударь факелом по дереву - загорается, бочка взрывается.

**Критерий фазы 2:** Роли работают, кавалерия контрится кольями, сквады стеной, баррикады ставятся и ломаются, огонь/взрывы.

---

## Фаза 3: Meta - Перки, Цели, Погода, Мутаторы, Директор, Командир, Боссы (7 дней)

### День 11-12: Перки (1) + Мутаторы (10)

1. `Models/PerkDefinition.cs` - 30 перков, 3 ветки

2. `Managers/PerkManager.cs` - создай:
   - `ShowChoiceToAll()` - если wave%3==0, открывает `NI_PerkChoice_VM` Gauntlet UI с 3 карточками
   - `ApplyPerk(player, perkId)` - сохраняет в `PlayerGoldComponent` slots, применяет к агенту

3. `UI/NI_PerkChoice_VM.cs` - 3 кнопки, 15 сек таймер, если не выбрал - рандом

4. `Behaviors/MutatorBehavior.cs` - допиши 12 мутаторов:
   - Berserk: speed 1.5, no block
   - Greedy: gold x2
   - Marked: random player marked, all bots target him
   - BossRush: 3 bosses

5. Тест: Волна 3 - выбор перка Iron Skin +15% HP, волна 4 - мутатор Thor's Fury.

### День 13: Цели (4) + Боссы с фазами (22)

1. `Behaviors/ObjectiveBehavior.cs` - допиши:
   - DestroyRam: спавн `Ram` который едет к воротам, 2000 HP, надо сломать
   - BurnCamps: 3 `Camp` за картой, поджечь факелом
   - Escort: villager идет по точкам

2. `Behaviors/BossPhaseBehavior.cs` уже есть - подключи:
   - Phase 1 100-66% обычный, Phase 2 66-33% призывает 2 миньонов + buff, Phase 3 <33% огонь вокруг + взрыв при смерти

3. Тест: Волна 3 - цель DestroyRam, волна 5 - босс с фазами.

### День 14-15: Погода (6) + Директор (5) + Командир (16) + Мораль (17)

1. `WeatherBehavior.cs` - уже есть, добавь эффекты:
   - Fog: `SetVisibilityMultiplier(0.5f)` для лучников
   - Rain: fire arrows disabled
   - Snow: speed 0.9
   - Night: time 2f, torches

2. `DirectorBehavior.cs` - уже есть, допиши relief: если Stress<20 спавн AmmoBox

3. `CommanderBehavior.cs` - уже есть:
   - Выбор командира (голосование или первый с рангом)
   - R -> топ-даун камера (используй `MissionScreen.SetCameraMode`)
   - Маркеры Attack/Build/Retreat

4. `MoraleBehavior.cs` - уже есть:
   - Squad morale, лидер умер -40, <30 flee
   - Player morale <30 - speed 0.8

5. Тест: Волна 5 - туман, лучники слепые. Убей лидера отряда - отряд бежит. Стань командиром, поставь маркер.

### День 16-17: Осадные орудия (18) + Закалка (19) + Ловушки (23)

1. `Machines/SiegeWeapons.cs` уже есть - Ballista pierce, Catapult AOE, OilPot

2. `Machines/ForgeUsable.cs` уже есть - 4 типа закалки: Sharpened +10% dmg, Hardened +20% durability, Poison tick, Flaming burn

3. `Machines/TrapMachines.cs` уже есть - Rock, Log, OilDitch, Drawbridge

4. Создай сцену с этими машинами: поставь Ballista на стене, Rock над тропой

5. Тест: Сядь в баллисту F, выстрели - пробивает 3. Закали меч у кузницы - +10% урона. Сруби веревку скалы - падает и давит.

**Критерий фазы 3:** Перки, мутаторы, цели, боссы с фазами, погода, директор, командир, мораль, осадные орудия, закалка, ловушки - все работает.

---

## Фаза 4: Persistence, Campaign, Loot, Scavenging, Ranks, Spectator, Elemental, Last Stand, Supply (7 дней)

### День 18-19: Физ-лут (8) + Скавенджинг (9) + Стихийный урон (28)

1. `Managers/LootManager.cs` + `Machines/TreasuryChest`:
   - При смерти босса `SpawnLootBag(pos, 500)` - UsableMachine мешок
   - Игрок F -> `AttachProp`, speed 0.7, донести до TreasuryChest -> +500 gold

2. `Managers/ScavengeManager.cs`:
   - При убийстве ветерана 20% +1 metal
   - При разрушении баррикады +2 wood

3. `Components/ElementalComponent.cs` уже есть:
   - Fire tick 5, Poison -30% speed, Ice 0.5 speed, Lightning chain 3, Bleed stacks 5
   - Комбо: oil+fire=explosion

4. Тест: Убей босса - мешок, донеси до казны. Сломай баррикаду - 2 wood. Ударь огненным мечом - горит.

### День 20-21: Персистенция (12) + Мета-прокачка (24) + Ранги (26)

1. Запусти backend: `cd src/backend && pip install -r requirements.txt && uvicorn main:app --reload`

2. `Managers/PersistenceManager.cs` уже есть:
   - `OnAgentBuild` добавляет `PlayerGoldComponent` с Gold/Wood/Metal
   - `LoginPlayer(steamId, name)` -> POST /api/player/login
   - `OnKill` -> POST /api/kill

3. `Managers/MetaProgressionManager.cs` уже есть:
   - SkillTree 7 нод, Ranks 4 титула
   - `ApplyMetaBonuses()` - blacksmith_1 +1 wood, veteran_1 +5% HP
   - `ApplyCosmetics()` - наплечники, плащ

4. Тест: Зайди в игру, убей 10 нордов, выйди, проверь `GET /api/player/{steamId}` - gold сохранился. Купи чертеж.

### День 22-23: Фаза лагеря (20) + Динамические NPC (21) + Снабжение (30)

1. `Behaviors/CampPhaseBehavior.cs` уже есть:
   - Каждые 5 волн 90 сек, спавн Trader, Smith
   - Dynamic NPC: беженцы, дезертир, скавенджер-торговец, раненый рыцарь

2. `Behaviors/SupplyBehavior.cs` уже есть:
   - Caravan каждые 3 волны: 2 повозки + 4 охранника, 6 рейдеров в засаде
   - Дошел +20 wood +10 metal, разграблен - 3 волны без ресурсов
   - Warehouse Level 2: 100 wood + авто-ремонт

3. Тест: Волна 5 - лагерь 90 сек, торговец продает чертеж. Волна 3 - караван, защити от засады.

### День 24: Спектатор/Ставки (27) + Last Stand (29)

1. `Behaviors/SpectatorBettingBehavior.cs` уже есть:
   - Killcam 3 сек слоу-мо когда босс убил
   - Мертвые ставят золото на выжившего, выиграл +50%
   - Спектатор - нативный Bannerlord Spectator

2. `Behaviors/LastStandBehavior.cs` уже есть:
   - 1 живой + все упали + 1 норд = Last Stand 10 сек слоу-мо, последний +100% урона, упавшие ползут

3. Тест: Умри, поставь ставку, смотри киллкам. Last Stand - 1 vs 1.

**Критерий фазы 4:** Лут, скавенджинг, стихии, персистенция, мета, лагерь, NPC, снабжение, ставки, Last Stand - все работает, backend сохраняет.

---

## Фаза 5: Полировка, Карты, Dedicated Server, Релиз (3 дня)

### День 25: Карты

1. Создай 4 карты в Scene Editor:
   - `mp_ni_town_01` - город, узкие улицы
   - `mp_ni_castle_01` - замок, ворота
   - `mp_ni_bridge_01` - мост, бутылочное горлышко (лучшая для баррикад)
   - `mp_ni_forest_01` - лес, засады

2. Для каждой: entry 0-31 в форте, 32-63 вокруг, 64 босс, пропсы: chest, brazier, ballista, rock, oil ditch

3. Тест: Все карты грузятся, боты доходят до игроков.

### День 26: UI Полировка

1. Допиши Gauntlet UI:
   - `NI_HUD_VM` - волна, норды, живые, мутатор, погода, цель, stress, склад wood/metal
   - `NI_Shop_VM` - золото, wood, metal, кнопки Buy Sword/Bow/Armor/Barricade/Stakes/Oil
   - `NI_BuildMenu_VM` - Foundation/Wall/Door/Stakes/Oil/Brazier
   - `NI_PerkChoice_VM` - 3 карточки с иконками
   - `NI_CampaignMap_VM` - 8 деревень, голосование

2. Добавь иконки, звуки для мутаторов, Last Stand музыки

### День 27: Dedicated Server

1. Скачай Dedicated Server: `steamcmd +login anonymous +app_update 1058080 validate +quit`

2. Скопируй модуль `Modules/NordInvasion` в `.../Dedicated Server/Modules/`

3. Конфиг `DedicatedServer/Bannerlord/DedicatedCustomServerConfig.xml` уже готов

4. Запуск:
   ```
   TaleWorlds.MountAndBlade.DedicatedCustomServer.exe /dedicatedcustomserverconfig DedicatedCustomServerConfig.xml
   ```

5. Тест: 2 клиента подключаются по IP:7240, волны работают.

### День 28: Релиз

1. Собери релиз: `NordInvasion v2.1.0 - 29 mechanics` zip с `Modules/NordInvasion/`

2. Напиши README с инструкцией

3. Выложи на NexusMods / ModDB

**Итого: 28 дней для 1 разработчика.**

## Инструкция по запуску (краткая)

### Для игрока:

1. Установи Bannerlord 1.4.8 (без War Sails DLC)
2. Внешние моды (ButterLib/UIExtenderEx/MCM) не требуются
3. Скачай NordInvasion, закинь в `Modules/`
4. Включи в лаунчере
5. Custom Battle -> mp_ni_bridge_01 -> mp_nord_invasion
6. Или через Bannerlord Co-op мод - Host -> NordInvasion

### Для хоста Co-op (4-8 игроков):

1. Установи Bannerlord Co-op мод
2. Host -> Co-op Campaign -> Запусти Custom Battle mp_ni_bridge_01
3. Друзья Join через Steam

### Для Dedicated Server (32 игрока):

1. `steamcmd +login anonymous +app_update 1058080`
2. Скопируй `Modules/NordInvasion` в `Dedicated Server/Modules/`
3. Запусти `start_bannerlord_server.bat`
4. Игроки: Multiplayer -> Add Server -> IP:7240

### Для backend (персистенция):

```bash
cd src/backend
pip install fastapi uvicorn
uvicorn main:app --host 0.0.0.0 --port 8000
```

Укажи URL в `PersistenceManager.cs` `_backendUrl = "http://yourserver:8000"`
