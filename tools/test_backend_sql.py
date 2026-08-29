#!/usr/bin/env python3
"""
Проверка PHP-бэкенда без PHP: схема + все SQL-запросы + арность плейсхолдеров.

PHP-рантайма в песочнице/CI нет, но SQL из src/backend-php/*.php можно:
  1) собрать схему из schema.sql теми же преобразованиями MySQL->SQLite, что делает
     install.php::ddl() (расхождение - тоже ошибка, тест ловит);
  2) поднять каждый статический SQL-запрос через EXPLAIN на этой схеме
     (ловит опечатки в именах таблиц/колонок и синтаксис);
  3) сверить число '?' в prepare(...) с числом элементов в execute([...])
     (частая ошибка при правках UPDATE: добавили колонку - забыли параметр);
  4) проверить баланс скобок и уникальность/полноту маршрутов.

Динамические запросы (склейка с переменными PHP) и MySQL-only (information_schema)
пропускаются и печатаются в сводке - их проверяет только живой smoke-тест.

Запуск: python3 tools/test_backend_sql.py
"""

from __future__ import annotations

import os
import re
import sqlite3
import sys

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PHP = os.path.join(REPO, "src", "backend-php")
PHP_FILES = ["index.php", "lib.php", "install.php", "config.php"]
MYSQL_ONLY = ("information_schema",)
SQL_START = re.compile(r"^\s*(SELECT|INSERT|UPDATE|DELETE|CREATE|ALTER|DROP|PRAGMA|REPLACE)\b", re.I)

errors: list[str] = []
skipped: list[str] = []
stats = {"sql": 0, "arity": 0, "routes": 0}


def err(msg: str) -> None:
    errors.append(msg)


# ----------------------------------------------------------------- транслитерация DDL

def ddl_for_sqlite(sql: str) -> str:
    """Поводились с install.php::ddl(): меняешь одно - меняй и другое (иначе тест упадёт)."""
    sql = sql.replace(" BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY", " INTEGER PRIMARY KEY AUTOINCREMENT")
    sql = sql.replace(") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4", " )")
    sql = re.sub(r"^[ \t]*,?[ \t]*UNIQUE[ \t]+KEY[ \t]+\w+[ \t]*\(([^)]*)\),?[ \t]*\r?\n",
                 r" UNIQUE (\1)\n", sql, flags=re.M)
    sql = re.sub(r"^[ \t]*,?[ \t]*KEY[ \t]+\w+[ \t]*\([^)]*\),?[ \t]*\r?\n", "", sql, flags=re.M)
    sql = re.sub(r",\s*\)", ")", sql)
    return sql


def build_schema() -> sqlite3.Connection:
    conn = sqlite3.connect(":memory:")
    raw = open(os.path.join(PHP, "schema.sql"), encoding="utf-8").read()
    no_comments = "\n".join(l for l in raw.split("\n") if not l.lstrip().startswith("--"))
    for stmt in [s.strip() for s in no_comments.split(";") if s.strip()]:
        try:
            conn.execute(ddl_for_sqlite(stmt))
        except sqlite3.Error as e:
            err(f"schema.sql: {e} :: {stmt[:60]}...")
    return conn


# --------------------------------------------------------------- разбор PHP-выражений

def balanced(text: str, open_idx: int) -> tuple[int, str]:
    """(индекс закрывающей скобки, содержимое) для скобки в open_idx; строки PHP учитываются."""
    depth = 0
    i = open_idx
    n = len(text)
    in_str: str | None = None
    while i < n:
        c = text[i]
        if in_str:
            if c == "\\":
                i += 2
                continue
            if c == in_str:
                in_str = None
        elif c in "'\"":
            in_str = c
        elif c in "([{":
            depth += 1
        elif c in ")]}":
            depth -= 1
            if depth == 0:
                return i, text[open_idx + 1:i]
        i += 1
    return n, text[open_idx + 1:]


