#!/usr/bin/env python3
"""
Доукомплектация сцен Nord Invasion бинарным террейном (продолжение пункта "4 карты").

scene.xscene - это XML сущностей (entry points + пропсы). Террейн (terrain.bin,
flora.bin, ShaderCache, references.txt) - бинарные данные, которые генерирует
Bannerlord Scene Editor. Этот скрипт копирует их из vanilla-сцены, чтобы
наши карты сразу загрузились с нормальным полом.

Использование (на машине с Bannerlord):
    python3 tools/prepare_scenes.py
    python3 tools/prepare_scenes.py --bannerlord "D:\\Games\\Bannerlord" --source mp_ye_battle_01

Что делает:
1. Находит (или принимает --bannerlord) корень установки Bannerlord.
2. Для каждой папки ModuleData/Scenes/mp_ni_*/ копирует из
   Modules/Native/ModuleData/Scenes/<source>/:
     - terrain.bin, flora.bin, references.txt
     - ShaderCache/ (рекурсивно)
3. Печатает чек-лист следующих шагов.
"""
import argparse
import os
import shutil
import sys
import glob

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCENES_ROOT = os.path.join(
    REPO_ROOT, "BannerlordModule", "Modules", "NordInvasion", "ModuleData", "Scenes"
)

CANDIDATES = [
    os.path.expandvars(r"%ProgramFiles(x86)%\Steam\steamapps\common\Mount & Blade II Bannerlord"),
    os.path.expanduser("~/steamapps/common/Mount & Blade II Bannerlord"),
    os.path.expanduser("~/.steam/steam/steamapps/common/Mount & Blade II Bannerlord"),
]

COPIED = ["terrain.bin", "flora.bin", "references.txt"]


def find_bannerlord() -> str:
    for c in CANDIDATES:
        if os.path.isdir(os.path.join(c, "Modules", "Native")):
            return c
    return ""


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--bannerlord", default="")
    ap.add_argument("--source", default="mp_ye_battle_01",
                    help="vanilla mp-сцена для террейна (mp_ye_battle_01, mp_scn_north... )")
    args = ap.parse_args()

    bl = args.bannerlord or find_bannerlord()
    if not bl or not os.path.isdir(os.path.join(bl, "Modules", "Native")):
        print("Bannerlord не найден. Укажи путь: --bannerlord <путь к игре>")
        return 1

    src_dir = os.path.join(bl, "Modules", "Native", "ModuleData", "Scenes", args.source)
    if not os.path.isdir(src_dir):
        avail = ", ".join(os.listdir(os.path.join(bl, "Modules", "Native", "ModuleData", "Scenes")))
        print(f"Vanilla-сцена {args.source} не найдена. Доступны: {avail}")
        return 1

    missing_src = [f for f in COPIED if not os.path.exists(os.path.join(src_dir, f))]
    if missing_src:
        print(f"ВНИМАНИЕ: в {src_dir} нет: {missing_src}")

    if not os.path.isdir(SCENES_ROOT):
        print("Сначала сгенерируй сцены: python3 tools/gen_ni_scenes.py")
        return 1

    for scene_folder in sorted(glob.glob(os.path.join(SCENES_ROOT, "mp_ni_*"))):
        scene_id = os.path.basename(scene_folder)
        ok, missing = [], []
        for f in COPIED:
            s = os.path.join(src_dir, f)
            if os.path.exists(s):
                shutil.copy2(s, os.path.join(scene_folder, f))
                ok.append(f)
            else:
                missing.append(f)
        # ShaderCache - рекурсивно
        src_sh = os.path.join(src_dir, "ShaderCache")
        if os.path.isdir(src_sh):
            dst_sh = os.path.join(scene_folder, "ShaderCache")
            if os.path.isdir(dst_sh):
                shutil.rmtree(dst_sh)
            shutil.copytree(src_sh, dst_sh)
            ok.append("ShaderCache/")
        else:
            missing.append("ShaderCache/")

        status = "OK" if not missing else "PARTIAL"
        print(f"[{status}] {scene_id}: +{', '.join(ok)}" + (f"  НЕ ХВАТАЕТ: {', '.join(missing)}" if missing else ""))

    print("""
Дальше:
1. Собери модуль (build_module.bat / .sh) - это скопирует ModuleData в игру.
2. (Рекомендуется) Открой каждую сцену в Bannerlord Scene Editor:
   Launcher -> Tools -> Editor -> open mp_ni_* -> Save.
   Это перегенерирует navmesh и проверит, что все prefab'ы валидны.
3. Custom Battle -> сцена mp_ni_bridge_01 -> миссия mp_nord_invasion -> Start.
   Должны заспавниться волна из ~12 ботов через 8 секунд.
4. Если сцена падает при загрузке - посмотри rgl_log.txt:
   обычно там имя пропса, которого нет в твоей версии (поправить в gen_ni_scenes.py).
""")
    return 0


if __name__ == "__main__":
    sys.exit(main())
