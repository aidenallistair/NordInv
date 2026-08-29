# Nord Invasion Better Edition - Bannerlord

Кооперативный PvE мод для Mount & Blade II: Bannerlord. Оборона Свадии от волн нордов с прокачкой, строительством и мета-прогрессией.

## Что это

Команда 4-32 игроков держит форт против волн:

- Волны усиливаются, каждые 3 волны - спец-цель (таран, эскорт, поджог лагерей)
- Роли: Медик поднимает, Инженер строит/чинит, Знаменосец бафает, Пехота/Лучник дамажат
- Модульный форт: фундамент -> стены -> ворота -> колья против кавалерии -> масляный котел
- Roguelite перки каждые 3 волны, мутаторы богов каждые 4, AI-Директор как в L4D
- Погода влияет: туман слепит лучников, дождь тушит огненные стрелы, ночь требует факелов
- Физ-лут с боссов надо донести до казны, скавенджинг ресурсов с трупов и обломков
- Персистенция по SteamID: золото, чертежи, сезоны, BattlePass, глобальная кампания 8 деревень

## Структура проекта

```
BannerlordModule/ - C# мод
  Modules/NordInvasion/SubModule.xml - описание модуля
  src/NordInvasion/
    SubModule.cs - точка входа
    Behaviors/ - логика волн, директора, погоды, целей, мутаторов
    Components/ - медик, ранения/стамина, перки, лут
    Machines/ - баррикады, колья, котел, казна, таран
    Managers/ - стройка, отряды с формациями, скавенджинг, персистенция
    UI/ - HUD, магазин, выбор перков, стройка, карта кампании
    Models/ - WaveDefinition, Mutator, Perk, Village

DedicatedServer/Bannerlord/ - конфиг и скрипты для выделенного сервера
src/backend/ - FastAPI бекенд для персистенции, кампании, сезонов

docs/BANNERLORD_PLAN_RU.md - полный план реализации 15 механик
```

## Быстрый старт для разработки

1. Установи Bannerlord + Modding Kit (Steam -> Tools)
2. Установи зависимости с Nexus: ButterLib, UIExtenderEx, ModConfigurationMenu v5
3. Склонируй репо в `.../Mount & Blade II Bannerlord/Modules/NordInvasion/`
4. Открой `BannerlordModule/NordInvasion.csproj` в Rider/VS, пропиши пути к TaleWorlds.*.dll
5. Скомпилируй -> `Modules/NordInvasion/bin/Win64_Shipping_Client/NordInvasion.dll`
6. В лаунчере включи NordInvasion
7. Custom Battle -> Scene `mp_ni_bridge_01` -> Mission `mp_nord_invasion` или через Co-op мод

## Dedicated Server

```bash
# Скачать сервер через SteamCMD
steamcmd +login anonymous +app_update 1058080 validate +quit

# Запуск
TaleWorlds.MountAndBlade.DedicatedCustomServer.exe /dedicatedcustomserverconfig DedicatedServer/Bannerlord/DedicatedCustomServerConfig.xml
```

Подробнее в `DedicatedServer/Bannerlord/README.md`

## Backend (персистенция, кампания, сезоны)

```bash
cd src/backend
pip install -r requirements.txt
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

API:
- `POST /api/player/login` - логин по SteamID
- `POST /api/kill` - убийство, золото, ресурсы
- `GET /api/campaign/villages` - деревни кампании
- `POST /api/campaign/battle` - результат битвы за деревню
- `GET /api/leaderboard` - топ игроков

## 15 механик Better Edition

1. **Roguelite перки** - выбор 1 из 3 каждые 3 волны
2. **Модульный форт** - стройка из фундамента в стены, ворота, колья, котел
3. **Роли** - Медик, Инженер, Знаменосец, Пехота, Лучник
4. **Цели волн** - таран, эскорт, поджог лагерей, защита казны
5. **AI-Директор** - адаптирует сложность под команду как в L4D2
6. **Погода/время** - туман, дождь, снег, ночь влияют на бой
7. **Кавалерия нордов** - фланговые рейды, контрмеры кольями
8. **Физ-лут** - мешок с босса надо донести до казны
9. **Скавенджинг** - ресурсы с трупов и обломков, крафт у костра
10. **Мутаторы богов** - 12 проклятий от Тора, Локи, Одина и т.д.
11. **Отряды с формациями** - стена щитов, клин берсерков, лучники под прикрытием
12. **Персистенция 2.0** - SteamID, чертежи, скины, сезоны, BattlePass
13. **Ранения/усталость** - 3 падения до смерти, стамина влияет на урон
14. **Разрушаемость/огонь** - деревья падают, бочки взрываются, поджоги
15. **Глобальная кампания** - 8 деревень, голосование, захват карты

Детали в `docs/BANNERLORD_PLAN_RU.md`

## Следующие шаги (актуальный план, см. docs/PROGRESS.md)

1. **Террейн карт (Windows + Bannerlord):** `python3 tools/prepare_scenes.py`
   - сцены `mp_ni_*` уже сгенерированы (XML: 65 entry points + пропсы,
     `tools/gen_ni_scenes.py`); скрипт дополнит их бинарным террейном из vanilla
2. **Собрать dll:** открыть `BannerlordModule/NordInvasion.csproj`, прописать
   HintPath, Build. При ошибках компиляции - точечные правки (API меняется
   между патчами Bannerlord; см. BUILD_FROM_SOURCE.md)
3. **Тест:** Custom Battle -> `mp_ni_bridge_01` -> `mp_nord_invasion`,
   пройти чеклист из `docs/LAUNCH_GUIDE.md`
4. **Арт-задачи** (docs/ART_TASKS.md): иконки перков, меши ni_*-пропсов,
   кастомные звуки (UI и код уже готовы, ждут ассеты)
5. **Тест Dedicated Server** (2 клиента, SteamCMD) -> upload на NexusMods
   (source-зип уже собран: `dist/NiNordInvasion_v2_0_0_source.zip`)

Полезные инструменты: `tools/validate_module.py` (проверка модуля перед релизом),
`tools/make_release.py` (сборка релиз-зипа).
