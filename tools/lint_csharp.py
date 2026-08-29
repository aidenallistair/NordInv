#!/usr/bin/env python3
"""
Линтер C# мода NordInvasion: статические проверки, доступные без Bannerlord.

Скомпилировать мод в песочнице нельзя (нужны TaleWorlds.*.dll из установки игры),
но большую часть ошибок такого рода ловит статический анализ:

  1. баланс () {} [] по файлу со строками/комментариями, вырезанными токенайзером
     (обычный "посчитать скобки" даёт ложные срабатывания на скобках в комментариях);
  2. required usings: типы живут в конкретных namespace-ах TaleWorlds -
     нет using => CS0246 "не найден тип";
  3. override-ы MissionBehavior/AgentComponent против списка виртуальных методов:
     опечатка в сигнатуре = "new override" или CS0115 (только предупреждение:
     список сверяется с DLL 1.0.3, в 1.2.x состав виртуалов мог измениться);
  4. контракт бэкенда: все "/api/..." из C# есть в PHP (index.php) и в Python (nidb.py);
  5. каталог магазина: встроенная таблица C# == shop_catalog.json (ids, цены, grants);
  6. все классы-поведения объявлены и зарегистрированы в SubModule.cs;
  7. сводка по заглушкам (TODO/закомментированные вызовы API).

Запуск: python3 tools/lint_csharp.py        (0 = чисто, 1 = есть ошибки)
Внутри validate_module.py вызывается автоматически.
"""

from __future__ import annotations

import json
import os
import re
import sys

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(REPO, "BannerlordModule", "src", "NordInvasion")
MOD = os.path.join(REPO, "BannerlordModule", "Modules", "NordInvasion")
PHP_INDEX = os.path.join(REPO, "src", "backend-php", "index.php")
PY_CORE = os.path.join(REPO, "src", "backend", "nidb.py")
CATALOG = os.path.join(REPO, "src", "backend-php", "shop_catalog.json")

errors: list[str] = []
warnings: list[str] = []
info: list[str] = []


def err(msg: str) -> None:
    errors.append(msg)


def warn(msg: str) -> None:
    warnings.append(msg)


# --------------------------------------------------------------------- токенайзер

def strip_code(src: str) -> str:
    """Убирает строки, символы, verbatim-строки и комментарии (баланс скобок честный)."""
    out: list[str] = []
    i, n = 0, len(src)
    state = None  # None | 'str' | 'verbatim' | 'chr' | 'line' | 'block'
    while i < n:
        c = src[i]
        nxt = src[i + 1:i + 2]
        if state is None:
            if c == "/" and nxt == "/":
                state = "line"; i += 2; continue
            if c == "/" and nxt == "*":
                state = "block"; i += 2; continue
            if c == "@" and nxt == '"':
                state = "verbatim"; i += 2; continue
            if c == '"':
                state = "str"; i += 1; out.append(" "); continue
            if c == "'":
                state = "chr"; i += 1; continue
            out.append(c); i += 1; continue
        if state == "line":
            if c == "\n":
                state = None; out.append("\n")
            i += 1; continue
        if state == "block":
            if c == "*" and nxt == "/":
                state = None; i += 2; continue
            out.append("\n" if c == "\n" else " ")
            i += 1; continue
        if state == "verbatim":
            if c == '"' and nxt == '"':
                i += 2; continue
            if c == '"':
                state = None
            i += 1; continue
        if state == "str":
            if c == "\\":
                i += 2; continue
            if c == '"':
                state = None
            i += 1; continue
        if state == "chr":
            if c == "\\":
                i += 2; continue
            if c == "'":
                state = None
            i += 1; continue
    return "".join(out)


# ------------------------------------------------------------- 1) баланс скобок

