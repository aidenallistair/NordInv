# Аудит модуля NordInvasion (session 2, 2026-08-29)

## Что это за проект

M&B II: Bannerlord, кооп-PvE "hold the fort": 25 волн нордов, 29 механик
(15 базовых + 14 доп., без питомцев), 4-32 игрока, метка-персистенция
через FastAPI backend.

## Реальное состояние на момент аудита

Документация (PROGRESS.md) заявляла "DONE 90%". Фактически:

| Слой | Реальность |
|---|---|
| C# (24 файла, ~2938 строк) | **Скелет**: все 29 механик задуманы, ~12 из них рабочие, ~10 — заглушки "показать сообщение" |
| XML (12 файлов) | Валидный, но **SubModule.xml регистрировал только 2 из 6** — Characters/Items/Missions/SceneProps игра не подгружала (мод не работал бы как задумано) |
| Сцены | Не существовали (только записи в MultiplayerScenes.xml) |
| Компиляция | **Никогда не прогонялась**: `MathF` (нет в net472), `Peer.Communicator`, `SetActionChannel` с неверной сигнатурой, `Quaternion.f`, отсутствие `using TaleWorlds.Core` в 20 файлах, `System.Net.Http` без ссылки в csproj |
| Backend (FastAPI) | Рабочий, полный |

## Найденные и исправленные проблемы (session 2)

### Блокеры
1. **SubModule.xml** — не зарегистрированы Characters.xml, Items.xml, Missions.xml,
   SceneProps.xml. Исправлено (+ правильные пути, относительные от ModuleData/).
2. **Missions.xml** — нестандартный блок `<Behaviors>` (в Bannerlord behaviors
   регистрируются только кодом). Убран.
3. **NordInvasion.csproj** — нет ссылки `System.Net.Http` (нужна `HttpClient`
   на net472). Добавлена.

### Ошибки компиляции (high confidence)
4. `MathF.Clamp` в DirectorBehavior — MathF отсутствует в net472 →
   `Utils/NIMath.ClampInt`.
5. `Debug.Print` — заменил на `System.Diagnostics.Debug.WriteLine`.
6. `Agent.SetActionChannel(...)` в WoundStaminaComponent — 12 аргументов,
   сигнатура в Bannerlord другая → убран (Fallen-логика через HP/speed сохранена,
   визуал ragdoll — TODO с комментарием).
7. `MissionPeer.Peer.Communicator.ToString()` (PersistenceManager,
   SpectatorBettingBehavior) — нет такого свойства → `Utils/NIPeers.GetPeerId`
   (try/catch + fallback на имя).
8. `frame.rotation.f.AsVec3` в LogTrap — у Quaternion нет поля `f` →
   `frame.rotation * new Vec3(1,0,0)`.
9. `using TaleWorlds.Core;` отсутствовал в 20 файлах, использующих
   `CharacterObject`, `ItemObject`, `Scene`, `GameEntity`, `DestructibleComponent`,
   `ManagedScript` (namespace-раскладка Bannerlord 1.2.x). Добавлен.
   `Vec3` при этом в `TaleWorlds.Library` — проверено по декомпиляции.
10. Двойной вызов killcam: WaveManager и SpectatorBettingBehavior оба обрабатывали
    OnAgentRemoved → убран override в SpectatorBettingBehavior.

### Логические дыры (ядро игрового цикла)
11. Миссия **никогда не заканчивалась**: `State = Failed` + комментарий
    "TODO: End mission" → реализовано отложенное `Mission.Current.EndMission()`
    (победа 25 волн, все мертвы, провал цели).
12. **Цели волн — только сообщения** → реальный спавн: таран (пропс, 2000 HP,
    разрушение = цель выполнена), 3 лагеря (300 HP, поджиг факелом), эскорт
    (крестьянин, смерть = поражение), казна (разрушение = поражение).
    Polling HP в tick — без зависимости от точного API Destructible.
13. **Физ-лут — только сообщение** → `LootManager.SpawnLootBag` реально спавнит
    пропс (`Scene.LoadSceneProp` + vanilla-fallback) с `LootBagUsable`;
    `IsCarryingLoot` в PlayerGoldComponent; казна принимает только несомый мешок.
