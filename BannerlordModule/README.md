# Nord Invasion Better Edition - Bannerlord Module

Кооперативный PvE мод для Bannerlord. Оборона от волн нордов с 15 механиками.

## Установка для разработки

1. Установи Bannerlord + Modding Kit (Steam -> Tools -> Mount & Blade II Bannerlord - Modding Kit)
2. Установи зависимости: ButterLib, UIExtenderEx, ModConfigurationMenu v5 (Nexus)
3. Клонируй репо в `.../Mount & Blade II Bannerlord/Modules/NordInvasion/`
4. Открой `BannerlordModule/NordInvasion.csproj` в Rider / VS
5. Пропиши пути к TaleWorlds.*.dll
6. Скомпилируй -> `Modules/NordInvasion/bin/Win64_Shipping_Client/NordInvasion.dll`

## Структура

- `src/NordInvasion/SubModule.cs` - точка входа
- `Behaviors/` - волны, директор, погода, цели, мутаторы
- `Components/` - медик, ранения, перки
- `Machines/` - баррикады, колья, котел
- `Managers/` - стройка, отряды, лут, персистенция
- `UI/` - HUD, магазин, перки, стройка, кампания
- `Models/` - WaveDefinition, Mutator, Perk, Village

## Тест

- Запусти Bannerlord, включи мод NordInvasion
- Custom Battle -> Scene `mp_ni_bridge_01` -> Mission `mp_nord_invasion`
- Или через Co-op мод: Host -> NordInvasion mission

## Backend

Бекенд в `src/backend/main.py` - персистенция, чертежи, сезоны, кампания.

```bash
cd src/backend
uvicorn main:app --reload
```