def check_balance(files: dict[str, str]) -> None:
    for rel, src in files.items():
        code = strip_code(src)
        for open_c, close_c, label in (("{", "}", "фигурные"), ("(", ")", "круглые"), ("[", "]", "квадратные")):
            diff = code.count(open_c) - code.count(close_c)
            if diff != 0:
                err(f"{rel}: разбаланс {label}: {open_c}{code.count(open_c)} vs {close_c}{code.count(close_c)} ({diff:+d})")
        # строка не должна обрываться на середине (незакрытая строка = съеденный код)
        if code.count('"') % 2:
            warn(f"{rel}: нечётное число кавычек после вычитания строк - проверь экранирование")


# ------------------------------------------------------------- 2) required usings

NEEDS_USING = {
    "TaleWorlds.Core": ["CharacterObject", "ItemObject", "ItemModifier", "EquipmentIndex", "BasicCharacterObject",
                        "WeaponComponent", "ItemUsageCategory", "DestructibleComponent", "GameEntity", "Scene",
                        "AgentIndex", "BattleSideEnum", "Equipment"],
    "TaleWorlds.Library": ["Vec3", "Vec2", "MatrixFrame", "Frame", "InformationManager", "InformationMessage",
                           "Colors", "ViewModel", "DataSourceProperty", "MBRandom", "Utility", "MathHelper", "Mission"],
    "TaleWorlds.MountAndBlade": ["MissionBehavior", "Agent", "AgentComponent", "AgentBuildData", "Mission",
                                 "Team", "Formation", "MBSubModuleBase", "UsableMachine", "ManagedScript",
                                 "MissionLog", "FormationClass", "KillingBlow", "ScriptedMissionBehavior"],
    "TaleWorlds.ObjectSystem": ["MBObjectManager"],
}


def check_usings(files: dict[str, str]) -> None:
    for rel, src in files.items():
        if "Utils/NIJson.cs" in rel or "NIMath" in rel:
            continue
        usings = set(re.findall(r"^\s*using\s+([\w.]+)\s*;", src, re.M))
        code = strip_code(src)
        for ns, types in NEEDS_USING.items():
            used = [t for t in types if re.search(r"\b" + re.escape(t) + r"\b", code)]
            if not used:
                continue
            if ns in usings or f"global::{ns}" in usings:
                continue
            # тип может быть полностью квалифицирован (TaleWorlds.Core.Xxx) - тогда using не нужен
            qualified = any(re.search(re.escape(ns) + r"\." + re.escape(t) + r"\b", code) for t in used)
            if qualified and len(used) == 1:
                continue
            warn(f"{rel}: используются {', '.join(sorted(used)[:4])} - нужен using {ns};")


# --------------------------------------------------- 3) override-ы vs известные виртуалы

KNOWN_VIRTUALS = {
    "MissionBehavior": {
        "OnBehaviorInitialize", "OnMissionInitialize", "OnMissionStart", "OnMissionEnd", "OnMissionTick",
        "OnAgentBuild", "OnAgentRemoved", "OnAgentHit", "OnAgentKilled", "OnAgentHealthChanged",
        "OnPlayerAgentChange", "OnPlayerLogin", "OnPlayerDisconnect", "OnEncounter", "OnUpdateAreaOfInterest",
        "OnDeploymentStart", "OnMissionTimeChanged", "OnAgentCheckedCollision", "OnWeaponEquip",
        "OnCharacterObjectScoreGet", "OnAgentFleeing", "OnAgentBecomingDamaged", "OnMissionKeyChange",
        "OnKeyDown", "OnKeyUp", "OnGameKeyChanged2", "OnReceivedChatMessage", "OnPlayerRequestSpawnAgent",
        "OnInitialAddMissionBehaviorsToMission", "OnEndMission", "OnEnterTiles", "OnUpdateMenuTypes",
        "OnTeamAdded", "OnTeamRemoved", "OnSpawnPointChanged", "OnApplyScenePieceVisibilityToAgent",
    },
    "AgentComponent": {"OnTick", "OnAgentMoved", "OnAgentHit", "OnAgentKilled", "OnAgentRemoved", "OnEquippingItem",
                       "OnUnEquippingItem", "OnAgentWeaponUnwielded", "OnCurrentDefaultActionUpdated", "OnInit",
                       "OnTaskStarted", "OnTaskStopped"},
    "MBSubModuleBase": {"OnSubModuleLoad", "OnSubModuleUnloaded", "OnBeforeInitialModuleScreenSetAsRoot",
                        "InitializeSubModule", "OnGameStart", "DoApplicationTick", "OnGameEnd",
                        "OnGameKeyChanged", "OnMissionBehaviorInitialize", "OnApplicationTick", "DeclareBindings",
                        "OnInitialModuleScreenSetAsRoot"},
    "DestructibleComponent": {"OnInit", "OnTick", "OnHit", "OnDamage", "OnDestroy", "OnBreak"},
    "UsableMachine": {"OnUse", "OnUseStarted", "OnUseStopped", "OnTick", "OnInit", "OnUsageAvailabilityChanged"},
    "ManagedScript": {"OnInit", "OnRelease", "OnTick", "OnDraw", "OnScriptTickClient"},
}
# Виртуалы, которых нет в справочнике 1.0.3, но которые код использует осознанно:
# это не опечатки, а API, требующий подтверждения по DLL конкретной версии игры.
UNCERTAIN_VIRTUALS = {
    "AgentComponent": {"OnTickAsAI", "OnTickBeforeAgents"},
}

