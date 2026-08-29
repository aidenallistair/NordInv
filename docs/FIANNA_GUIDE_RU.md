# Как поднять сервер Nord Invasion как на Fianna.ru

## 1. Архитектура мода Nord Invasion

### Что происходит в матче:

```
Lobby -> Игроки спавнятся (team 0, defenders) -> Сервер спавнит волну 1 (team 1, attackers, боты)
-> Игроки убивают ботов -> Проверка: все боты мертвы?
  -> Да: пауза 8 сек, выдача золота, wave_number++, спавн следующей волны
  -> Нет: ждем
-> Если все игроки мертвы до конца волны -> Конец миссии, поражение
-> Каждые 4 волны: респавн всех мертвых
```

### Ключевые отличия от Native Warband:

1.  **Боты в мультиплеере**: Native не умеет спавнить ботов в мультиплеере нормально. Нужен WSE (Warband Script Enhancer) и трюк с `add_visitors_to_current_scene` + `set_visitor` или `spawn_agent`.
2.  **Экономика**: В Native деньги - только сингл. В мультиплеере используем `player slots` и `agent slots` для хранения золота.
3.  **Магазин**: Через `scene props` (сундуки/оружейные стойки) + `presentation` (меню покупки по F).
4.  **Баррикады**: Разрушаемые `scene props` которые игроки могут ставить за золото. `spawn_scene_prop`.

### Стек технологий Fianna:

- Warband 1.168 (у Fianna) / 1.174 (сейчас актуальная)
- Module System 1.174
- WSE 4.8.0+ (обязательно!) - дает `wse_*` операции, http запросы, файловые операции, расширенный ИИ ботов
- mb_warband_dedicated.exe - выделенный сервер
- Их собственный мастер-сервер (патч exe чтобы стучался на fianna.ru вместо taleworlds). Сейчас не нужно - можно играть по IP или через официальный мастер (еще работает) или через community master.

---

## 2. Структура Module System

Берем чистый Module System 1.174 отсюда: https://forums.taleworlds.com/index.php?threads/module-system-1-174.400830/

Копируем наши файлы поверх:

### 2.1 `module_constants.py` - добавляем константы

```python
# Game types - добавляем свой
multiplayer_game_type_nord_invasion = 10

# Команды
ni_team_defenders = 0
ni_team_nords = 1

# Состояния волны
ni_wave_state_waiting = 0
ni_wave_state_spawning = 1
ni_wave_state_fighting = 2
ni_wave_state_completed = 3

# Слоты
slot_player_gold = 100  # золото игрока
slot_player_wave_kills = 101
slot_player_is_dead = 102

slot_agent_is_nord_bot = 200

# Награды
ni_gold_per_kill_peasant = 3
ni_gold_per_kill_warrior = 8
ni_gold_per_kill_boss = 50

# Респавн
ni_respawn_wave_interval = 4  # каждые 4 волны
```

### 2.2 `module_troops.py` - норды

В оригинальном NI было ~50 типов нордов. Для Fianna-версии достаточно 12-15:

- nord_peasant, nord_footman, nord_archer, nord_huscarl, nord_berserker, nord_jarls_guard, nord_boss_*

Каждый с `tf_guarantee_all` экипировкой, `tf_is_mounted` если надо.

### 2.3 Логика волн - сердце мода

Волна = список (troop_id, количество). Количество масштабируется от числа игроков.

Пример таблицы:

| Волна | Состав | Всего ботов |
|-------|--------|-------------|
| 1 | 10x peasant | 10 |
| 2 | 15x peasant, 3x footman | 18 |
| 3 | 10x footman, 5x archer | 15 |
| 4 | RESPAWN + 20x mixed | 20 |
| 8 | 5x huscarl + boss | 6 элиты |
| 12 | 20x huscarl + 2 boss | hard |

В Fianna было до 20-25 волн, потом цикл или победа.

### 2.4 `module_mission_templates.py` - главный файл

Смотри `ModuleSystem/module_mission_templates.py` в этом репо - там готовый шаблон `mp_nord_invasion`.

Основные триггеры:

1.  `ti_before_mission_start` - инициализация: `call_script, script_nord_invasion_init`
2.  `ti_on_agent_spawn` - если агент бот, дать ему тактику (charge)
3.  `ti_on_agent_killed_or_wounded` - главный:
    - Если убит норд-бот: дать золото убийце, уменьшить счетчик живых ботов
    - Если убит игрок: пометить как мертвого, проверить остались ли живые игроки
    - Если счетчик ботов == 0 -> `call_script, script_nord_invasion_wave_completed`