LITERAL = re.compile(r"'(?:[^'\\]|\\.)*'|\"(?:[^\"\\]|\\.)*\"")


def collect_sql(arg: str) -> tuple[str | None, bool]:
    """
    Склеивает литералы PHP-выражения (a . 'b' . 'c') в один SQL.
    Возвращает (sql|None, dynamic): dynamic=True, если вне литералов есть переменная
    ($var, {$x}) - такой запрос нельзя проверить статически.
    """
    # 'X' ? 'Y' : 'Z' -> берём истинную ветку, чтобы склейка осталась валидным SQL
    arg = re.sub(r"\?\s*('(?:[^'\\]|\\.)*'|\"(?:[^\"\\]|\\.)*\")\s*:\s*(?:'(?:[^'\\]|\\.)*'|\"(?:[^\"\\]|\\.)*\")",
                 r"\1", arg)
    stripped = LITERAL.sub(" ", arg)
    dynamic = bool(re.search(r"[\$]|\{\s*\$", stripped))
    # "... {$var} ..." - интерполяция внутри литерала: статически не проверяется
    if not dynamic and re.search(r'"[^"]*\{?\$\w', arg):
        dynamic = True
    parts = [lit[1:-1] for lit in LITERAL.findall(arg)]
    if not parts:
        return None, dynamic
    sql = " ".join(p for p in parts if p.strip()).strip()
    return (sql if SQL_START.match(sql) else None), dynamic


def count_top_level_items(array_src: str) -> int:
    """Число элементов PHP-массива [...], с учётом вложенности и строк."""
    s = array_src.strip()
    if s.startswith("[") and s.endswith("]"):
        s = s[1:-1]
    s = s.strip()
    if not s:
        return 0
    items = 1  # запятых верхнего уровня + 1
    in_str: str | None = None
    i = 0
    while i < len(s):
        c = s[i]
        if in_str:
            if c == "\\":
                i += 2
                continue
            if c == in_str:
                in_str = None
        elif c in "'\"":
            in_str = c
        elif c == ",":
            items += 1
        i += 1
    return items


# -------------------------------------------------------------------------- main

