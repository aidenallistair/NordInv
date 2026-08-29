# Звуки Nord Invasion (пункт плана "Звуки для мутаторов, Last Stand музыка")

## Что уже есть

Кодовые триггеры (`src/NordInvasion/Audio/NISound.cs`) проигрывают vanilla FMOD-события
в ключевых точках:

| Триггер | Когда | Event ID (по умолчанию) |
|---|---|---|
| Мутатор | Волны 4, 8, 12... | `event:/combat/male_shout_1` |
| Босс спавн | Каждые 5 волн | `event:/combat/drum_hit_1` |
| Босс фаза 2 | 66% HP | `event:/combat/male_shout_2` |
| Босс фаза 3 | 33% HP | `event:/combat/male_shout_3` |
| Last Stand старт | 1 живой vs 1 норд | `event:/combat/horn_1` |
| Last Stand конец | | `event:/combat/horn_2` |
| Победа | 25 волн | `event:/combat/victory_horn` |
| Поражение | Все игроки мертвы | `event:/combat/defeat_horn` |
| Караван прибыл | Снабжение | `event:/ambience/bell_1` |
| Перк получен | Каждые 3 волны | `event:/ui/click_1` |

## Шаг 1: проверить ID (обязательно после первой сборки)

1. Запусти игру с модом, дойди до волны 4 (мутатор).
2. Открой `Documents/Mount and Blade II Bannerlord/Logs/rgl_log.txt`.
3. Если есть строка `cannot load sound event` — ID неверно для твоей версии.
4. Найди правильный ID в `Modules/Native/ModuleData/` (файлы со звуковыми
   событиями; ищи по имени, например `shout`, `horn`, `victory`).
5. Поправь константы в `NISound.EventIds` и пересобери dll.

Все ID собраны в **одном месте** — это единственная точка правки.

## Шаг 2 (опционально): свои звуки (.ogg)

Vanilla события ограничены. Для своего саунд-дизайна:

### Вариант A: FMOD Studio (полный контроль)
1. Установи [FMOD Studio](https://www.fmod.com/download) (бесплатно).
2. Собери event: `event:/ni/mutator_thor` и т.д. (шум, рёв, хорн).
3. Экспортируй event-банк.
4. Подключи через BLSE `ISoundEvent.CreateEventFromExternalFile`
   (интерфейс есть в BannerlordUnlocked) или через нативный плагин.

### Вариант B: готовые .ogg через BLSE
1. Подготовь .ogg (22-44 kHz, mono/stereo).
2. `CreateEventFromSoundBuffer` / `CreateEventFromExternalFile` с путём
   `Modules/NordInvasion/Audio/<имя>.ogg`.
3. Поставь файлы сюда: `ModuleData/Sounds/` (эта папка - место для кастомных
   аудио-ассетов; пока пуста, т.к. кастомные события требуют BLSE).

## Файлы для добавления (список арт-задач)

- `Sounds/mutator_thor.ogg` - рёв/гром (Thor's Fury)
- `Sounds/mutator_odin.ogg` - скрежет/ветер (Odin's Mark)
- `Sounds/mutator_loki.ogg` - звон монет (Loki's Greed)
- `Sounds/last_stand_drum.ogg` - драм-ролл на 10 сек
- `Sounds/boss_phase_horn.ogg` - рог (2 фазы босса)
- `Sounds/victory_choir.ogg` - фанфары победы
- `Sounds/defeat_drums.ogg` - тяжёлые удары (поражение)

Пока кастомные события не добавлены — используются vanilla-замены из таблицы выше.