4.  `ti_server_player_joined` - выдать начальное золото, снаряжение
5.  Повторяющийся триггер каждые 1 сек: проверка состояния волны, спавн, таймеры, показ UI (wave number)

### 2.5 `module_scripts.py` - скрипты

Смотри `ModuleSystem/module_scripts.py`:

- `script_nord_invasion_init`
- `script_nord_invasion_setup_wave`
- `script_nord_invasion_spawn_bots`
- `script_nord_invasion_wave_completed`
- `script_nord_invasion_reward_player`
- `script_nord_invasion_check_defeat`

---

## 3. Поднятие выделенного сервера

### Windows (классика Fianna):

1.  Скачай Dedicated Server Files: https://www.taleworlds.com/en/Games/Warband/Download (Other Downloads -> Dedicated Server)
2.  Распакуй в `C:\mb_warband_dedicated\`
3.  Скачай WSE: https://forums.taleworlds.com/index.php?threads/warband-script-enhancer-2-wse2-v1-1-0-7.324870/
    - Распакуй, создай папку `WSE` внутри папки сервера
    - Скопируй `WSELoaderServer.exe` и `*.dll` в `WSE/`
    - Замени `mb_warband_dedicated.exe` на WSE-совместимый из архива WSE
4.  Скомпилируй модуль: в ModuleSystem запусти `build_module.bat`
5.  Скопируй папку `Modules/Fianna_NordInvasion` в `C:\mb_warband_dedicated\Modules\`
6.  Создай конфиг `nordinvasion.cfg` (пример в `ServerConfig/`)

7.  Запуск:

```bat
WSE\WSELoaderServer.exe -r nordinvasion.cfg -m Fianna_NordInvasion -p mb_warband_dedicated.exe
```

Или через bat:

```bat
@echo off
:loop
WSE\WSELoaderServer.exe -r nordinvasion.cfg -m Fianna_NordInvasion -p mb_warband_dedicated.exe
echo Server crashed, restarting in 5 sec...
timeout 5
goto loop
```

### Linux (современный способ):

Через Wine + screen:

```bash
sudo apt install wine64 screen
unzip mb_warband_dedicated_*.zip -d ~/warband_server
cd ~/warband_server

# WSE setup
mkdir WSE
unzip wse_*.zip -d WSE
cp WSE/mb_warband_dedicated.exe ./

# Модуль
cp -r ~/NordInv/Build/Fianna_NordInvasion Modules/

screen -S warband
wineconsole --backend=curses mb_warband_dedicated.exe -r nordinvasion.cfg -m Fianna_NordInvasion
# detach: Ctrl+A D
```

### Docker (рекомендуется сейчас):

Смотри `ServerConfig/docker-compose.yml` и `Dockerfile` в этом репо.

```bash
docker-compose up -d
```

### Конфиг сервера `nordinvasion.cfg`:

```
set_pass_admin YOUR_ADMIN_PASS
set_server_name Fianna_NordInvasion_RU #1
set_welcome_message Добро пожаловать! Nord Invasion | Волна: текущая
set_max_players 32 32
set_port 7240
set_add_to_game_servers_list 1
set_upload_limit 1000000
set_mission mp_nord_town_01 mp_nord_village_01 mp_nord_castle_01
set_map mp_nord_town_01
set_game_type 10
set_friendly_fire 0
set_melee_friendly_fire 0
set_friendly_fire_damage_self_ratio 0
set_friendly_fire_damage_friend_ratio 0
set_allow_player_banners 1
set_ghost_mode 0
set_control_block_dir 0
set_combat_speed 2
set_round_max_seconds 3600
set_respawn_period 100000 # отключаем стандартный респавн, управляем сами
```

---

## 4. Магазин и баррикады

### Магазин:

Самый простой вариант как на Fianna:

- В `module_scene_props.py` добавляем `spr_ni_armory_chest`
- В `module_mission_templates.py` триггер `ti_on_scene_prop_use`:
  - Если игрок нажал F на сундук -> открыть `prsnt_ni_shop`
- В `module_presentations.py` - презенташка с кнопками купить оружие/броню
- Цена проверяется через `player_get_gold`, списание через `player_set_gold`

Продвинутый вариант: как в оригинальном NI - веб-магазин + инвентарь. Но для Fianna-стиля достаточно внутриигрового.

### Баррикады:

```
spr_ni_barricade_wood - разрушаемый, 500 HP
spr_ni_barricade_shieldwall
```

Игрок покупает баррикаду в магазине -> `spawn_scene_prop` перед ним -> снимает золото.

Скрипт:

```python
(script_ni_place_barricade,
  [
    (store_script_param, ":player_no", 1),
    (player_get_agent_id, ":agent_id", ":player_no"),
    (agent_get_position, pos1, ":agent_id"),
    (position_move_forward, pos1, 150),
    (set_spawn_position, pos1),
    (spawn_scene_prop, "spr_ni_barricade_wood"),
  ])
