# Чек-лист компиляции и известные API-риски (этап 0 — «добиться сборки dll»)

Цель: собрать `NordInvasion.dll` под **Bannerlord 1.4.8 (build 1193)** максимально
с первого раза и точечно закрыть CS-ошибки по `rgl_log.txt`. Этот файл — живой
трекер: обновляется по мере верификации по DLL.

**Версия:** 1.4.8.1193. Reference assemblies в `NordInvasion.csproj` =
`Bannerlord.ReferenceAssemblies 1.4.8.119303` (совпадает). **War Sails (NavalDLC)
не используется** — модуль работает без морского DLC, в коде и XML ссылок на него нет.

## Как использовать

1. Открой `BannerlordModule/NordInvasion.csproj`, пропиши `HintPath` к
   `TaleWorlds.*.dll` из установки игры (см. `BUILD_FROM_SOURCE.md`), Build.
2. Открой `rgl_log.txt`. Любая строка `error CS####` — сопоставь с таблицей ниже.
3. Правки — точечные; линтер (`tools/lint_csharp.py`) сам подсвечивает места,
   где остались неоднозначные API (0 ошибок = синтаксис/usings/контракты целы).

## Уже исправлено статически (не должно больше падать)

| Было | Стало | Где |
|---|---|---|
| `Agent.SetHitPoints(x)` | `Agent.Health = x` | 20 вызовов в ElementalComponent, WoundStaminaComponent, RoleComponents, BarricadeMachines, TrapMachines, MetaProgressionManager, PersistenceManager |
| `Agent.SetMaximumHitPoints(x)` | `Agent.HealthLimit = x` | WoundStaminaComponent, MetaProgressionManager |

`Agent` не имеет методов `SetHitPoints`/`SetMaximumHitPoints` (CS1061); у него
сеттерные свойства `int Health` и `int HealthLimit`.

## Осталось верифицировать по DLL (все помечены `WARN` в линтере)

| # | Место | Вызов | Риск | Кандидатный фикс |
|---|---|---|---|---|
| 1 | `Machines/SiegeWeapons.cs:26` | `Mission.Current.SpawnMissile(pos, dir, 50f, item, userAgent)` | Сигнатура 5-арг может не совпасть | Проверить перегрузки `Mission.SpawnMissile`; обычно нужны `shooterAgent`, `weaponData` |
| 2 | `Behaviors/BossPhaseBehavior.cs:83`, `Components/ElementalComponent.cs:140`, `Machines/SiegeWeapons.cs:53` | `Mission.Current.AddExplosion(pos, r, dmg, agent)` | Сигнатура не подтверждена | Проверить `Mission.AddExplosion` (возможно `(Vec3, float, float, Agent, int extraHitCount=0)`) |
| 3 | `Machines/PropSpawner.cs:33` | `scene.LoadSceneProp(propId)` | Есть ли 1-арг перегрузка | Если CS1503 — `LoadSceneProp(propId, frame)` и передавать `MatrixFrame` |
| 4 | `Behaviors/NordInvasionWeatherBehavior.cs:25,42` | `Scene.SetFog(30f, 0x888888)` | 2-й аргумент может быть `uint` → int→uint нет неявного | `(uint)0x888888` |
| 5 | `Managers/SquadManager.cs:30` | `formation.Captain = leader` | Нет ли публичного setter | `Formation.Captain` обычно сеттерный; если нет — заменить на API формирования |
| 6 | `Components/RoleComponents.cs:102`, `Managers/FortressBuildManager.cs:195,214` | `destructible.SetHitPoints(...)` | `DestructibleComponent.SetHitPoints` — сигнатура | Проверить `HitPoints`/`MaxHitPoints` setter |
| 7 | Все `Components/*`, `Behaviors/*` | `override void OnTickAsAI(float)` (4 класса) | Виртуала нет в 1.0.3 | В 1.2+/1.4 `OnTickAsAI(float)` есть; если CS0115 — заменить на `OnTick` |

## MP-слой — самый свежий и не компилировался

`Multiplayer/` (GameMode, MissionMultiplayer*, NIMissionRepresentative, NISpawnBehaviors,
NINetworkMessages) использует официальный неткод Bannerlord:

- `GameNetworkMessage` + `WriteIntToPacket/ReadIntFromPacket/WriteStringToPacket/
  ReadStringFromPacket`, `CompressionInfo.Integer` — для 1.2.x выглядит корректно.
- Регистрация обработчиков в `MissionMultiplayerNordInvasion.AddRemoveMessageHandlers`
  сделана через reflection с fallback на `GameNetwork.NetworkMessageHandlerRegisterer`
  — это площадка, где чаще всего будут расходиться сигнатуры между патчами.

Если в MP-коде будут CS-ошибки, править по `rgl_log` в порядке появления.
Контракт стройки (клиент→сервер→broadcast) от этого не зависит — это чистый C#.

## Рекомендуемый порядок при сборке

1. `python3 tools/validate_module.py` — должно быть «0 ошибок» (12 варнингов
   про terrain.bin — ожидаемо, нужен `prepare_scenes.py` на машине с игрой).
2. Build в Rider/VS. Сначала исправить CS02xx/CS01xx (usings/опечатки) — их
   линтер уже снял, поэтому маловероятны.
3. Останутся в основном `CS1503` (аргументы) и `CS1061` (нет члена) на таблицу
   выше. Править точечно, менять таблицу в этом файле (✔ когда проверено).
4. После успешной сборки: Custom Battle `mp_ni_bridge_01` → `mp_nord_invasion`
   → чек-лист `docs/LAUNCH_GUIDE.md` → потом Dedicated Server.
