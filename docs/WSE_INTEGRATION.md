# Интеграция с WSE для персистенции

WSE (Warband Script Enhancer) добавляет операции которых нет в Native:

### Полезные WSE операции для Nord Invasion:

```python
# HTTP запросы к бекенду
(wse_http_get, ":result", "http://localhost:8000/api/player/123"),
(wse_http_post, ":result", "http://localhost:8000/api/kill", "player_id=123&gold=10"),

# Файловые операции - сохранение прогресса локально
(wse_file_write_string, "@saves/player_123.txt", "@gold=500"),
(wse_file_read_string, "@saves/player_123.txt", s1),

# Расширенные операции с ботом ИИ
(wse_agent_set_max_health, ":agent_no", 200),

# JSON парсинг
(wse_json_parse, ":json_id", s1),
(wse_json_get_int, ":gold", ":json_id", "gold"),

# Более 100 ботов на карте (Native лимит 100-150, WSE до 300)
```

### Пример скрипта с бекендом:

```python
("nord_invasion_backend_player_joined",
 [
   (store_script_param, ":player_no", 1),
   (player_get_unique_id, ":unique_id", ":player_no"), # WSE operation
   (player_get_name, s1, ":player_no"),
   
   # Формируем URL
   (str_store_string, s2, "@http://localhost:8000/api/player/login"),
   # POST запрос
   (str_store_string, s3, "@player_id={reg1}&player_name={s1}"),
   (assign, reg1, ":unique_id"),
   (wse_http_post, reg2, s2, s3), # reg2 = http result code
   
   # Если успешно, парсим золото
   (try_begin),
     (eq, reg2, 200),
     (wse_http_get_response_body, s4),
     (wse_json_parse, ":json", s4),
     (wse_json_get_int, ":gold", ":json", "gold"),
     (player_set_slot, ":player_no", slot_player_ni_gold, ":gold"),
   (try_end),
 ]),
```

### Без WSE - упрощенный вариант:

Если не хочешь ставить WSE, можно обойтись Native операциями, но с ограничениями:
- Макс 100 ботов одновременно (хватит для малых волн)
- Нет HTTP, только слоты игроков (прогресс сбрасывается при выходе)
- ИИ ботов тупее

Fianna использовали WSE обязательно - иначе их мод не работал.

### Где скачать WSE:

- WSE 4.8.0: https://www.nexusmods.com/mbwarband/mods/6123
- WSE2: https://forums.taleworlds.com/index.php?threads/warband-script-enhancer-2-wse2-v1-1-0-7.324870/

Установка:
1. Распаковать в папку сервера
2. Создать папку `WSE` рядом с `mb_warband_dedicated.exe`
3. Скопировать туда `WSELoaderServer.exe`, `WSEServer.dll`, etc
4. Заменить `mb_warband_dedicated.exe` на версию из WSE архива (она патченная)
5. Запускать через `WSELoaderServer.exe -r config -m Module -p exe`

В логе должно быть: `WSE Loader: Module loaded successfully`
