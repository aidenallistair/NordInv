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

## Уже исправлено статически (портирование под 1.4.8, session 6)

| Было | Стало | Где |
|---|---|---|
| `Agent.SetHitPoints(x)` | `Agent.Health = x` | 20 вызовов в ElementalComponent, WoundStaminaComponent, RoleComponents, BarricadeMachines, TrapMachines, MetaProgressionManager, PersistenceManager |
| `Agent.SetMaximumHitPoints(x)` | `Agent.HealthLimit = x` | WoundStaminaComponent, MetaProgressionManager |
| `Mission.Current.SpawnMissile(pos, dir, 50f, item, userAgent)` (5-арг) | `Mission.Current.AddExplosion(pos + dir * 30f, 2f, 100f, userAgent)` | SiegeWeapons (баллиста) |
| `Scene.SetFog(30f, 0x888888)` / `SetFog(100f, 0xFFFFFF)` | `SetFog(..., (uint)0x...)` | NordInvasionWeatherBehavior |
| `override void OnTickAsAI(float)` (4 класса) | `override void OnTick(float)` | MedicComponent, BannerComponent, ElementalComponent, WoundStaminaComponent |
| `destructible.SetHitPoints(x)` | `destructible.HitPoints = x` | RoleComponents, FortressBuildManager (3 места) |
| `...InitialDirection(entryPoint.Direction)` | убрано (нет свойства `GameEntity.Direction`) | NordInvasionWaveManagerBehavior |
| `entity.MoveToFrame(new Frame(pos, yaw))` | `entity.MoveToFrame(new Frame(pos, yaw).ToMatrixFrame())` | PropSpawner |

`Agent` не имеет методов `SetHitPoints`/`SetMaximumHitPoints` (CS1061); у него
сеттерные свойства `int Health` и `int HealthLimit`. `DestructibleComponent.HitPoints`
и `MaxHitPoints` — сеттерные свойства.

## Осталось верифицировать по DLL (все помечены `WARN` в линтере)

| # | Место | Вызов | Риск | Кандидатный фикс |
|---|---|---|---|---|
| 1 | `BossPhaseBehavior.cs`, `ElementalComponent.cs`, `SiegeWeapons.cs` | `Mission.AddExplosion(pos, r, dmg, agent)` | Сигнатура (4-арг, `extraHitCount` опц.) | Проверить `AddExplosion(Vec3, float, float, Agent, int=0)` |
| 2 | `SquadManager.cs:30` | `formation.Captain = leader` | Нет ли публичного setter | Если CS0200 — заменить на `formation.UnitLeaderAgent`/API формирования |
| 3 | `BossPhaseBehavior.cs:69`, `BarricadeMachines.cs:33,182`, `TrapMachines.cs:20,71,80`, `SiegeWeapons.cs:64` | `Scene.AddParticleSystem(string, Vec3)` | Есть ли перегрузка `(string, Vec3)` (может быть `(string, ref MatrixFrame)`) | Если CS1503 — `AddParticleSystem(name, ref frame)` |
| 4 | `NordInvasionWeatherBehavior.cs` | `Scene.SetRainDensity/SetSnowDensity/SetTimeOfDay(float)` | Существуют ли на `Scene` | Проверить по DLL; снег может быть через `SetSnowDensity` |
| 5 | `WaveManager.cs` (`Marked` мутатор) | `bot.SetTargetForAI(agent)` | Нет ли метода `SetTargetForAI(Agent)` | Проверить `Agent.SetTargetForAI` / `SetScriptedTarget` |
| 6 | `LastStand.cs`, `SpectatorBetting.cs` | `Mission.Current.SetTimeSpeed(float)` | Существует ли | Проверить `Mission.SetTimeSpeed(float)` |
| 7 | `NordInvasionCampaignBehavior.cs` | `CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched)` с сигнатурой `(CampaignGameStarter)` | Тип события может быть `Action` | Если CS1502 — сменить сигнатуру хэндлера на без-арг |

## MP-слой — самый свежий, ни разу не компилировался (нужен реальный прогон)

`Multiplayer/` (GameMode, MissionMultiplayerNordInvasion(+Client), NIMissionRepresentative,
NISpawnBehaviors, NINetworkMessages). Использует официальный неткод, но написан
«вслепую» по 1.2-доке — на 1.4.8 сигнатуры могли разойтись. Вероятные точки:

- `MissionMultiplayerGameModeBase` / `...BaseClient`: `AfterStart()`,
  `HandleNewClientAfterSynchronized(NetworkCommunicator)`,
  `HandleNewClientAfterLoadingFinished`, `OnPeerChangedTeam`, `GetScoreForKill/Assist`,
  `CheckForMatchEnd()`, `GetWinnerTeam()`, `GetMissionType()` — виртуальны ли,
  совпадают ли сигнатуры.
- `Mission.Teams.Add(BattleSideEnum, uint, uint, Banner, bool, bool, bool)` — сигнатура.
- `MBMultiplayerOptionsAccessor.SetCultureTeam1/2(BasicCultureObject)` — существуют ли.
- `peer.ControlledAgent.Team = team` — сеттер `Agent.Team` рискован.
- Регистрация обработчиков `GameNetwork.NetworkMessageHandlerRegistererContainer`,
  `GameNetworkMessage.ClientMessageHandlerDelegate<T>`/`ServerMessageHandlerDelegate<T>`,
  `GameNetwork.AddNetworkHandler`, интерфейс `TaleWorlds.MountAndBlade.IUdpNetworkHandler`
  — на 1.4.8 API регистрации сообщений менялся (это самая вероятная зона CS-ошибок).
- `SpawnFrameBehaviorBase.GetSpawnFrame(Team, bool, bool)` / `SpawningBehaviorBase`
  (`Initialize(SpawnComponent)`, `SpawnAgents()`, `GetMaximumReSpawnPeriodForPeer`,
  `AllowEarlyAgentVisualsDespawning`) — сигнатуры виртуалов.
- `MissionRepresentativeBase.OnPeerVariableChanged()` — есть ли виртуал.
- `GameNetwork.BeginBroadcastModuleEvent()/EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags)`
  — в новых версиях enum мог переехать в другой namespace.

**Рекомендация:** MP-слой править только по реальному `rgl_log.txt` — это самая
большая зона неопределённости. Одиночный Custom Battle (singleplayer) соберётся
раньше и не зависит от MP-кода в части регистрации сообщений (миссии запускаются
через `OnMissionBehaviorInitialize`).

## Рекомендуемый порядок при сборке

1. `python3 tools/validate_module.py` — должно быть «0 ошибок» (12 варнингов
   про terrain.bin — ожидаемо, нужен `prepare_scenes.py` на машине с игрой).
2. Build в Rider/VS. Сначала исправить CS02xx/CS01xx (usings/опечатки) — их
   линтер уже снял, поэтому маловероятны.
3. Останутся в основном `CS1503` (аргументы) и `CS1061` (нет члена) на таблицу
   выше. Править точечно, менять таблицу в этом файле (✔ когда проверено).
4. После успешной сборки: Custom Battle `mp_ni_bridge_01` → `mp_nord_invasion`
   → чек-лист `docs/LAUNCH_GUIDE.md` → потом Dedicated Server.
