# Быстрый старт за 15 минут

## Для тех кто хочет просто поднять сервер как на Fianna.ru

### Шаг 1: Серверные файлы

1. Скачай dedicated server 1.174 с сайта TaleWorlds (Other Downloads)
2. Распакуй в `C:\WarbandServer\`
3. Скачай WSE 4.8.0 и распакуй:
   - Создай `C:\WarbandServer\WSE\`
   - Скопируй туда `WSELoaderServer.exe`, `WSEServer.dll`, `mb_warband_dedicated.exe` (из WSE)
4. Скопируй готовый модуль (если нет исходников - возьми Native и добавь наши файлы, или скачай Fianna_NordInvasion с fianna.ru торрента - он еще жив)

### Шаг 2: Конфиг

Создай `C:\WarbandServer\nordinvasion.cfg` из `ServerConfig/nordinvasion.cfg` в этом репо. Поменяй `set_pass_admin`.

### Шаг 3: Запуск

Запусти `ServerConfig/start_server.bat` (скопируй в корень сервера)

Должен увидеть:

```
WSE Loader v4.8.0
Loading module... Fianna_NordInvasion
Server started on port 7240
```

### Шаг 4: Клиент

1. У клиента должен быть Warband 1.174 и точно такой же модуль `Fianna_NordInvasion` в `Modules/`
2. Запуск игры с WSE: `WSELoader.exe` (для клиента)
3. В игре: Multiplayer -> Add server to favorites -> IP твоего сервера:7240
4. Подключайся

### Шаг 5: Проверка

- Зашел 1 игрок - через 8 сек должна заспавниться волна 1 (10 крестьян)
- Убил - получил золото
- Нажал F на сундук - открылся магазин
- Убил всех - волна 2
- Умер - ждешь 4 волны для респавна

Если не работает:
- Проверь что в `module_mission_templates.py` есть `mp_nord_invasion` и в `nordinvasion.cfg` `set_mission mp_nord_invasion` и карта существует
- Проверь логи `WSE/logs/` и `server_log.txt`
- Порт 7240 UDP открыт? Проверь `netstat -an | find "7240"`

### Без WSE (упрощенно):

Если не хочешь заморачиваться с WSE, можно запустить Native dedicated без WSE, но тогда боты не будут работать нормально. Нужно использовать костыль:

В `module_mission_templates.py` вместо `add_visitors_to_current_scene` используй `spawn_agent` + `agent_set_team` - но это работает только в сингле. В мульте без WSE ботов можно спавнить только через `add_visitors` которые спавнятся в начале миссии, а не динамически.

Поэтому для Nord Invasion WSE обязателен.

### Где взять готовый Fianna_NordInvasion модуль:

1. Торрент с fianna.ru: https://mbhandlerservers.fianna.ru/forum/viewtopic.php?f=53&t=1082 (нужна регистрация)
2. Яндекс.Диск / Google Drive ссылки там же
3. Или собери из этого репо (ModuleSystem/*)

Готовый модуль от Fianna уже содержит все карты и баланс - можно просто взять его и запустить по этой инструкции, без компиляции.

### Минимальные системные требования сервера:

- CPU: 2 ядра 2.5GHz (Warband сервер однопоточный, но Wine ест)
- RAM: 1GB
- Интернет: 10 Mbps upload для 32 игроков
- OS: Windows 10/Server 2019 или Linux с Wine
- Трафик: ~50KB/s на игрока

Для 32 игроков нужно `set_upload_limit 1500000` (1.5M)

### Как сделать сервер публичным:

1. Пробрось порт 7240 UDP на роутере (Port Forwarding)
2. Узнай внешний IP: https://2ip.ru
3. Дай игрокам IP
4. Если `set_add_to_game_servers_list 1`, сервер появится в списке Internet (если мастер-сервер TaleWorlds работает)

Fianna делали свой мастер-сервер, чтобы обходить проверку ключа. Сейчас официальный мастер еще работает, но если хочешь свой - смотри проект `Warband Master Server Emulator` на GitHub.

Готово! Удачной обороны!