BASE_OF = {
    "MissionBehavior": "MissionBehavior", "AgentComponent": "AgentComponent", "MBSubModuleBase": "MBSubModuleBase",
    "DestructibleComponent": "DestructibleComponent", "UsableMachine": "UsableMachine",
    "BaseViewModelMixin": "ManagedScript", "ManagedScript": "ManagedScript",
}


def check_overrides(files: dict[str, str]) -> None:
    uncertain: dict[str, list[str]] = {}
    for rel, src in files.items():
        code = strip_code(src)
        for m in re.finditer(r"class\s+(\w+)\s*:\s*([A-Za-z_][\w.]*)", code):
            cls, base = m.group(1), m.group(2).split(".")[-1]
            known = KNOWN_VIRTUALS.get(BASE_OF.get(base, ""))
            if not known:
                continue
            body_start = code.find("{", m.end())
            if body_start < 0:
                continue
            depth, i = 0, body_start
            while i < len(code):
                if code[i] == "{":
                    depth += 1
                elif code[i] == "}":
                    depth -= 1
                    if depth == 0:
                        break
                i += 1
            body = code[body_start:i]
            for ov in re.finditer(r"\boverride\s+(?:public\s+|private\s+|protected\s+|static\s+|async\s+)*[\w<>\[\],.?\s]+?\s(\w+)\s*\(", body):
                name = ov.group(1)
                if name in known:
                    continue
                if name in UNCERTAIN_VIRTUALS.get(BASE_OF.get(base, ""), set()):
                    uncertain.setdefault(f"{name}() : {base}", []).append(cls)
                    continue
                err(f"{rel}: {cls} : {base} - override {name}() не является виртуальным методом базы "
                    f"(CS0115; проверь опечатку в сигнатуре)")
    for sig, users in sorted(uncertain.items()):
        warn(f"сигнатура {sig} не подтверждена по справочнику 1.0.3, но используется в "
             f"{len(users)} классах ({', '.join(sorted(set(users))[:3])}...) - проверить по DLL "
             f"перед релизом: если это не override, механика молчит")


# ------------------------------------------------------------------ 4) контракт API

def php_routes() -> set[str]:
    if not os.path.isfile(PHP_INDEX):
        return set()
    text = open(PHP_INDEX, encoding="utf-8").read()
    routes = set(re.findall(r"\$p === '([^']+)'", text))
    routes.add("player/{id}")
    return routes


def python_routes() -> set[str]:
    if not os.path.isfile(PY_CORE):
        return set()
    text = open(PY_CORE, encoding="utf-8").read()
    return set(re.findall(r'p == "([^"]+)"', text)) | {p for p in re.findall(r'r"player/\(\[\\w\.\\-\]\+\)"', text) and ["player/{id}"]}


