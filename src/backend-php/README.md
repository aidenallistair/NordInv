# Nord Invasion Backend (PHP + MySQL)

Бэкенд сохранения для **Nord Invasion Better Edition**: золото/дерево/металл, уровень/XP,
очки сезона, перки, чертежи, мета-дерево, титулы, статистика побед, кампания (деревни/голоса),
сезоны, battlepass. Работает рядом с Bannerlord Dedicated Server: мод шлёт HTTP-запросы,
PHP пишет в MySQL.

```
Bannerlord Dedicated Server
        │  HTTP (form-encoded → JSON)
        ▼
  nginx (80/443)  →  PHP-FPM  →  MySQL 8
        ▲
   DedicatedServer (C#) опрашивает этот же API
```

## Требования

- PHP 7.4+ (расширения: `pdo_mysql` — есть по умолчанию)
- MySQL 5.7+ / 8.x (или MariaDB 10.3+)
- nginx (Linux) или IIS (Windows)
- 64-bit, любой хост, где крутится выделенный сервер

## Установка (Linux, nginx + php-fpm)

```bash
# 1. Копируем код
sudo mkdir -p /var/www/nordinv
sudo cp -r src/backend-php/. /var/www/nordinv/
sudo chown -R www-data:www-data /var/www/nordinv

# 2. База
sudo mysql -u root -p -e "CREATE DATABASE nordinv CHARACTER SET utf8mb4;"
sudo mysql -u root -p -e "CREATE USER 'nordinv'@'localhost' IDENTIFIED BY 'ПАРОЛЬ';
                         GRANT ALL ON nordinv.* TO 'nordinv'@'localhost'; FLUSH PRIVILEGES;"

# 3. Конфиг
nano /var/www/nordinv/config.php
#    DB_DRIVER = 'mysql', DB_HOST, DB_NAME, DB_USER, DB_PASS, API_SECRET

# 4. Схема + сид
cd /var/www/nordinv
php install.php

# 5. nginx (site: /etc/nginx/sites-available/nordinv)
server {
    listen 80;
    server_name nordinv.example.com;
    root /var/www/nordinv;
    index index.php;

    # защищаем: только API + health, остальное - 403
    location ~ /\.ht { deny all; }
    location ~ ^/(config|install|lib|schema|seed)\. { deny all; }

    location / {
        try_files $uri /index.php$is_args$args;
    }
    location ~ \.php$ {
        include snippets/fastcgi-php.conf;
        fastcgi_pass unix:/run/php/php8.2-fpm.sock; # под свою версию PHP
    }
}
sudo ln -s /etc/nginx/sites-available/nordinv /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx

# 6. Проверка
curl http://nordinv.example.com/health
# => {"ok":true,"db":"mysql","time":...}
```

## Установка (Windows, IIS)

1. Ставим **PHP for Windows** (php.net, x64, Thread Safe) в `C:\php`.
2. В `php.ini`: `extension=pdo_mysql`, `extension_dir = "ext"`.
3. IIS: **Add Web Site** → physical path `C:\nordinv` (сюда скопирован `src/backend-php`),
   port 8080 (или ваш).
4. **Handler Mappings** → Add Script Map:
   - Executable: `C:\php\php-cgi.exe`
   - Extension: `.php`
   - Module: `FastCgiModule`
   - Check "Verify that mapped path exists", Script Processor Options: Pass CGI arguments as headers.
   - Request Restrictions: remove `.exe`.
5. База: MySQL/MariaDB (или XAMPP) → `CREATE DATABASE nordinv CHARACTER SET utf8mb4;`
   пользователь + права как в Linux-варианте.
6. `C:\php\php.exe C:\nordinv\install.php`
7. `C:\nordinv\config.php` → `DB_DRIVER='mysql'`, `API_SECRET`.
8. Проверка: `http://<host>:8080/health`.

> IIS по умолчанию не отдаёт JSON с PHP — после шага 4 это работает. Если 503 —
> проверите, что FastCgiModule и приложение запущены (`%windir%\system32\inetsrv\appcmd list vdir`).

## Smoke-тест

После установки: `bash tests/smoke.sh http://nordinv.example.com`
Проверяет: health → login → kill → wave/complete → run/save → blueprints → meta → stat →
campaign → leaderboard → season. Всё должно вернуть `ok`.

## Безопасность

- **API_SECRET** обязателен в проде: мод шлёт заголовок `X-NI-Secret`, без него 403.
  Секрет совпадает в `config.php` и в строке подключения мода (`NIPersistence.ServerUrl`,
  параметр `secret`).
- MySQL-юзер — только на `nordinv.*`, доступ с localhost.
- nginx: закрыт прямой доступ к `config.php`, `install.php`, `lib.php`, `schema.sql`.
- Все запросы через prepared statements — SQL-инъекций нет.
- Рекомендация: HTTPS (Let's Encrypt / SSL-сертификат IIS) — секрет не должен лететь в открытом виде.

## Как мод подключается

C#-сторона (`PersistenceManager.cs`) шлёт `form-urlencoded` на:

| Метод | Путь | Тело | Что делает |
|---|---|---|---|
| POST | `/api/player/login` | player_id, steam_id, name | создаёт/тронет профиль, возвращает профиль |
| GET | `/api/player/{id}` | — | профиль (gold, wood, metal, level, xp, blueprints, perks, meta, titles...) |
| POST | `/api/kill` | player_id, steam_id, name, killed_troop, gold_reward, wood, metal, wave, is_boss | +золото/+XP/+kill, запись в kill_log |
| POST | `/api/wave/complete` | ..., wave, gold, wood, metal, perk_id | награды за волну, best_wave, перк |
| POST | `/api/run/save` | ..., won, wave_reached, kills, deaths | победа/поражение, бонусы, титул «wall» |
| POST | `/api/blueprint/unlock` | ..., blueprint_id | разблокировка чертежа (whitelist в config) |
| POST | `/api/meta/unlock` | ..., node_id | покупка узла мета-дерева за season_points |
| POST | `/api/stat/increment` | ..., stat (revives\|builds\|boss_kills) | счётчики + авто-титулы |
| GET | `/api/campaign/villages` | — | список деревень + голоса |
| POST | `/api/campaign/battle` | village_id, won, players, wave_reached | исход сражения, награды игрокам |
| POST | `/api/campaign/vote` | voter, village_id | голос (1 на сезон) |
| GET | `/api/season/current` | — | текущий сезон |
| GET | `/api/leaderboard` | — | топ-20 по season_points |
| GET | `/api/battlepass/rewards` | — | награды battlepass |
| GET | `/health` | — | проверка API+БД |

Все ответы — JSON. Ошибки: `{"error": "..."}` + HTTP 400/404/409/500.

## Dev-режим без MySQL

`config.php`: `DB_DRIVER = 'sqlite'` — база в `ni_better.db` рядом с кодом.
Всё то же API, `php install.php` создаст файл. Для локальной проверки:
`php -S 0.0.0.0:8080 -t src/backend-php`.
