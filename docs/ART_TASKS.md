# Арт-задачи Nord Invasion (бинарные ассеты)

Все механики работают БЕЗ этих ассетов (в коде есть vanilla-fallback).
Этот список — что нужно создать, чтобы мод выглядел как заявлено.

## 1. Иконки перков (пункт плана "Иконки перков - mesh + material")

- UI готов: `NI_PerkChoice.xml` имеет ImageWidget-слоты, `NI_PerkChoice_VM`
  ждёт путь через `IconPathPrefix`.
- Сделай: 13 иконок 128x128 (dds) в `Modules/NordInvasion/Textures/PerkIcons/`:
  - Survivor: `iron_skin`, `regen`, `second_wind`, `tough`
  - Berserk: `bloodlust`, `vampirism`, `frenzy`, `executioner`
  - Tactician: `engineer`, `gold`, `banner`, `scavenger`
- После импорта: в `NI_PerkChoice_VM` поставь
  `IconPathPrefix = "Modules/NordInvasion/Textures/PerkIcons/"`.

## 2. Мешы сцены (пункт плана "SceneObj")

`SceneProps.xml` описывает `ni_*` пропсы. Пока меши не импортированы,
`PropSpawner` использует vanilla-fallback (см. константы `PropSpawner.Fallback*`).
Мешы нужны в `Modules/NordInvasion/Meshes/`, материалы в `Materials/`:

| Пропс | Фоллбек (пока) | Назначение |
|---|---|---|
| ni_foundation_wood | bd_wood_heap_a | фундамент форта |
| ni_wall_wood / ni_wall_door | empire_garden_wall_a1 | стены/ворота |
| ni_stakes | fence_empire_a | колья против кавалерии |
| ni_oil_cauldron | bd_barrel_a | масляный котёл |
| ni_brazier | torch_a_wm | жаровня (ночь) |
| ni_shield_wall | empire_garden_wall_a1 | щитовая стена |
| ni_loot_bag_gold | vlandia_chest_c | мешок золота босса |
| ni_treasury_chest | vlandia_chest_c | казна |
| ni_ram | empire_garden_wall_a1 | таран (2000 HP) |
| ni_camp_nord | village_tent_e | лагерь нордов (3 шт. на цель) |
| ni_spike_trap | fence_empire_a | ловушка-колья |
| ni_rock_trap / ni_log_trap / ni_oil_ditch | bd_barrel_a / bd_wood_heap_a | ловушки (session 4: ставятся, фоллбек есть) |
| ni_ballista / ni_catapult | bd_wood_heap_a | осадные (session 4: команды BuildBallista/BuildCatapult + фоллбек) |
| ni_armory_chest | vlandia_chest_c | ящик оружейной: F = аптечка/снаряды/ремонт (`NI_ArmoryUsable`) |

Порядок: 1) импортируй меши через Modding Kit, 2) поправь `SceneProps.xml`
(добавь `material=`), 3) убери `SpawnWithFallback` -> `Spawn` для нужного пропса.

Все 13 типов построек и ящик оружейной уже спавнятся с фоллбеком, поэтому арт — это
«стало красиво», а не «иначе не работает». Тотемы выбора перка временно используют
`ni_brazier` (жаровня): своего меша `ni_perk_totem` в `SceneProps.xml` ещё нет.

## 3. Звуки (пункт плана "Звуки для мутаторов, Last Stand музыка")

См. `Modules/NordInvasion/ModuleData/Sounds/README.md`:
- таблица vanilla event-ID в `Audio/NISound.cs` (проверить по rgl_log.txt),
- свои .ogg через FMOD Studio + BLSE.

## 4. Косметика рангов (Mechanic 26)

`Items.xml` уже описывает: `pauldron_wall`, `cloak_medic_white`,
`sword_blood_jarl`, `helmet_engineer` — нужны меши + материалы,
иначе ранги дают только титул в сообщении.