def check_backend_contract(files: dict[str, str]) -> None:
    all_code = "\n".join(files.values())
    called = set(re.findall(r'(?:PostForm|GetText)\("(/api/[\w/{}.\-]+)"', all_code))
    called |= set(re.findall(r'GetText\("(/api/[\w/{}.\-]+)', all_code))
    if not called:
        warn("C#: ни одного вызова бэкенда не найдено (PostForm/GetText) - проверка контракта пропущена")
        return
    phpr, pyr = php_routes(), python_routes()
    for raw in sorted(called):
        route = raw[len("/api/"):]
        qs = route.find("?")
        if qs >= 0:
            route = route[:qs]
        if route == "player/login":
            continue
        if phpr and route not in phpr:
            err(f"контракт: C# зовёт /api/{route}, а в PHP index.php такого маршрута нет")
        if pyr and route not in pyr and route != "player/{id}":
            err(f"контракт: C# зовёт /api/{route}, а в src/backend/nidb.py его нет")
    info.append(f"маршрутов, вызываемых из C#: {len(called)}")


# --------------------------------------------------------------- 5) каталог магазина

def shop_json_ids() -> dict[str, dict] | None:
    if not os.path.isfile(CATALOG):
        return None
    with open(CATALOG, encoding="utf-8") as fh:
        data = json.load(fh)
    return {i["id"]: i for i in data.get("items", [])}


def check_shop_catalog(files: dict[str, str]) -> None:
    rel = "Models/ShopCatalog.cs"
    src = None
    for k, v in files.items():
        if k.endswith("ShopCatalog.cs"):
            src, rel = v, k
            break
    if src is None:
        err("нет Models/ShopCatalog.cs (встроенный fallback каталога)")
        return
    json_items = shop_json_ids()
    if json_items is None:
        warn(f"нет {os.path.relpath(CATALOG, REPO)} - сравнение каталога пропущено")
        return

    # парсим C#-таблицу: new ShopItem { Id = "x", ..., Gold = 60, Wood = 0, Metal = 0, Grants = new[] {"wood:10"} }
    code = strip_code(src)
    cs_items: dict[str, dict] = {}
    for m in re.finditer(r"new ShopItem \{(.{0,400}?)\},\n", src, re.S):
        body = m.group(1)
        cid = re.search(r'Id\s*=\s*"([^"]+)"', body)
        if not cid:
            continue
        entry = {
            "gold": int(re.search(r"Gold\s*=\s*(\d+)", body).group(1)) if re.search(r"Gold\s*=\s*(\d+)", body) else 0,
            "wood": int(re.search(r"Wood\s*=\s*(\d+)", body).group(1)) if re.search(r"Wood\s*=\s*(\d+)", body) else 0,
            "metal": int(re.search(r"Metal\s*=\s*(\d+)", body).group(1)) if re.search(r"Metal\s*=\s*(\d+)", body) else 0,
            "grants": re.findall(r'"([^"]+)"', re.search(r"Grants = new\[\]\s*\{([^}]*)\}", body).group(1))
            if re.search(r"Grants = new\[\]\s*\{([^}]*)\}", body) else [],
        }
        cs_items[cid.group(1)] = entry

    if not cs_items:
        err(f"{rel}: не удалось разобрать таблицу каталога (изменился формат?)")
        return
    missing = set(json_items) - set(cs_items)
    extra = set(cs_items) - set(json_items)
    if missing:
        err(f"{rel}: в C# нет позиций каталога {sorted(missing)} (есть в shop_catalog.json)")
    if extra:
        err(f"{rel}: в C# есть позиции {sorted(extra)}, которых нет в shop_catalog.json")
    for iid, js in json_items.items():
        cs = cs_items.get(iid)
        if not cs:
            continue
        for key, ckey in (("gold", "gold"), ("wood", "wood"), ("metal", "metal")):
            if int(js.get(key, 0)) != cs[ckey]:
                err(f"{rel}: {iid}.{key}: C# {cs[ckey]} != JSON {int(js.get(key, 0))}")
        if [str(g) for g in js.get("grants", [])] != cs["grants"]:
            err(f"{rel}: {iid}.grants: C# {cs['grants']} != JSON {js.get('grants')}")
    info.append(f"каталог магазина сверен: {len(json_items)} позиций")

    # blueprint-гейм: каждый blueprint-id из каталога должен быть в allowlist и в SceneProps
    allow = set(json.load(open(CATALOG, encoding="utf-8")).get("blueprint_ids", []))
    props = open(os.path.join(MOD, "ModuleData", "SceneProps.xml"), encoding="utf-8").read()
    prop_ids = set(re.findall(r'<SceneProp id="([^"]+)"', props))
    build_src = files.get("Managers/FortressBuildManager.cs", "")
    for bp in sorted(allow):
        prop = "ni_" + bp if ("ni_" + bp) in prop_ids else None
        if prop and prop not in build_src:
            warn(f"FortressBuildManager: чертёж {bp} открывает проп {prop}, но Place для него не вызывается")


