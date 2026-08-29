# Nord Invasion Better Edition - Bannerlord Module

Порт Fianna Nord Invasion с 15 новыми механиками на Bannerlord.

## Установка для разработки

1. Установи Bannerlord + Modding Kit (Steam -> Tools -> Mount & Blade II Bannerlord - Modding Kit)
2. Установи зависимости: ButterLib, UIExtenderEx, ModConfigurationMenu v5 (с Nexus)
3. Клонируй этот репо в `Mount & Blade II Bannerlord/Modules/NordInvasion/`
4. Открой `BannerlordModule/NordInvasion.csproj` в Rider / Visual Studio
5. Пропиши пути к `TaleWorlds.*.dll` в csproj (или используй `Bannerlord.Module.Template`)
6. Скомпилируй -> `Modules/NordInvasion/bin/Win64_Shipping_Client/NordInvasion.dll`

## Структура

- `src/NordInvasion/SubModule.cs` - точка входа
- `Behaviors/` - логика волн, директора, погоды, целей, мутаторов
- `Components/` - компоненты агентов (медик, ранения, перки)
- `Machines/` - разрушаемые объекты (баррикады, колья, котел)
- `Managers/` - стройка, скавенджинг, отряды, лут, персистенция
- `UI/` - Gauntlet UI для магазина, перков, стройки, кампании

## Тест в игре

- Запусти Bannerlord, включи мод NordInvasion в лаунчере
- Custom Battle -> Scene `mp_ni_bridge_01` -> Mission `mp_nord_invasion`
- Или через Co-op мод: Host -> NordInvasion mission

## Кооп vs Dedicated

- **Co-op (проще):** Используй мод `Bannerlord Co-op` - он синхронизирует сингл-миссию для 4-8 игроков. Наш WaveManager работает из коробки.
- **Dedicated (сложнее):** Нужен `DedicatedCustomServer.exe` + конфиг из `DedicatedServer/Bannerlord/`. Требует доп. работы с `NetworkCommunicator`.

## Backend

Бекенд уже готов в `src/backend/main.py` - поддерживает персистенцию, чертежи, сезоны, кампанию. Запусти `uvicorn main:app --reload` и укажи URL в `PersistenceManager.cs`.

## Что реализовано

Все 15 механик из `docs/BETTER_EDITION.md` уже в виде C# скелета. Нужно дописать детали UI и баланс.

## Следующие шаги

1. Создать сцены `mp_ni_*` в Bannerlord Scene Editor (порт из Warband)
2. Создать CharacterObject XML для нордов в `ModuleData/Characters.xml`
3. Дописать Gauntlet UI (пример в `UI/`)
4. Протестировать с 4 игроками