14. **Стройка — только сообщение** → `FortressBuildManager.Place` реально ставит
    пропс + `BarricadeDestructible`/`StakesTrap`/`BrazierUsable`; экономика:
    личные ресурсы (скраутинг) → общий склад; лимит 40 построек.
15. **Респавн-волны** (`IsRespawnWave`) пропускали проверку поражения → soft-lock
    при смерти всех на 4-й волне → реализован `RespawnAllPlayers()`
    (воскрешение упавших + переспаун мертвых у форта).
16. **Перки — авто-выбор первого** → окно выбора 15 сек: сообщение с 3 перками,
    тайм-аут = рандом, `ChooseForAgent()` готов к Gauntlet-кнопкам
    (ExecuteChoose1-3 в VM).
17. **Караван** проверялся в OnAgentRemoved по позиции *убитого* агента →
    tick-логика: все повозки живы и в радиусе 6м от форта = прибыл;
    все мертвы = разграблен. + звук.
18. **MoraleBehavior.SetTeam(...)** (uncertain API) → убран, паника = -15% speed
    отряда (TODO: Formation.AI.SetBehavior(Flee) после верификации API).

### Новые возможности (в рамках плана)
19. **4 сцены** (`tools/gen_ni_scenes.py`): scene.xscene в формате, сверенном
    с реальными vanilla-сценами (mp_spawnpoint entry points, environment
    properties, border_soft, vanilla пропсы). 65 точек спавна на карту.
20. **`tools/prepare_scenes.py`** — бинарный террейн из vanilla-сцены
    (terrain.bin, flora.bin, ShaderCache, references.txt).
21. **`Audio/NISound.cs`** — 10 звуковых триггеров (мутатор, босс-спавн/фазы,
    last stand старт/конец, победа/поражение, караван, перк). Рефлекссионный
    вызов SoundController: неверный ID/сигнатура = одно предупреждение,
    не падение и не сломанная компиляция. ID в одной таблице.
22. **PerkChoice UI** — ImageWidget-слоты иконок + `IconPathPrefix` в VM
    (арт-задача: 13 иконок 128x128, docs/ART_TASKS.md).
23. **`tools/validate_module.py`** — валидация: XML, SubModule-регистрации,
    полнота сцен (65 спавнов, бинарные файлы), troop/item/prop ID кода vs XML,
    конфиг dedicated-сервера. Текущий статус: **0 ошибок**.
24. **`tools/make_release.py`** — release-зип (source):
    `dist/NiNordInvasion_v2_0_0_source.zip`.
25. **DedicatedCustomServerConfig.xml** — `GameType` был `NordInvasion` (не
    существует) → `Multiplayer` (vanilla-конвейер Custom Battle сервера).

## Что осталось (нужна машина с Bannerlord / человек)

| Задача | Почему не в песочнице |
|---|---|
| terrain.bin для 4 сцен | бинарный, генерируется Scene Editor / prepare_scenes.py |
| Компиляция dll | нужны TaleWorlds.*.dll (закрытые, из установки игры) |
| Кастомные звуки (.ogg) | FMOD-банки, BLSE |
| Мешы ni_*-пропсов, иконки перков | бинарный арт (docs/ART_TASKS.md) |
| Тест Dedicated Server 2 клиента | Windows exe, SteamCMD |
| Upload на NexusMods | аккаунт + собранная dll |

## Риски, честно

- **API Bannerlord менялся между 1.0 и 1.2.x** (namespace Core/Library/Engine).
  Код написан под 1.2.x. Если компиляция на твоей версии даст CS-ошибки —
  правки точечные (using/сигнатуры), rgl_log.txt скажет что именно.
- **ID частиц/звуков** (NIEffects/NISound) — vanilla-названия, проверять по
  логам после первого запуска; обе таблицы в одном месте.
- **Gauntlet-UI не подключен** к пайплайну экранов (PerkManager работает через
  сообщения; VM/команды готовы — следующий код-шаг: ButterLib/UIExtenderEx
  подключение prefab'ов NI_HUD/NI_Shop и т.д.).
- **Мораль: flee** — сейчас только замедление отряда (SetBehavior(Flee) не
  верифицирован против DLL).
