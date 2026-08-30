# Реализовано 29 из 30 механик (без питомцев)

По запросу: все кроме питомцев (25).

## Базовые 15 (из Better Edition)

| # | Механика | Файл | Статус |
|---|----------|------|--------|
| 1 | Roguelite перки | `Managers/PerkManager`, `Components/PerkAgentComponent`, `Behaviors/WaveManager` | ✅ |
| 2 | Модульный форт | `Managers/FortressBuildManager`, `Machines/BarricadeDestructible` | ✅ |
| 3 | Роли Медик/Инженер/Знаменосец | `Components/WoundStaminaComponent`, `Managers/FortressBuildManager` | ✅ |
| 4 | Цели волн | `Behaviors/ObjectiveBehavior`, `Machines/Ram` | ✅ |
| 5 | AI-Директор | `Behaviors/DirectorBehavior` | ✅ |
| 6 | Погода/время | `Behaviors/WeatherBehavior` | ✅ |
| 7 | Кавалерия нордов | `Models/WaveDefinition`, `Machines/StakesTrap` | ✅ |
| 8 | Физ-лут | `Managers/LootManager`, `Machines/TreasuryChest` | ✅ |
| 9 | Скавенджинг | `Managers/ScavengeManager` | ✅ |
| 10 | Мутаторы богов | `Behaviors/MutatorBehavior`, `Models/MutatorType` | ✅ |
| 11 | Отряды с формациями | `Managers/SquadManager` | ✅ |
| 12 | Персистенция 2.0 | `Managers/PersistenceManager`, `src/backend/main.py` | ✅ |
| 13 | Ранения/стамина | `Components/WoundStaminaComponent` | ✅ |
| 14 | Разрушаемость/огонь | `Machines/BarricadeDestructible`, `Machines/TrapMachines` | ✅ |
| 15 | Глобальная кампания | `Behaviors/CampaignBehavior`, `src/backend` villages | ✅ |

## Extra 14 (без питомцев)

| # | Механика | Файл | Статус |
|---|----------|------|--------|
| 16 | Командир и приказы | `Behaviors/CommanderBehavior.cs` | ✅ |
| 17 | Мораль и паника | `Behaviors/MoraleBehavior.cs` | ✅ |
| 18 | Осадные орудия | `Machines/SiegeWeapons.cs` - Ballista, Catapult, OilPot | ✅ |
| 19 | Закалка оружия | `Machines/ForgeUsable.cs` | ✅ |
| 20 | Фаза лагеря | `Behaviors/CampPhaseBehavior.cs` | ✅ |
| 21 | Динамические NPC | `Behaviors/CampPhaseBehavior.cs` - беженцы, дезертир, торговец, рыцарь | ✅ |
| 22 | Боссы с фазами | `Behaviors/BossPhaseBehavior.cs` | ✅ |
| 23 | Ловушки окружения | `Machines/TrapMachines.cs` - Rock, Log, OilDitch, Drawbridge | ✅ |
| 24 | Мета-прокачка | `Managers/MetaProgressionManager.cs` - SkillTree | ✅ |
| 26 | Ранги и косметика | `Managers/MetaProgressionManager.cs` - Ranks | ✅ |
| 27 | Спектатор/киллкам/ставки | `Behaviors/SpectatorBettingBehavior.cs` | ✅ |
| 28 | Стихийный урон | `Components/ElementalComponent.cs` - Fire, Poison, Ice, Lightning, Bleed + combos | ✅ |
| 29 | Last Stand | `Behaviors/LastStandBehavior.cs` | ✅ |
| 30 | Снабжение | `Behaviors/SupplyBehavior.cs` - Caravan, Warehouse | ✅ |

## Исключено

| # | Механика | Причина |
|---|----------|---------|
| 25 | Питомцы - боевые псы, вороны | По запросу пользователя исключено |

## Итого

- 29 механик реализованы в C# скелете
- Все без BLSE, кроме 16 и 27 где BLSE даст плюсы
- Готов к компиляции в Bannerlord 1.4.8 (без War Sails DLC)
- Backend поддерживает все: gold, wood, metal, blueprints, seasons, villages, battlepass

## Как тестировать без питомцев

- Командир: R -> топ-даун, ставишь маркеры
- Мораль: убей лидера отряда - отряд бежит
- Баллиста: построй за 10 wood, сядь F, стреляй
- Закалка: у кузницы F с 3 metal -> +10% урон
- Лагерь: каждые 5 волн 90 сек, торговец, кузнец
- Босс фазы: бей босса до 66% и 33% - смотри фазы
- Ловушки: F на скале над тропой -> падает
- Мета: Season Points тратятся в древе
- Ранги: титул дает наплечники/плащ
- Ставки: умри -> ставь на выжившего
- Стихии: масло + огонь = взрыв
- Last Stand: 1 живой + все упали + 1 норд = слоу-мо 10 сек
- Снабжение: защити караван 2 повозки, +20 wood

Все механики кроме питомцев в коде.
