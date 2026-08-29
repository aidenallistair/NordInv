#!/usr/bin/env python3
"""
Валидатор модуля NordInvasion: XML, регистрация в SubModule, сцены, troop/item ID.

Запуск: python3 tools/validate_module.py
Использовать перед каждым релизом и после правок ModuleData.
"""
import glob
import os
import re
import sys
import xml.etree.ElementTree as ET

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MOD = os.path.join(REPO_ROOT, "BannerlordModule", "Modules", "NordInvasion")
SRC = os.path.join(REPO_ROOT, "BannerlordModule", "src", "NordInvasion")

errors, warnings = [], []


def err(m): errors.append(m)
def warn(m): warnings.append(m)


# 1. Все XML валидны
for f in sorted(glob.glob(os.path.join(MOD, "**", "*.xml"), recursive=True)):
    try:
        ET.parse(f)
    except Exception as e:
        err(f"XML {os.path.relpath(f, REPO_ROOT)}: {e}")

# 2. SubModule.xml регистрирует все ModuleData-XML
sub = ET.parse(os.path.join(MOD, "SubModule.xml")).getroot()
registered = {n.find("XmlName").get("path") for n in sub.findall(".//XmlNode") if n.find("XmlName") is not None}
moddata_files = {
    os.path.relpath(f, os.path.join(MOD, "ModuleData")).replace(os.sep, "/")
    for f in glob.glob(os.path.join(MOD, "ModuleData", "*.xml"))
}
for f in sorted(moddata_files):
    if f not in registered:
        err(f"SubModule.xml не регистрирует ModuleData/{f}")

# 3. Scenes: папки из MultiplayerScenes.xml существуют и содержат scene.xscene
scenes_root = os.path.join(MOD, "ModuleData", "Scenes")
mp_scenes = ET.parse(os.path.join(MOD, "ModuleData", "MultiplayerScenes.xml")).getroot()
for s in mp_scenes.findall("Scene"):
    sid = s.get("id")
    folder = os.path.join(scenes_root, sid)
    if not os.path.isdir(folder):
        err(f"Сцена {sid}: папка не найдена (запусти tools/gen_ni_scenes.py)")
        continue
    xscene = os.path.join(folder, "scene.xscene")
    if not os.path.exists(xscene):
        err(f"Сцена {sid}: нет scene.xscene")
        continue
    content = open(xscene, encoding="utf-8").read()
    n_spawns = content.count("mp_spawnpoint") // 2  # name + old_prefab_name
    if n_spawns < 65:
        warn(f"Сцена {sid}: только {n_spawns} entry points (нужно 65: 0-31 игроки, 32-63 норды, 64 босс)")
    for binf in ["terrain.bin", "flora.bin", "ShaderCache"]:
        if not os.path.exists(os.path.join(folder, binf)):
            warn(f"Сцена {sid}: нет {binf} (нужен tools/prepare_scenes.py на машине с игрой)")

# 4. Missions.xml ссылается на существующую сцену
missions = ET.parse(os.path.join(MOD, "ModuleData", "Missions.xml")).getroot()
scene_ids = {s.get("id") for s in mp_scenes.findall("Scene")}
for m in missions.findall("Mission"):
    if m.get("scene") not in scene_ids:
        err(f"Missions.xml: {m.get('id')} -> сцена {m.get('scene')} не в MultiplayerScenes.xml")

# 5. Characters.xml: troop id, которые использует код
chars = open(os.path.join(MOD, "ModuleData", "Characters.xml"), encoding="utf-8").read()
char_ids = set(re.findall(r'<NPCCharacter id="([^"]+)"', chars))
code_refs = set()
for f in glob.glob(os.path.join(SRC, "**", "*.cs"), recursive=True):
    code_refs |= set(re.findall(r'GetObject<CharacterObject>\("([^"]+)"\)', open(f, encoding="utf-8").read()))
for r in sorted(code_refs - char_ids):
    if r in ("swadian_villager",):  # vanilla fallback
        continue
    err(f"Код запрашивает troop {r!r}, которого нет в Characters.xml")

# troop, которые определены, но не используются (не критично)
all_cs = ""
for f in glob.glob(os.path.join(SRC, "**", "*.cs"), recursive=True):
    all_cs += open(f, encoding="utf-8").read()
for cid in sorted(char_ids - code_refs):
    if cid not in all_cs:
        warn(f"Characters.xml: troop {cid!r} не используется кодом (можно оставить)")

# 6. SceneProps.xml: prop id, которые использует код
props = open(os.path.join(MOD, "ModuleData", "SceneProps.xml"), encoding="utf-8").read()
prop_ids = set(re.findall(r'<SceneProp id="([^"]+)"', props))
code_props = set()
for f in glob.glob(os.path.join(SRC, "**", "*.cs"), recursive=True):
    code_props |= set(re.findall(r'SpawnWithFallback\(\s*Mission\.Current\.Scene,\s*"([^"]+)"', open(f, encoding="utf-8").read()))
    code_props |= set(re.findall(r'Spawn\(Mission\.Current\.Scene,\s*"([^"]+)"', open(f, encoding="utf-8").read()))
for r in sorted(code_props - prop_ids):
    err(f"Код спавнит prop {r!r}, которого нет в SceneProps.xml")

# 7. Items.xml: item id из кода
items = open(os.path.join(MOD, "ModuleData", "Items.xml"), encoding="utf-8").read()
item_ids = set(re.findall(r'<Item id="([^"]+)"', items))
code_items = set()
for f in glob.glob(os.path.join(SRC, "**", "*.cs"), recursive=True):
    code_items |= set(re.findall(r'GetObject<ItemObject>\("([^"]+)"\)', open(f, encoding="utf-8").read()))
    code_items |= set(re.findall(r'StringId\s*==\s*"([^"]+)"', open(f, encoding="utf-8").read()))
for r in sorted(code_items - item_ids):
    if r in ("ballista_bolt", "torch"):
        continue  # ballista_bolt - vanilla; torch есть
    if r not in item_ids and r != "torch":
        warn(f"Код упоминает item {r!r} - проверь Items.xml")

# 8. Dedicated server config валиден
dsc = os.path.join(REPO_ROOT, "DedicatedServer", "Bannerlord", "DedicatedCustomServerConfig.xml")
try:
    root = ET.parse(dsc).getroot()
    gt = root.findtext("GameType")
    if gt != "Multiplayer":
        err(f"DedicatedCustomServerConfig.xml: GameType должен быть Multiplayer (сейчас {gt})")
    for m in root.findall(".//Modules/Module"):
        if m.get("Id") == "NordInvasion":
            break
    else:
        err("DedicatedCustomServerConfig.xml: нет <Module Id=NordInvasion>")
except Exception as e:
    err(f"DedicatedCustomServerConfig.xml: {e}")

print("=" * 60)
for e in errors:
    print("ERROR:", e)
for w in warnings:
    print("WARN: ", w)
print("=" * 60)
print(f"Валидация: {len(errors)} ошибок, {len(warnings)} предупреждений")
sys.exit(1 if errors else 0)