# ------------------------------------------------------- 6) регистрация поведений

def check_behavior_registration(files: dict[str, str]) -> None:
    all_code = "\n".join(files.values())
    sub = files.get("SubModule.cs", "")
    declared = set(re.findall(r"class\s+(\w+Behavior|\w+Manager)\s*:\s*MissionBehavior", all_code))
    registered = set(re.findall(r"AddMissionBehavior\(new ([\w.]+)\(", sub))
    # PerksManager/LootManager и т.п. могут вызываться через GetMissionBehavior - но обязаны быть зарегистрированы
    for cls in sorted(declared):
        if cls in ("SubModule",):
            continue
        if not any(cls in r for r in registered):
            warn(f"{cls} : MissionBehavior не зарегистрирован в SubModule.cs (GetMissionBehavior вернёт null)")
    for cls in sorted(registered):
        simple = cls.split(".")[-1]
        if simple not in declared and not re.search(r"class\s+" + re.escape(simple) + r"\b", all_code):
            err(f"SubModule.cs регистрирует {cls}, которого нет в исходниках")


# --------------------------------------------------------------- 7) заглушки

def check_stubs(files: dict[str, str]) -> None:
    todos = 0
    for rel, src in files.items():
        for line in src.split("\n"):
            if re.search(r"TODO|заглушк|placeholder", line, re.I):
                todos += 1
    info.append(f"TODO/заглушек в коде: {todos} (см. docs/PROGRESS.md)")


# ------------------------------------------------------------------------- main

def load_files() -> dict[str, str]:
    out: dict[str, str] = {}
    for dirpath, dirnames, filenames in os.walk(SRC):
        dirnames[:] = [d for d in dirnames if d not in ("bin", "obj")]
        for fn in filenames:
            if fn.endswith(".cs"):
                full = os.path.join(dirpath, fn)
                out[os.path.relpath(full, SRC).replace(os.sep, "/")] = open(full, encoding="utf-8").read()
    return out


def run(files: dict[str, str] | None = None) -> tuple[list[str], list[str], list[str]]:
    files = files if files is not None else load_files()
    if not files:
        err(f"нет C-файлов в {SRC}")
        return errors, warnings, info
    check_balance(files)
    check_usings(files)
    check_overrides(files)
    check_backend_contract(files)
    check_shop_catalog(files)
    check_behavior_registration(files)
    check_stubs(files)
    return errors, warnings, info


def main() -> int:
    files = load_files()
    run(files)
    print("=" * 60)
    print(f"C#-файлов: {len(files)}")
    for i in info:
        print("INFO: ", i)
    for w in warnings:
        print("WARN: ", w)
    for e in errors:
        print("ERROR:", e)
    print("=" * 60)
    print(f"Линтер: {len(errors)} ошибок, {len(warnings)} предупреждений")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
