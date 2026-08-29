# План реализации Nord Invasion Better Edition - 15 улучшений

## Общая архитектура Better Edition

Базовый Fianna модуль -> Better Edition = 5 слоев:

```
Layer 0: Core (волны, боты, золото) - уже есть
Layer 1: Combat (медики, инженеры, формации, кавалерия)
Layer 2: Fortress (модульные баррикады, огонь, разрушаемость)
Layer 3: Meta (перки, мутаторы, директор, погода)
Layer 4: Persistence (SteamID, чертежи, кампания, сезоны)
```

## Фаза 1: Combat & Roles (неделя 1) - Механики 3,7,11,13

### 3. Роли Медик/Инженер/Знаменосец
- Файлы: `module_constants.py` - добавить `slot_player_class`, `class_medic`, `class_engineer`
- `module_scripts.py`:
  - `script_ni_class_medic_heal` - проверка дистанции, `agent_get_animation`, таймер 5 сек, `agent_set_hit_points`
  - `script_ni_class_engineer_repair` - для баррикад `slot_prop_health < max` -> +10 HP за удар молотом
  - `script_ni_banner_buff` - тик каждые 2 сек, всем в радиусе 15м `agent_set_damage_modifier`
- `module_mission_templates.py`: триггер `ti_on_agent_hit` для подсчета падений
- `module_items.py`: добавить `itm_ni_medical_kit`, `itm_ni_repair_hammer`, `itm_ni_banner`
- `module_presentations.py`: UI выбора класса при спавне

### 7. Кавалерия нордов
- `module_troops.py`: добавить `trp_ni_nord_raider_mounted`, `trp_ni_nord_horse_archer`
- `module_scripts.py`: в `script_nord_invasion_setup_wave` если wave >=10, 20% ботов - кавалерия
- `module_scene_props.py`: `spr_ni_stakes` - при контакте лошади `ti_on_scene_prop_hit` -> лошадь падает
- Баланс: кавалерия спавнится с флангов (entry 56-63), цель - лучники

### 11. Отряды с формациями
- `module_scripts.py`:
  - `script_ni_spawn_squad_shieldwall`: спавн 1 лидера + 4 щитоносца + 3 копейщика, лидеру `agent_set_slot squad_id`, остальным `agent_set_attached`
  - Squad AI: лидеру `ai_set_behaviour = hold_position`, остальным `follow_leader`
- Требует WSE: `wse_agent_set_formation` или костыль через `agent_set_scripted_destination`
- Состав сквадов в `module_constants.py`: `ni_squad_types`

### 13. Ранения и усталость
- `module_constants.py`: `slot_agent_wounds = 210`, `slot_agent_stamina = 211`
- `module_scripts.py`:
  - `script_ni_on_hit`: каждый удар -5 стамины, если <20% -> урон -50%
  - `script_ni_wound_system`: 0 HP -> не смерть, а `fallen` (лежит, можно поднять медиком). 3 падения = смерть.
- `module_mission_templates.py`: `ti_on_agent_hit` -> уменьшение стамины, `ti_on_agent_killed` -> проверка ранений

## Фаза 2: Fortress (неделя 2) - Механики 2,8,9,14

### 2. Модульный форт-конструктор
- `module_scene_props.py`: 10+ пропсов:
  - `spr_ni_foundation_wood`, `spr_ni_wall_wood`, `spr_ni_wall_door`, `spr_ni_wall_window`, `spr_ni_stakes`, `spr_ni_oil_cauldron`, `spr_ni_brazier`, `spr_ni_spike_trap`
- `module_scripts.py`:
  - `script_ni_place_foundation`: проверка земли, нельзя в воздухе
  - `script_ni_upgrade_wall`: из фундамента -> стена за ресурсы
  - `script_ni_oil_cauldron_use`: льет кипяток, урон по площади
- `module_presentations.py`: меню строительства `prsnt_ni_build_menu` - сетка 3x3 выбора
- Ресурсы: `slot_player_wood`, `slot_player_metal` (из скавенджинга)

### 8. Физический лут с босса
- `module_scene_props.py`: `spr_ni_loot_bag_gold`, `spr_ni_loot_blueprint`
- `module_scripts.py`: `script_ni_spawn_boss_loot` - при смерти босса `spawn_scene_prop` на его позиции, `prop_set_slot gold_value = 500`
- `ti_on_scene_prop_use`: подобрать мешок -> `agent_attach`, скорость -30%, надо донести до `spr_ni_treasury_chest`
- Если норд-бот подходит к мешку - крадет (деспавн + золото нордам)

### 9. Скавенджинг
- `module_scripts.py`:
  - `script_ni_scavenge_barricade`: сломанная баррикада -> 2 доски
  - `script_ni_scavenge_corpse`: труп норда-ветерана -> шанс 20% `itm_ni_scrap_metal`
- `module_items.py`: `itm_ni_wood_plank`, `itm_ni_scrap_metal`, `itm_ni_cloth`
- У костра `spr_ni_campfire` можно крафтить: 3 доски + 1 металл = стрелы

### 14. Разрушаемость и огонь
- `module_scene_props.py`: все деревянные пропсы `sokf_destructible` + `ti_on_scene_prop_hit` с проверкой `item = torch`
- `script_ni_ignite_prop`: если ударили факелом, `prop_set_slot is_burning = 1`, запускаешь `particle_system_add` огонь, тик урона
- Дерево `spr_ni_tree_oak` - при уничтожении спавнит `spr_ni_fallen_tree` который блокирует проход
- Бочка `spr_ni_powder_barrel` - при уроне взрывается `particle_system_burst`, урон по площади