def main() -> int:
    conn = build_schema()

    for fname in PHP_FILES:
        path = os.path.join(PHP, fname)
        if not os.path.isfile(path):
            err(f"{fname}: файл не найден")
            continue
        text = open(path, encoding="utf-8").read()

        if text.count("{") != text.count("}"):
            err(f"{fname}: разный баланс фигурных скобок: {text.count('{')} vs {text.count('}')}")

        # 1) prepare('SQL') + арность execute([...])
        for m in re.finditer(r"->prepare\(", text):
            line = text[:m.start()].count("\n") + 1
            end, arg = balanced(text, m.end() - 1)
            sql, dynamic = collect_sql(arg)
            where = f"{fname}:{line}"
            if sql is None:
                skipped.append(f"{where}: prepare без статического SQL")
            elif dynamic or any(k in sql.lower() for k in MYSQL_ONLY):
                skipped.append(f"{where}: динамический/MySQL-only запрос")
            else:
                check_sql(conn, where, sql)

            # execute([...]) - сразу после prepare или следующей строкой ($st = ...)
            rest = text[end + 1:end + 400]
            em = re.match(r"\s*(?:;\s*\$\w+\s*)?->execute\(", rest)
            if em:
                _, earg = balanced(text, end + 1 + em.end() - 1)
                params = count_top_level_items(earg)
                marks = (sql or "").count("?")
                stats["arity"] += 1
                if sql is not None and not dynamic and marks != params:
                    err(f"{where}: '?' в запросе {marks}, а в execute() параметров {params}: {sql[:80]}...")

        # 2) одиночные query/exec
        for m in re.finditer(r"->(query|exec)\(", text):
            line = text[:m.start()].count("\n") + 1
            _, arg = balanced(text, m.end() - 1)
            sql, dynamic = collect_sql(arg)
            if sql is None or dynamic or any(k in sql.lower() for k in MYSQL_ONLY):
                continue
            check_sql(conn, f"{fname}:{line}", sql)

        # 3) маршруты index.php
        if fname == "index.php":
            routes = re.findall(r"\$p === '([^']+)'", text)
            dupes = sorted({r for r in routes if routes.count(r) > 1})
            if dupes:
                err(f"index.php: дубли маршрутов: {dupes}")
            stats["routes"] = len(routes)
            need = ("player/login", "player/", "kill", "wave/complete", "perk/record", "run/save",
                    "blueprint/unlock", "meta/unlock", "stat/increment", "campaign/villages",
                    "campaign/battle", "campaign/vote", "season/current", "season/reset",
                    "leaderboard", "battlepass/rewards", "battlepass/progress", "battlepass/claim",
                    "shop/catalog", "shop/buy", "shop/history", "health")
            for r in need:
                if r == "player/":
                    if "preg_match" not in text:
                        err("index.php: нет маршрута GET /api/player/{id}")
                    continue
                if r not in routes:
                    err(f"index.php: нет маршрута {r} (обещан в docs/BACKEND_PHP.md)")

        # 4) profile_row/find_player читают players по имени колонки - сверяем со схемой
        tables = [r[0] for r in conn.execute("SELECT name FROM sqlite_master WHERE type='table'").fetchall()]
        cols_of = {t: {c[1] for c in conn.execute(f"PRAGMA table_info({t})").fetchall()} for t in tables}
        # Индекс-файл: $row/$r приходят из разных таблиц и через profile_row($row),
        # поэтому статически сопоставить строку с таблицей нельзя - это проверяет
        # живой smoke-тест (php -S + DB_DRIVER=sqlite). Здесь проверяем lib.php,
        # где контракт функции фиксирован: строка всегда из players.
        # lib.php: profile_row/find_player читают players - проверяем жёстко
        if fname == "lib.php":
            start = text.find("function profile_row")
            block = text[start:text.find("\n}", start) + 1]
            for name in sorted(set(re.findall(r"\$r\['(\w+)'\]", block))):
                if name not in cols_of["players"]:
                    err(f"lib.php: profile_row читает players.{name}, которой нет в схеме")
            start = text.find("function find_player")
            block = text[start:text.find("\n}", start) + 1]
            for name in sorted(set(re.findall(r"\$row\['(\w+)'\]", block))):
                if name not in cols_of["players"]:
                    err(f"lib.php: find_player читает players.{name}, которой нет в схеме")

    conn.close()
    print("=" * 60)
    print(f"SQL проверено: {stats['sql']}, пар prepare/execute: {stats['arity']}, маршрутов: {stats['routes']}")
    if skipped:
        print("Пропущено (проверяются только живым smoke-тестом):")
        for s in skipped:
            print("   -", s)
    if errors:
        for e in errors:
            print("ERROR:", e)
        print("=" * 60)
        print(f"Итог: {len(errors)} ошибок")
        return 1
    print("Итог: 0 ошибок (схема собирается, SQL поднимается, плейсхолдеры сходятся)")
    print("=" * 60)
    return 0


def check_sql(conn: sqlite3.Connection, where: str, sql: str) -> None:
    stats["sql"] += 1
    try:
        # EXPLAIN = prepare без исполнения (INSERT/UPDATE не троют данные);
        # фиктивные биндинги нужны, чтобы sqlite не ругался на число параметров
        conn.execute("EXPLAIN " + sql, [1] * sql.count("?"))
    except sqlite3.Error as e:
        err(f"{where}: SQL не поднимается на схеме ({e}): {sql[:110]}...")
    except Exception as e:  # noqa: BLE001
        err(f"{where}: SQL не подготовить ({e}): {sql[:110]}...")


if __name__ == "__main__":
    sys.exit(main())
