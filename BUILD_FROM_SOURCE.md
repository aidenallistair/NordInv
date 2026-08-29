# Сборка NordInvasion из исходников (Windows, нужна установка Bannerlord)

Этот пакет — source-релиз: `NordInvasion.dll` не входит, потому что
компиляция требует DLL самой игры (TaleWorlds.*.dll из bin/Win64_Shipping_Client).

## Шаги

1. **Bannerlord 1.2.10+** через Steam + **Modding Kit** (Steam → Library → Tools).

2. **Зависимости с NexusMods** (включить в лаунчере перед NordInvasion):
   - ButterLib 2.8.11+
   - UIExtenderEx 2.8.0+
   - Mod Configuration Menu v5 5.9.0+

3. **Копирование модуля:**
   ```
   xcopy /E /I Modules\NordInvasion "C:\...\Mount & Blade II Bannerlord\Modules\NordInvasion"
   ```

4. **Компиляция dll:**
   - Открой `src-build/NordInvasion.csproj` в Rider/Visual Studio.
   - Замените HintPath на путь к твоей игре:
     `C:\...\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client\*.dll`
   - Build (Release). DLL появится в
     `Modules/NordInvasion/bin/Win64_Shipping_Client/NordInvasion.dll`.
   - Или используй `build_module.bat` (пропиши свой BANNERLORD_PATH).

5. **Террейн для карт (обязательно, один раз):**
   ```
   python3 tools/prepare_scenes.py
   ```
   Копирует `terrain.bin`/`flora.bin`/`ShaderCache` из vanilla-сцены
   (`mp_ye_battle_01`) в папки `mp_ni_*`. Затем пересобери/скопируй ModuleData.

6. **Проверка:**
   - Launcher → включи ButterLib, UIExtenderEx, MCMv5, NordInvasion.
   - Custom Battle → Map `mp_ni_bridge_01` → Mission `mp_nord_invasion` → Start.
   - Через 8 сек должна появиться "Wave 1 preparing..." и заспавниться ~12 ботов.

7. **Диагностика:** `Documents/Mount and Blade II Bannerlord/Logs/rgl_log.txt`.
   - `cannot load scene prop` — пропс без меша (нормально: сработает fallback
     или пропс не спавнится; список в docs/ART_TASKS.md).
   - `cannot load sound event` — поправь ID в `src/NordInvasion/Audio/NISound.cs`
     (одна таблица).
   - `CS####` ошибки компиляции — проверь версию игры; API меняется
     между патчами Bannerlord.

## Dedicated Server
См. `docs/LAUNCH_GUIDE.md` раздел "Dedicated Server" и
`src-build/` (конфиг в `DedicatedServer/Bannerlord/` в полном репозитории).
