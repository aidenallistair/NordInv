# NordInv - Nord Invasion Better Edition

Реализация мультиплеерного режима Nord Invasion в стиле Fianna.ru, но лучше.

## Ветки

- **Warband Edition** (оригинал Fianna): `ModuleSystem/` + `ServerConfig/` + `docs/FIANNA_GUIDE_RU.md`
  - Python Module System 1.174 + WSE
  - Готовый скелет мода с волнами, магазином, баррикадами
  - Инструкция по поднятию dedicated сервера

- **Better Edition (15 улучшений)**: `ModuleSystem/` (обновлен) + `docs/BETTER_EDITION.md` + `docs/IMPLEMENTATION_PLAN.md`
  - 15 механик: перки, форт-конструктор, роли, цели волн, AI-директор, погода, кавалерия, физ-лут, скавенджинг, мутаторы, отряды с формациями, персистенция 2.0, ранения, разрушаемость, глобальная кампания
  - Backend FastAPI в `src/backend/`

- **Bannerlord Edition (NEW)**: `BannerlordModule/` + `docs/BANNERLORD_PLAN_RU.md`
  - Порт на Bannerlord C# + Harmony
  - Архитектура: MissionBehaviors, AgentComponents, DestructibleComponents, Gauntlet UI
  - Поддержка Co-op и Dedicated MP
  - Тот же backend

## Быстрый старт

### Warband Fianna (как было)

Смотри `docs/QUICKSTART.md` и `docs/FIANNA_GUIDE_RU.md`

### Better Edition Warband

Смотри `docs/BETTER_EDITION.md` и `docs/IMPLEMENTATION_PLAN.md` - там план реализации всех 15 механик.

### Bannerlord Better Edition (рекомендуемый путь)

Смотри `docs/BANNERLORD_PLAN_RU.md` - полный план под Bannerlord с кодом.

Скелет C# проекта уже в `BannerlordModule/`:
- `SubModule.cs` - точка входа
- `Behaviors/NordInvasionWaveManagerBehavior.cs` - ядро волн с директором, мутаторами, погодой
- `Managers/` - стройка, отряды, лут, персистенция
- `Components/` - медик, ранения, перки

Скомпилируй в `Modules/NordInvasion/bin/`.

## Dedicated Server Files

Я не смог скачать `mb_warband_dedicated_1174.zip` и Bannerlord Dedicated Server в этом окружении - TaleWorlds CDN блокирует запросы из sandbox (SSL_ERROR_SYSCALL).

**Что делать:**
1. Скачай вручную:
   - Warband: https://www.taleworlds.com/en/Games/Warband/Download (Other Downloads -> Dedicated Server) или прямая ссылка `https://download.taleworlds.com/mb_warband_dedicated_1174.zip` (91 MB)
   - Bannerlord: SteamCMD `app_update 1058080` или Steam Tools -> Mount & Blade II Dedicated Server
2. Закинь в папки:
   - `DedicatedServer/Warband/` - для Warband
   - `DedicatedServer/Bannerlord/` - для Bannerlord
3. Инструкции в `DedicatedServer/Bannerlord/README.md` и `ServerConfig/`

Если загрузишь файлы, я добавлю их в репо (используй Git LFS для больших бинарников).

## Backend

```bash
cd src/backend
pip install -r requirements.txt
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

Поддерживает: игроков, золото, чертежи, деревни (кампания), сезоны, battlepass, лидерборд.

## Документация

- `docs/FIANNA_GUIDE_RU.md` - как поднять Fianna сервер
- `docs/QUICKSTART.md` - за 15 минут
- `docs/WSE_INTEGRATION.md` - WSE + HTTP
- `docs/MAPS_GUIDE.md` - карты
- `docs/IMPLEMENTATION_PLAN.md` - план 15 механик для Warband
- `docs/BETTER_EDITION.md` - что реализовано в Better
- `docs/BANNERLORD_PLAN_RU.md` - план под Bannerlord (главный)

Удачной обороны Свадии!
