# Nord Invasion: Бэкенд персистентности (PHP + MySQL + Dedicated Server)

Сохранение вещей и прокачки между забегами: золото/дерево/металл, уровень/XP,
очки сезона, перки, чертежи, мета-дерево навыков, титулы (ранги), победы/лучшая волна,
кампания (деревни, голоса), сезоны, battlepass.

**Стек:** PHP 7.4+ (PDO) → MySQL 5.7+/8.x. Бэкенд крутится **на том же хосте**,
что и Bannerlord Dedicated Server; мод (C#, server-side) общается с ним по HTTP
(form-encoded запросы → JSON ответы).

## Архитектура

```
┌───────────────────────────── хост выделенного сервера ─────────────────────────────┐
│                                                                                    │
│  Bannerlord Dedicated Server (64-bit)                                             │
│    └─ NordInvasion.dll (PersistenceManager + хуки волн/киллов/построек)            │
│              │  HTTP POST form-urlencoded  (заголовок X-NI-Secret)                 │
│              ▼                                                                     │
│  nginx :80/:443  ──►  PHP-FPM  (index.php, config.php, lib.php)                    │
│              │  PDO (mysql, prepared statements)                                   │
│              ▼                                                                     │
│  MySQL 8  (база nordinv)                                                           │
│    players / kill_log / villages / seasons / battlepass_rewards /                  │
│    skill_nodes / campaign_votes                                                    │
└────────────────────────────────────────────────────────────────────────────────────┘
```

Ключевые решения:

- **Server-authoritative.** Все награды начисляет PHP (C# лишь сообщает события).
  Подделка локальной переменных без API ничего не даёт.
- **Идентичность игрока.** `player_id = steam_<SteamID64>` (если доступен через
  `Peer.SteamId64`, reflection-safe) иначе `name_<md5(peer_name)>`. Формула одинаковая
  в C# (`NIPeers.MakePlayerId`) и PHP (`player_identity`).
- **JSON-колонки в `players`** (`perks`, `blueprints`, `meta`, `titles`) — списки
  маленькие, читаются целиком; проще, чем 4 join-таблицы.
- **Whitelist чертежей** в `config.php` — клиент не может выдать произвольный id.
- **Мета-дерево** проверяется сервером: узел существует, prerequisite открыт,
  хватает `season_points` (дедупликация покупки).
- **Титулы** выдаются автоматически сервером по порогам: savior (50 реанимаций),
  jarl_slayer (10 боссов), engineer_master (100 построек), wall (10 волн без смертей).

## Что сохраняется (таблица `players`)

| Поле | Описание | Как меняется |
|---|---|---|
| gold | Золото (старт 500) | kill (+3..100), wave/complete, победа (+100), кампания (+200) |
| wood / metal | Ресурсы для построек (скраутинг) | kill 20-30%, волна |
| kills, deaths | Статистика | kill, run/save |
| level, xp | Уровень: пока `xp ≥ level·100` — уровень растёт | kill (+10), волна (+5·wave) |
| season_points | Очки сезона: покупка мета-узлов | kill (+1), волна (+1), победа (+50), кампания (+10) |
| wins, losses, best_wave | Забег | run/save |
| revives / builds / boss_kills | Счётчики для рангов | stat/increment, kill(is_boss) |
| perks | Выбранные перки (id 0-24) | perk/record, wave/complete(perk_id) |
| blueprints | Чертежи (whitelist) | blueprint/unlock |
| meta | Открытые узлы дерева | meta/unlock |
| titles | Титулы | авто-выдача порогов |

## API

Все запросы: `X-NI-Secret: <API_SECRET>` (если секрет задан), тело `application/x-www-form-urlencoded`
или JSON. Ответы — JSON. Ошибки: `{"error": "..."}` + код 400/401/404/409/500.

| Метод | Путь | Поля | Что делает |
|---|---|---|---|
| POST | `/api/player/login` | player_id, steam_id, name | создаёт/тронет профиль → полный профиль |
| GET | `/api/player/{id}` | — | профиль |
| POST | `/api/kill` | player_id, steam_id, name, killed_troop, gold_reward, wood, metal, wave, is_boss | +gold/+kills/+XP/+SP, запись в kill_log, boss→boss_kills |
| POST | `/api/wave/complete` | ..., wave, gold, wood, metal, perk_id | награды волны, best_wave, +XP(5·wave), перк |
| POST | `/api/perk/record` | ..., perk_id (0-99) | сохранить выбранный перк (без наград) |
| POST | `/api/run/save` | ..., won, wave_reached, kills, deaths | победа: wins+1, +100g, +50sp; поражение: losses+1; best_wave; титул wall |
| POST | `/api/blueprint/unlock` | ..., blueprint_id | whitelist-проверка, +чертёж |
| POST | `/api/meta/unlock` | ..., node_id | prerequisite + season_points, −cost, +узел |
| POST | `/api/stat/increment` | ..., stat ∈ {revives, builds, boss_kills} | +1, авто-титулы при порогах |
| GET | `/api/campaign/villages` | — | деревни + голоса сезона |
| POST | `/api/campaign/battle` | village_id, won, players (csv), wave_reached | исход сражения, owner/defense, игрокам +200g/+10sp |
| POST | `/api/campaign/vote` | voter, village_id | голос (1 на сезон, UNIQUE) |
| GET | `/api/season/current` | — | текущий сезон |
| GET | `/api/leaderboard` | — | топ-20 по season_points |
| GET | `/api/battlepass/rewards` | — | награды battlepass |
| GET | `/health` | — | `{"ok":true,"db":"mysql"}` |

## Установка

Подробно — [`src/backend-php/README.md`](../src/backend-php/README.md) (Linux nginx+php-fpm
и Windows IIS пошагово). Коротко:

```bash
# MySQL
mysql -e "CREATE DATABASE nordinv CHARACTER SET utf8mb4;
          CREATE USER 'nordinv'@'localhost' IDENTIFIED BY 'ПАРОЛЬ';
          GRANT ALL ON nordinv.* TO 'nordinv'@'localhost';"

# Код
mkdir -p /var/www/nordinv && cp -r src/backend-php/. /var/www/nordinv/

# Конфиг: config.php -> DB_*, API_SECRET
# Схема + начальные данные:
php /var/www/nordinv/install.php

# Smoke:
bash /var/www/nordinv/tests/smoke.sh http://nordinv.example.com <API_SECRET>
```

## Подключение мода (C#)

В `PersistenceManager.cs`:

```csharp
PersistenceManager.BackendUrl = "http://127.0.0.1:8080"; // URL бэкенда
PersistenceManager.ApiSecret  = "тот-же-секрет-что-в-config.php";
```

(статические поля, ставятся при старте миссии; DedicatedServer может читать из своего конфига).

Хуки уже подключены:

| Механика | Где (C#) | Запрос |
|---|---|---|
| Логин + загрузка профиля | `PersistenceManager.OnAgentBuild` → `StartLogin` | POST /api/player/login → ApplyProfile (gold/wood/metal/perks/titles/meta) |
| Убийство норда | `NordInvasionWaveManagerBehavior.OnBotKilled` | POST /api/kill (gold, is_boss) |
| Волна завершена | `NordInvasionWaveManagerBehavior.OnWaveCompleted` | POST /api/wave/complete (по каждому живому) |
| Выбор перка | `PerkManager.ApplyPerk` | POST /api/perk/record |
| Конец забега | победа/`Defeat` в WaveManager | POST /api/run/save |
| Реанимация медика | `MedicComponent.OnTickAsAI` | POST /api/stat/increment (revives) |
| Построение | `FortressBuildManager.Place` | POST /api/stat/increment (builds) |
| Победа в кампании | `PersistenceManager.OnCampaignWin` | POST /api/campaign/battle |

Все HTTP-вызовы — `Task.Run` (не блокируют тик миссии), таймаут 10 c,
ошибки логируются в Debug и не роняют игру: без бэкенда мод работает как раньше
(профили локальные, на закате процесса пропадают).

## Dev-режим без MySQL

`config.php`: `DB_DRIVER = "sqlite"` → база в `ni_local.db`. Поднять локально:

```bash
php -S 0.0.0.0:8080 -t src/backend-php
php src/backend-php/install.php   # в config.php: DB_DRIVER='sqlite'
bash src/backend-php/tests/smoke.sh http://localhost:8080
```

## Ограничения / следующие шаги

- Боевой магазин (покупка чертежей за золото в UI) — `NI_Shop_VM` уже читает
  `PlayerGoldComponent`; подключение к `UnlockBlueprint` — следующий шаг.
- Battlepass-выдача (reward claim) — таблица есть, endpoint выдачи не написан.
- Сброс сезона (скрипт: обнулить season_points, обнулить campaign_votes за сезон).
- HTTPS обязательно в проде (секрет в заголовке).
- Бэкап: `mysqldump nordinv` раз в день (cron) — база маленькая (<10 МБ при сотнях игроков).