## Фаза 3: Meta & Roguelite (неделя 3) - Механики 1,4,6,10,5

### 1. Roguelite перки
- `module_constants.py`: 30 перков, 3 ветки по 10
- `module_presentations.py`: `prsnt_ni_perk_choice` - 3 карточки с иконками, описанием
- `module_scripts.py`:
  - `script_ni_apply_perk`: в зависимости от perk_id меняет `agent slots`: `slot_agent_perk_damage`, `slot_agent_perk_hp`, etc.
  - При спавне агента применяешь все перки игрока
- Триггер: `wave % 3 == 0` -> показать презентацию

### 4. Цели волн
- `module_constants.py`: `ni_wave_objective_types = [kill_all, destroy_ram, escort, burn_camps, defend_treasury]`
- `module_scripts.py`:
  - `script_ni_setup_objective`: в зависимости от типа спавнишь `spr_ni_ram`, `spr_ni_villager`, etc.
  - `script_ni_check_objective`: тик проверки
- `module_mission_templates.py`: дополнительные entry points для эскорта

### 6. Погода и время
- `module_scripts.py`:
  - `script_ni_set_weather`: `set_shader_param`, `set_fog_distance`, `set_skybox`
  - `script_ni_weather_effects`: туман -> `agent_set_visibility`, дождь -> проверка `item = flaming_arrow` -> тухнет
- `module_mission_templates.py`: триггер каждые 5 волн меняет погоду, `store_random`
- Визуал: `particle_systems` для дождя/снега (уже есть в Native)

### 10. Мутаторы Богов
- `module_constants.py`: 12 мутаторов
- `module_scripts.py`:
  - `script_ni_apply_mutator`: глобальные модификаторы, например ` $g_ni_mutator = mutator_berserk` -> всем ботам `agent_set_speed_modifier 150`
  - Применяются в `setup_wave`
- `module_presentations.py`: HUD иконка мутатора с описанием

### 5. AI-Директор
- `module_scripts.py`:
  - `script_ni_director_calculate_stress`: считает `team_kd = kills/deaths`, `avg_gold`, `alive_ratio`
  - Если `stress < 0.3` (команда вайпается) -> `director_give_relief`: спавн ящика с патронами, респавн 1 игрока, туман
  - Если `stress > 0.8` (команда тащит) -> `director_increase_pressure`: +10% ботов, фланги, 2 босса
- Хранится в `$g_ni_director_stress` (0-100)

## Фаза 4: Persistence & Campaign (неделя 4) - Механики 12,15

### 12. Персистенция 2.0
- `src/backend/main.py`: расширить:
  - Таблицы: `players(id, steam_id, gold, level, xp, blueprints JSON, season_points)`
  - `seasons(id, name, start, end, rewards JSON)`
  - `battlepass(player_id, level, rewards_claimed)`
- `module_scripts.py`:
  - `script_ni_backend_login`: WSE `wse_http_post` SteamID -> получить данные
  - `script_ni_backend_save`: при выходе / конце волны сохранить
- `module_presentations.py`: `prsnt_ni_character_sheet` - уровень, чертежи, скины

### 15. Глобальная кампания
- `src/backend/main.py`: таблица `villages(id, name, owner (swadia/nords), defense, x, y)`
- `module_game_menus.py`: новое меню `mnu_ni_campaign_map` - карта Свадии, точки деревень, выбор куда идти
- `module_scripts.py`: `script_ni_campaign_vote` - игроки голосуют, большинство выбирает
- После победы/поражения: `POST /api/campaign/village/{id}/battle_result`

## Технические требования

- Warband 1.174 + WSE 4.8.0+
- Module System Extended (Python 3) рекомендуется
- Backend: Python 3.11 + FastAPI + SQLite/Postgres
- Для #14 разрушаемость: нужны `bo_` коллизии для всех новых пропсов
- Для #5 директора: нужен сбор статистики в реальном времени

## Порядок файлов для компиляции

1. module_constants.py (все константы)
2. module_troops.py (новые войска)
3. module_items.py (новые предметы)
4. module_scene_props.py (новые пропсы)
5. module_scripts.py (вся логика)
6. module_mission_templates.py (триггеры)
7. module_presentations.py (UI)
8. module_game_menus.py (кампания)

## Тестирование каждой механики

- Для каждой механики: отдельный тестовый сервер с 1 картой, 1 волной
- Команда `/cheat` для админа: `/spawn boss`, `/set wave 10`, `/give gold 1000`, `/set weather fog`
- Логирование в `server_log.txt` + `WSE/logs/`

## Dedicated Server Files

- Нужны `mb_warband_dedicated.exe` 1.174 + `WSELoaderServer.exe`
- В репо должны быть в `DedicatedServer/` папке
- Если нет - пользователь загружает отдельно (инструкция в docs/QUICKSTART.md)

## Оценка времени

- Фаза 1: 7 дней
- Фаза 2: 7 дней
- Фаза 3: 7 дней
- Фаза 4: 7 дней
- Полировка: 3 дня
- Итого: ~31 день для 1 разработчика