```

---

## 5. Персистенция (как на официальном NI)

Официальный NI имел сайт nordinvasion.com с базой персонажей.

Как реализовать:

1.  **Простой (Fianna)**: Храним в `player slots`, сбрасываем при выходе. Золото внутри сессии. Для сохранения между картами - используем `WSE file operations` чтобы писать в `saves/players/<unique_id>.txt`

2.  **Продвинутый (Оригинал)**:
    - Сервер при ивентах (убийство, конец волны) делает HTTP запрос через `wse_http_get` / `wse_http_post` на ваш бекенд: `https://yourapi.com/api/kill?player_id=...&gold=...`
    - Бекенд: PHP/Python + MySQL: таблицы `players`, `characters`, `inventory`, `bank`
    - При заходе игрока сервер запрашивает его персонажа с API и выдает предметы

Fianna использовали второй вариант, но упрощенный - у них была система регистрации ключа `Fianna MB Key` и свой лаунчер.

Для современного сервера можно сделать:

- FastAPI бекенд (пример в `src/backend/`)
- MySQL/Postgres
- WSE скрипт который шлет запросы

---

## 6. Карты

Нужны карты с правильными entry points:

- 0-31: `mtef_defenders` - спавн игроков, зона обороны
- 32-63: `mtef_attackers` - спавн нордов, вокруг карты, 3-4 направления
- 64: центр, для боссов

Используй Native карты `mp_town`, `mp_village` и добавь свои через Thorgrim's Map Editor (входит в Module System).

Для NI лучше узкие карты с бутылочным горлышком - чтобы игроки могли ставить баррикады.

---

## 7. Чек-лист запуска

- [ ] Module System скомпилирован без ошибок
- [ ] WSE установлен и сервер запускается с ним (в консоли должно писать `WSE v4.x loaded`)
- [ ] В логах сервера `Loading module... Fianna_NordInvasion`
- [ ] Клиент с таким же модулем может подключиться по `IP:PORT` (Add server to favorites)
- [ ] Первая волна спавнится
- [ ] Боты бегут к игрокам и атакуют
- [ ] После убийства всех - следующая волна
- [ ] Магазин работает
- [ ] Респавн каждые 4 волны работает
- [ ] Сервер виден в списке (если `set_add_to_game_servers_list 1` и порт 7240 UDP открыт)

### Порты:

- 7240 UDP - основной
- 7241 UDP - для запросов списка серверов (порт+1)
- Открыть в файрволе и на роутере (port forwarding)

---

## 8. Отличия Fianna_NordInvasion от оригинала

| Фича | Оригинал nordinvasion.com | Fianna |
|------|---------------------------|--------|
| Персистенция | Веб-сайт, MySQL, 100+ предметов | Локальная, внутри сессии, ~50 предметов |
| Крафт | Есть, 3 профессии | Нет или упрощен |
| Классы | Дерево классов | Обычные войска Swadia |
| Дома (кланы) | Есть | Нет |
| Карты | 30+ | 8-10 переделанных Native |
| Мастер-сервер | Официальный TaleWorlds | Свой fianna.ru |

Если хочешь 1-в-1 Fianna - делай упрощенную версию без сайта. Это то, что в этом репо.

Если хочешь как оригинал - нужно писать бекенд (пример в `src/backend/`).

---

## 9. Где брать исходники?

- Оригинальный NI не open source, но есть утечка NI 0.3.0 и реверс-инжиниринг на GitHub (поищи `NordInvasion source`)
- Fianna моды тоже закрыты, но их можно декомпилировать из `txt` файлов обратно в `py` через `decompiler` (есть на forums.taleworlds.com)
- Лучший путь: писать с нуля по этому гайду, используя Native как базу - как сделано в этом репо.

Удачи! Если поднимешь - скинь IP, зайдем поиграть.
