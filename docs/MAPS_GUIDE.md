# Карты для Nord Invasion

## Требования к карте:

1. **Entry points 0-31** - спавн игроков (defenders)
   - Расположи их в одном месте, в укреплении / за баррикадами
   - Должны быть близко друг к другу
   - Пример: внутренний двор замка

2. **Entry points 32-63** - спавн нордов (attackers)
   - Расположи вокруг карты, 3-4 направления атаки
   - 32-39: север, 40-47: восток, 48-55: юг, 56-63: запад
   - Боты должны видеть игроков и иметь путь (AI mesh)

3. **Entry point 64** - босс
   - Центр карты атаки, эффектный спавн

4. **Scene props**:
   - `spr_ni_armory_chest` - 2-3 штуки в зоне игроков
   - `spr_ni_barricade_wood` места - где можно ставить баррикады (не обязательно)
   - Стены, укрытия

## Как сделать карту:

### Вариант 1: Thorgrim's Map Editor (старый, но рабочий)
- Входит в Module System: `Tools/Thorgrim/`
- Открываешь `scn_mp_nord_town_01` и редактируешь

### Вариант 2: Warband Edit Mode
- В игре: Camp -> Edit Mode
- Но для мультиплеерных карт лучше Thorgrim

### Вариант 3: Переделать Native карты
Возьми `mp_town`, `mp_castle`, `mp_village` и:
- Добавь сундуки
- Перенеси entry points 32-63 за стены
- Добавь баррикады как разрушаемые объекты

## Список карт Fianna:

У Fianna было ~8 карт:
- mp_ni_town_01 - город, узкие улицы
- mp_ni_village_01 - деревня, открытая
- mp_ni_castle_01 - замок, оборона ворот
- mp_ni_forest_01 - лес, засады
- mp_ni_snow_01 - снег, нордская тематика
- mp_ni_desert_01 - пустыня
- mp_ni_fort_01 - форт
- mp_ni_bridge_01 - мост, бутылочное горлышко (самая популярная для NI)

Самая лучшая для NI - bridge / fort с одним проходом, где можно ставить баррикады.

## Добавление карты в мод:

В `module_scenes.py`:

```python
("mp_ni_town_01", sf_generate, "none", "none", (0,0),(100,100),-100,"0x000000003000000000000000000000000000000000000000",
  [], [], "outer_terrain_plain"),
```

В `nordinvasion.cfg`:

```
set_map mp_ni_town_01
set_mission mp_nord_invasion
```

Или несколько карт для ротации:

```
set_mission mp_nord_invasion mp_nord_invasion mp_nord_invasion
set_map mp_ni_town_01 mp_ni_castle_01 mp_ni_bridge_01
```

Сервер будет менять карту каждую миссию (после победы/поражения).
