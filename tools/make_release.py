#!/usr/bin/env python3
"""
Сборка релиз-пакета NordInvasion (пункт плана "Релиз zip на NexusMods").

Создаёт dist/NordInvasion_v<VER>_source.zip:
  - Modules/NordInvasion/ (SubModule.xml + ModuleData + сцены)
  - src/ (исходники C#), csproj, build-скрипты
  - docs/ (гайды), tools/
  - RELEASE_NOTES.md
  - BUILD_FROM_SOURCE.md (инструкция: dll не входит, т.к. нужна установка Bannerlord)

Запуск: python3 tools/make_release.py
"""
import os
import re
import shutil
import subprocess
import sys
import zipfile
from datetime import date

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DIST = os.path.join(REPO_ROOT, "dist")

# Версия из SubModule.xml
sub = open(os.path.join(REPO_ROOT, "BannerlordModule", "Modules", "NordInvasion", "SubModule.xml"),
           encoding="utf-8").read()
ver = re.search(r'<Version value="v?([\d.]+)"', sub).group(1)
VER = ver.replace(".", "_")

EXCLUDE_DIRS = {".git", "node_modules", ".venv", "__pycache__", "dist", "bin", "obj"}
EXCLUDE_EXT = {".dll", ".exe", ".pdb", ".db"}


def walk(include_root: str, rel_prefix: str, zf: zipfile.ZipFile):
    for dirpath, dirnames, filenames in os.walk(include_root):
        dirnames[:] = [d for d in dirnames if d not in EXCLUDE_DIRS]
        for fn in filenames:
            if os.path.splitext(fn)[1].lower() in EXCLUDE_EXT:
                continue
            full = os.path.join(dirpath, fn)
            arc = os.path.join(rel_prefix, os.path.relpath(full, include_root))
            zf.write(full, arc)


def main() -> None:
    os.makedirs(DIST, exist_ok=True)
    out = os.path.join(DIST, f"NiNordInvasion_v{VER}_source.zip")
    if os.path.exists(out):
        os.remove(out)

    prefix = f"NiNordInvasion_v{VER}"
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as zf:
        # Модуль (что кладётся в игру)
        mod_src = os.path.join(REPO_ROOT, "BannerlordModule", "Modules", "NordInvasion")
        walk(mod_src, f"{prefix}/Modules/NordInvasion", zf)
        # Исходники + csproj + build
        for f in ["NordInvasion.csproj", "build_module.bat", "build_module.sh", "README.md"]:
            p = os.path.join(REPO_ROOT, "BannerlordModule", f)
            if os.path.exists(p):
                zf.write(p, f"{prefix}/src-build/{f}")
        walk(os.path.join(REPO_ROOT, "BannerlordModule", "src", "NordInvasion"),
             f"{prefix}/src-build/src/NordInvasion", zf)
        # Гайды и инструменты
        walk(os.path.join(REPO_ROOT, "docs"), f"{prefix}/docs", zf)
        for f in os.listdir(os.path.join(REPO_ROOT, "tools")):
            zf.write(os.path.join(REPO_ROOT, "tools", f), f"{prefix}/tools/{f}")
        # Backend
        for f in os.listdir(os.path.join(REPO_ROOT, "src", "backend")):
            zf.write(os.path.join(REPO_ROOT, "src", "backend", f), f"{prefix}/backend/{f}")
        # Dedicated server
        walk(os.path.join(REPO_ROOT, "DedicatedServer"), f"{prefix}/dedicated-server", zf)
        # Релиз-ноты
        zf.write(os.path.join(REPO_ROOT, "RELEASE_NOTES.md"), f"{prefix}/RELEASE_NOTES.md")
        zf.write(os.path.join(REPO_ROOT, "BUILD_FROM_SOURCE.md"), f"{prefix}/BUILD_FROM_SOURCE.md")

    size = os.path.getsize(out)
    print(f"Готово: {out} ({size // 1024} KB)")


if __name__ == "__main__":
    main()
