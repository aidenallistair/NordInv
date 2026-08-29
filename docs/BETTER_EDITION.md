# Nord Invasion Better Edition - Реализация 15 механик

Это полная реализация всех 15 улучшений поверх Fianna.

## Список механик и статус

| # | Механика | Файлы | Статус |
|---|----------|-------|--------|
| 1 | Roguelite перки | `module_presentations.py: ni_perk_choice`, `module_scripts.py: ni_apply_perk` | ✅ |
| 2 | Модульный форт | `module_scene_props.py: foundation, wall, door, oil`, `module_scripts.py: ni_place_foundation` | ✅ |
| 3 | Роли Медик/Инженер/Знаменосец | `module_scripts.py: ni_class_medic_heal, engineer_repair, banner_buff`, `module_items.py: medical_kit` | ✅ |
| 4 | Цели волн | `module_constants.py: objective types`, `module_scripts.py: setup_objective`, `scene_props: ram, camp` | ✅ |
| 5 | AI-Директор | `module_scripts.py: ni_director_tick, director_stress` | ✅ |
| 6 | Погода и время | `module_scripts.py: ni_set_weather`, `set_fog_distance` | ✅ |
| 7 | Кавалерия нордов | `module_troops.py: raider_mounted`, `scene_props: stakes` | ✅ |
| 8 | Физический лут | `scene_props: loot_bag_gold, treasury_chest`, `scripts: spawn_boss_loot` | ✅ |
| 9 | Скавенджинг | `module_items.py: wood_plank, scrap`, `scripts: scavenge` | ✅ |
| 10 | Мутаторы Богов | `module_constants.py: 12 mutators`, `scripts: apply_mutator` | ✅ |
| 11 | Отряды с формациями | `module_troops.py: shield_leader`, `scripts: ni_spawn_squad` | ✅ |
| 12 | Персистенция 2.0 | `src/backend/main.py` - SteamID, blueprints, seasons, battlepass | ✅ |
| 13 | Ранения и усталость | `scripts: ni_wound_system_on_hit`, `slots: wounds, stamina` | ✅ |
| 14 | Разрушаемость и огонь | `scene_props: tree_oak, powder_barrel, campfire`, `ti_on_scene_prop_hit` с torch | ✅ |
| 15 | Глобальная кампания | `backend: villages`, `presentations: ni_campaign_map`, `scripts: campaign_vote` | ✅ |

## Как тестировать каждую механику

### 1. Перки
- `/set wave 2` -> `/set wave 3` -> должна открыться `prsnt_ni_perk_choice`
- Выбери Iron Skin -> проверь `agent_get_max_hit_points` стал 115

### 2. Форт
- Купи в магазине Barricade Kit (нужно 5 wood). Wood получаешь за скавенджинг.
- Нажми F -> ставится foundation. Еще раз F на foundation -> апгрейд в wall.
- Проверь что `stakes` убивает лошадь.

### 3. Роли
- Выбери класс Medic в `prsnt_ni_class_select` (открывается при спавне)
- Подойди к fallen игроку (лежит, не мертв) -> удержи F 5 сек -> revive
- Инженер: ударь молотом по баррикаде с < max HP -> +20 HP

### 4. Цели
- Волна 3,6,9... - `g_ni_wave_objective` !=0
- Если 1 (ram) - найди таран (entry 64), сломай 2000 HP
- Если 3 (burn camps) - найди 3 `spr_ni_camp_nord` и подожги факелом

### 5. Директор
- Умри 3 раза подряд -> `director_stress` растет, в логах "Pressure increased!"
- Убей 20 ботов без смертей -> stress падает, спавнится ящик с патронами

### 6. Погода
- `/set weather 1` -> туман, `fog_distance 30`
- Лучники должны промахиваться, боты не видеть дальше 20м (проверь AI)

### 7. Кавалерия
- Волна 10+ -> 20% кавалерии
- Поставь stakes перед фортом -> лошадь должна умереть при контакте

### 8. Лут
- Убей босса (волна 5) -> на его месте `spr_ni_loot_bag_gold`
- Подними F -> скорость 70%, донеси до `spr_ni_treasury_chest` -> +500 золота

### 9. Скавенджинг
- Сломай баррикаду -> должны выпасть 2 доски (пока костыль через `player_get_slot wood +=2`)
- Убей ветерана -> 20% шанс +1 metal

### 10. Мутаторы
- Волна 4,8,12... -> рандомный мутатор
- Проверь HUD: `Mutator: Thor's Fury`
- Thor: все боты берсерки, скорость 150%
- Greedy: золото x2

### 11. Отряды
- Волна 3,6... -> спавнятся сквады shieldwall: 1 лидер с баннером + 3 huscarl + 3 archer
- Лидер держит позицию, остальные follow

### 12. Персистенция
- Запусти backend: `cd src/backend && uvicorn main:app --reload`
- В игре WSE должен вызвать `POST /api/player/login` с SteamID
- Проверь `GET /api/player/{steam_id}` -> gold сохраняется
- `POST /api/blueprint/unlock` -> чертеж

### 13. Ранения
- Получи 3 удара двуручем -> стамина <20 -> урон -50%
- Умри 1 раз -> fallen, не dead. Медик может поднять. 3 падения -> смерть.

### 14. Огонь
- Возьми torch, ударь по `spr_ni_tree_oak` -> загорается, particle fire
- Ударь по `spr_ni_powder_barrel` -> взрыв, урон в радиусе 500

### 15. Кампания
- Открой `prsnt_ni_campaign_map` (команда `/campaign`)
- Выбери деревню -> голос
- После победы `POST /api/campaign/battle` -> деревня становится swadia

## Баланс Better Edition

- Стартовое золото 500, но wood/metal отдельно
- Перки стакаются, но максимум 8 за забег (25 волн /3 = 8)
- Баррикады: foundation 5 wood, wall 3 wood, door 5 wood+2 metal, oil 10+5
- Скавенджинг: 20% шанс metal, 100% wood от сломанной баррикады
- Мутаторы: Greedy x2 gold, но риск потерять gold при ударе (Loki)
- Директор: stress 0-100, влияет на кол-во ботов ±20%
- Кавалерия: только после волны 10, 20% от волны
- Лут: 500 золота за мешок, но надо донести (риск)

## Что дальше

- Добавить звуки для мутаторов
- Добавить иконки перков (mesh)
- Добавить анимации для medic revive
- Сделать админ-панель для кампании (веб)
