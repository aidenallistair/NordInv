"""
Nord Invasion - core dev-бэкенда (stdlib + sqlite3, без зависимостей).

Тот же контракт, что у PHP-бэкенда (src/backend-php): маршруты, поля, ответы.
Используется двумя обёртками:
  * dev_server.py - http.server (не надо ничего ставить)
  * main.py       - FastAPI (если fastapi/uvicorn установлены)

Единый источник каталога магазина/battlepass - src/backend-php/shop_catalog.json
(можно переопределить переменной NI_CATALOG).

Функция handle(method, path, fields, headers) -> (status, payload) чистая: её
же использует tools/test_backend_api.py без HTTP-сервера.
"""

from __future__ import annotations

import json
import os
import re
import sqlite3
import time

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))

DB_PATH = os.environ.get("NI_DB") or os.path.join(HERE, "ni_dev.db")
CATALOG_PATH = os.environ.get("NI_CATALOG") or os.path.join(REPO, "src", "backend-php", "shop_catalog.json")
API_SECRET = os.environ.get("NI_API_SECRET", "")
ADMIN_SECRET = os.environ.get("NI_ADMIN_SECRET", "")

START_GOLD = 500
BP_POINTS_PER_LEVEL = 25
BP_MAX_LEVEL = 20
BLUEPRINTS: list[str] = []
SHOP_ITEMS: dict[str, dict] = {}
BATTLEPASS: list[dict] = []

# ранги: титул -> (колонка-счётчик, порог)
TITLE_RULES = {"savior": ("revives", 50), "jarl_slayer": ("boss_kills", 10), "engineer_master": ("builds", 100)}
VILLAGES = [
    (0, "Village of Jelbegi", "swadia", 1, 100, 200),
    (1, "Forest Hamlet", "swadia", 1, 300, 400),
    (2, "Castle Outpost", "swadia", 2, 500, 100),
    (3, "Bridge Fort", "swadia", 3, 200, 500),
    (4, "Snow Village", "nords", 2, 700, 300),
    (5, "Desert Oasis", "swadia", 1, 400, 600),
    (6, "Mountain Keep", "nords", 3, 600, 700),
    (7, "Coastal Town", "swadia", 2, 100, 700),
]
SKILL_NODES = [
    ("blacksmith_1", "Apprentice Blacksmith", 10, ""),
    ("blacksmith_2", "Master Blacksmith", 20, "blacksmith_1"),
    ("veteran_1", "Veteran", 10, ""),
    ("veteran_2", "Elite Veteran", 25, "veteran_1"),
    ("engineer_1", "Engineer Basics", 15, ""),
    ("engineer_2", "Fortress Architect", 30, "engineer_1"),
    ("leader_1", "Squad Leader", 20, ""),
]


class ApiError(Exception):
    def __init__(self, message: str, status: int = 400):
        super().__init__(message)
        self.message = message
        self.status = status


# --------------------------------------------------------------------- catalog

def load_catalog(path: str = "") -> None:
    """Читает shop_catalog.json; при отсутствии файла - безопасные дефолты."""
    global START_GOLD, BP_POINTS_PER_LEVEL, BP_MAX_LEVEL, BLUEPRINTS, SHOP_ITEMS, BATTLEPASS
    p = path or CATALOG_PATH
    data = {}
    if os.path.isfile(p):
        with open(p, encoding="utf-8") as fh:
            data = json.load(fh)
    START_GOLD = int(data.get("new_player_gold", START_GOLD))
    BP_POINTS_PER_LEVEL = int(data.get("bp_points_per_level", BP_POINTS_PER_LEVEL)) or 25
    BP_MAX_LEVEL = int(data.get("bp_max_level", BP_MAX_LEVEL)) or 20
    BLUEPRINTS = list(data.get("blueprint_ids", []))
    SHOP_ITEMS = {i["id"]: i for i in data.get("items", []) if "id" in i}
    BATTLEPASS = list(data.get("battlepass", []))


load_catalog()

HAVE_CATALOG = bool(SHOP_ITEMS)


# ---------------------------------------------------------------------- schema

def connect() -> sqlite3.Connection:
    conn = sqlite3.connect(DB_PATH, isolation_level=None)
    conn.row_factory = sqlite3.Row
    conn.execute("PRAGMA foreign_keys = ON")
    return conn


SCHEMA = """
CREATE TABLE IF NOT EXISTS players (
  id TEXT PRIMARY KEY, steam_id TEXT NOT NULL DEFAULT '', peer_name TEXT NOT NULL DEFAULT '',
  gold INTEGER NOT NULL DEFAULT 500, wood INTEGER NOT NULL DEFAULT 0, metal INTEGER NOT NULL DEFAULT 0,
  kills INTEGER NOT NULL DEFAULT 0, deaths INTEGER NOT NULL DEFAULT 0,
  level INTEGER NOT NULL DEFAULT 1, xp INTEGER NOT NULL DEFAULT 0,
  season_points INTEGER NOT NULL DEFAULT 0, season_points_earned INTEGER NOT NULL DEFAULT 0,
  battlepass_level INTEGER NOT NULL DEFAULT 0,
  wins INTEGER NOT NULL DEFAULT 0, losses INTEGER NOT NULL DEFAULT 0, best_wave INTEGER NOT NULL DEFAULT 0,
  revives INTEGER NOT NULL DEFAULT 0, boss_kills INTEGER NOT NULL DEFAULT 0, builds INTEGER NOT NULL DEFAULT 0,
  perks TEXT, blueprints TEXT, meta TEXT, titles TEXT, cosmetics TEXT,
  last_seen INTEGER NOT NULL DEFAULT 0, created_at INTEGER NOT NULL DEFAULT 0);
CREATE INDEX IF NOT EXISTS idx_players_steam ON players(steam_id);

CREATE TABLE IF NOT EXISTS kill_log (
  id INTEGER PRIMARY KEY AUTOINCREMENT, player_id TEXT NOT NULL, wave INTEGER NOT NULL DEFAULT 0,
  troop TEXT NOT NULL DEFAULT '', gold INTEGER NOT NULL DEFAULT 0, created_at INTEGER NOT NULL DEFAULT 0);

CREATE TABLE IF NOT EXISTS villages (
  id INTEGER PRIMARY KEY, name TEXT NOT NULL, owner TEXT NOT NULL DEFAULT 'swadia',
  defense_level INTEGER NOT NULL DEFAULT 1, x INTEGER NOT NULL DEFAULT 0, y INTEGER NOT NULL DEFAULT 0,
  battles_won INTEGER NOT NULL DEFAULT 0, battles_lost INTEGER NOT NULL DEFAULT 0);

CREATE TABLE IF NOT EXISTS seasons (
  id INTEGER PRIMARY KEY, name TEXT NOT NULL, start_time INTEGER NOT NULL, end_time INTEGER NOT NULL, rewards TEXT);

CREATE TABLE IF NOT EXISTS battlepass_rewards (
  level INTEGER PRIMARY KEY, reward_type TEXT NOT NULL, reward_id TEXT NOT NULL, reward_name TEXT NOT NULL);

CREATE TABLE IF NOT EXISTS skill_nodes (
  id TEXT PRIMARY KEY, name TEXT NOT NULL, cost INTEGER NOT NULL DEFAULT 0, requires TEXT NOT NULL DEFAULT '');

CREATE TABLE IF NOT EXISTS campaign_votes (
  id INTEGER PRIMARY KEY AUTOINCREMENT, village_id INTEGER NOT NULL, voter TEXT NOT NULL,
  season INTEGER NOT NULL DEFAULT 1, created_at INTEGER NOT NULL DEFAULT 0,
  UNIQUE (village_id, voter, season));

CREATE TABLE IF NOT EXISTS battlepass_claims (
  id INTEGER PRIMARY KEY AUTOINCREMENT, player_id TEXT NOT NULL, level INTEGER NOT NULL,
  season INTEGER NOT NULL DEFAULT 1, reward TEXT NOT NULL DEFAULT '', created_at INTEGER NOT NULL DEFAULT 0,
  UNIQUE (player_id, level, season));

CREATE TABLE IF NOT EXISTS shop_purchases (
  id INTEGER PRIMARY KEY AUTOINCREMENT, player_id TEXT NOT NULL, item_id TEXT NOT NULL,
  qty INTEGER NOT NULL DEFAULT 1, gold INTEGER NOT NULL DEFAULT 0, wood INTEGER NOT NULL DEFAULT 0,
  metal INTEGER NOT NULL DEFAULT 0, created_at INTEGER NOT NULL DEFAULT 0);

CREATE TABLE IF NOT EXISTS season_history (
  id INTEGER PRIMARY KEY AUTOINCREMENT, season INTEGER NOT NULL, player_id TEXT NOT NULL,
  season_points INTEGER NOT NULL DEFAULT 0, bp_level INTEGER NOT NULL DEFAULT 0,
  kills INTEGER NOT NULL DEFAULT 0, boss_kills INTEGER NOT NULL DEFAULT 0, best_wave INTEGER NOT NULL DEFAULT 0,
  wins INTEGER NOT NULL DEFAULT 0, losses INTEGER NOT NULL DEFAULT 0, created_at INTEGER NOT NULL DEFAULT 0);
"""


def init_db() -> None:
    conn = connect()
    try:
        for stmt in [s.strip() for s in SCHEMA.split(";") if s.strip()]:
            conn.execute(stmt)
        if not conn.execute("SELECT COUNT(*) FROM villages").fetchone()[0]:
            conn.executemany("INSERT INTO villages (id, name, owner, defense_level, x, y) VALUES (?,?,?,?,?,?)", VILLAGES)
        if not conn.execute("SELECT COUNT(*) FROM seasons").fetchone()[0]:
            now = int(time.time())
            conn.execute("INSERT INTO seasons (id, name, start_time, end_time, rewards) VALUES (1,?,?,?,?)",
                         ("Season 1: Nord Awakening", now, now + 60 * 60 * 24 * 60, "[]"))
        if not conn.execute("SELECT COUNT(*) FROM skill_nodes").fetchone()[0]:
            conn.executemany("INSERT INTO skill_nodes (id, name, cost, requires) VALUES (?,?,?,?)", SKILL_NODES)
        if not conn.execute("SELECT COUNT(*) FROM battlepass_rewards").fetchone()[0] and BATTLEPASS:
            conn.executemany("INSERT INTO battlepass_rewards (level, reward_type, reward_id, reward_name) VALUES (?,?,?,?)",
                             [(int(r["level"]), str(r.get("type", "gold")), str(r.get("id", "")), str(r.get("name", "")))
                              for r in BATTLEPASS])
    finally:
        conn.close()


# ------------------------------------------------------------------ helpers

def _int(fields: dict, key: str, default: int = 0) -> int:
    v = fields.get(key, "")
    if v is None or v == "":
        return default
    try:
        return int(float(v))
    except (TypeError, ValueError):
        return default


def _bool(fields: dict, key: str, default: bool = False) -> bool:
    v = fields.get(key, None)
    if v is None or v == "":
        return default
    if isinstance(v, bool):
        return v
    return str(v).strip().lower() in ("1", "true", "yes", "on")


def _json_list(row: sqlite3.Row | None, col: str) -> list:
    if row is None:
        return []
    raw = row[col] if col in row.keys() else None
    try:
        val = json.loads(raw or "[]")
    except (TypeError, ValueError):
        return []
    return val if isinstance(val, list) else []


def _list_fields(row: sqlite3.Row) -> dict:
    return {
        "blueprints": _json_list(row, "blueprints"),
        "perks": [int(x) for x in _json_list(row, "perks")],
        "meta": _json_list(row, "meta"),
        "titles": _json_list(row, "titles"),
        "cosmetics": _json_list(row, "cosmetics"),
    }


def identity(fields: dict) -> tuple[str, str, str]:
    steam = str(fields.get("steam_id") or "").strip()
    name = str(fields.get("name") or fields.get("player_name") or "").strip()
    pid = str(fields.get("player_id") or "").strip()
    if not pid:
        pid = f"steam_{steam}" if steam else (f"name_{_md5(name)}" if name else "")
    if not pid:
        raise ApiError("missing player identity")
    return pid, steam, name


def _md5(s: str) -> str:
    import hashlib
    return hashlib.md5(s.encode("utf-8")).hexdigest()


def bp_level_from(earned: int) -> int:
    return min(BP_MAX_LEVEL, max(0, int(earned)) // max(1, BP_POINTS_PER_LEVEL))


def find_player(conn: sqlite3.Connection, pid: str, steam: str, name: str, create: bool = True) -> sqlite3.Row | None:
    row = conn.execute("SELECT * FROM players WHERE id = ? OR (steam_id <> '' AND steam_id = ?) LIMIT 1",
                       (pid, steam)).fetchone()
    if row is not None:
        if name:
            conn.execute("UPDATE players SET peer_name = ?, last_seen = ? WHERE id = ?", (name, int(time.time()), row["id"]))
            row = conn.execute("SELECT * FROM players WHERE id = ?", (row["id"],)).fetchone()
        return row
    if not create:
        return None
    now = int(time.time())
    conn.execute("INSERT INTO players (id, steam_id, peer_name, gold, last_seen, created_at) VALUES (?,?,?,?,?,?)",
                 (pid, steam, name, START_GOLD, now, now))
    return conn.execute("SELECT * FROM players WHERE id = ?", (pid,)).fetchone()


def apply_xp(row: sqlite3.Row, gain: int) -> tuple[int, int]:
    xp = int(row["xp"]) + gain
    level = int(row["level"])
    while xp >= level * 100:
        xp -= level * 100
        level += 1
    return level, xp


def credit_sp(conn: sqlite3.Connection, row: sqlite3.Row, amount: int) -> None:
    """+season_points (тратятся) и +season_points_earned (прогресс BattlePass)."""
    earned = int(row["season_points_earned"] or 0) + amount
    conn.execute("UPDATE players SET season_points = season_points + ?, season_points_earned = ?, battlepass_level = ? WHERE id = ?",
                 (amount, earned, bp_level_from(earned), row["id"]))


def grant_titles(conn: sqlite3.Connection, row: sqlite3.Row) -> list[str]:
    """Авто-титулы по счётчикам (пороги те же, что в PHP grant_titles)."""
    fresh = conn.execute("SELECT * FROM players WHERE id = ?", (row["id"],)).fetchone() or row
    titles = _json_list(fresh, "titles")
    earned = [t for t, (col, need) in TITLE_RULES.items() if int(fresh[col] or 0) >= need and t not in titles]
    if earned:
        conn.execute("UPDATE players SET titles = ? WHERE id = ?", (json.dumps(titles + earned), fresh["id"]))
    return earned


def grant_title(conn: sqlite3.Connection, row: sqlite3.Row, title: str) -> str | None:
    titles = _json_list(row, "titles")
    if title in titles:
        return None
    titles.append(title)
    conn.execute("UPDATE players SET titles = ? WHERE id = ?", (json.dumps(titles), row["id"]))
    return title


def parse_grant(g: str) -> tuple[str, int, str]:
    """'wood:10' -> ('wood',10,''); 'blueprint:stakes' -> ('blueprint',0,'stakes')."""
    kind, _, rest = str(g).partition(":")
    kind = kind.strip().lower()
    rest = rest.strip()
    try:
        value = int(float(rest)) if rest else 0
    except ValueError:
        value = 0
    return kind, value, rest


SERVER_SIDE = ("gold", "wood", "metal", "season_points", "blueprint", "title", "skin")


def apply_grants(conn: sqlite3.Connection, pid: str, grants: list[str]) -> dict:
    """Серверная часть наград. Клиентские токены (heal/ammo/repair) просто проксируются."""
    dg = dw = dm = dsp = 0
    new_bp: list[str] = []
    new_titles: list[str] = []
    new_cosm: list[str] = []
    applied: list[str] = []

    for raw in grants:
        kind, value, rest = parse_grant(raw)
        if not kind:
            continue
        if kind == "gold":
            dg += max(0, value); applied.append(f"gold:{max(0, value)}")
        elif kind == "wood":
            dw += max(0, value); applied.append(f"wood:{max(0, value)}")
        elif kind == "metal":
            dm += max(0, value); applied.append(f"metal:{max(0, value)}")
        elif kind == "season_points":
            dsp += max(0, value); applied.append(f"season_points:{max(0, value)}")
        elif kind == "blueprint":
            if not rest or rest not in BLUEPRINTS:
                raise ApiError(f"unknown blueprint: {rest}")
            if rest not in new_bp:
                new_bp.append(rest); applied.append(f"blueprint:{rest}")
        elif kind == "title":
            if rest and rest not in new_titles:
                new_titles.append(rest); applied.append(f"title:{rest}")
        elif kind == "skin":
            if rest and rest not in new_cosm:
                new_cosm.append(rest); applied.append(f"skin:{rest}")
        elif kind not in SERVER_SIDE:
            applied.append(str(raw).strip())

    row = conn.execute("SELECT gold, wood, metal, season_points, blueprints, titles, cosmetics FROM players WHERE id = ?",
                      (pid,)).fetchone()
    if row is None:
        raise ApiError("player vanished", 500)

    bps = _json_list(row, "blueprints")
    bps += [b for b in new_bp if b not in bps]
    titles = _json_list(row, "titles")
    titles += [t for t in new_titles if t not in titles]
    cosm = _json_list(row, "cosmetics")
    cosm += [c for c in new_cosm if c not in cosm]

    conn.execute("UPDATE players SET gold = MAX(0, gold + ?), wood = wood + ?, metal = metal + ?, "
                 "season_points = season_points + ?, season_points_earned = season_points_earned + ?, "
                 "blueprints = ?, titles = ?, cosmetics = ? WHERE id = ?",
                 (dg, dw, dm, dsp, dsp, json.dumps(bps), json.dumps(titles), json.dumps(cosm), pid))
    earned = int(conn.execute("SELECT season_points_earned FROM players WHERE id = ?", (pid,)).fetchone()[0])
    conn.execute("UPDATE players SET battlepass_level = ? WHERE id = ?", (bp_level_from(earned), pid))

    return {
        "applied": applied,
        "balances": {
            "gold": max(0, int(row["gold"]) + dg),
            "wood": int(row["wood"]) + dw,
            "metal": int(row["metal"]) + dm,
            "season_points": int(row["season_points"]) + dsp,
            "blueprints": bps,
            "titles": titles,
            "cosmetics": cosm,
        },
    }


def reward_to_grant(reward: sqlite3.Row) -> str:
    rtype, rid = str(reward["reward_type"]), str(reward["reward_id"])
    if rtype in ("gold", "wood", "metal", "season_points"):
        return f"{rtype}:{int(rid or 0)}"
    if rtype == "blueprint":
        if rid not in BLUEPRINTS:
            raise ApiError(f"unknown blueprint in battlepass: {rid}", 500)
        return f"blueprint:{rid}"
    if rtype in ("title", "skin"):
        return f"{rtype}:{rid}"
    raise ApiError(f"unknown reward type: {rtype}", 500)


def current_season(conn: sqlite3.Connection) -> sqlite3.Row | None:
    return conn.execute("SELECT * FROM seasons ORDER BY id DESC LIMIT 1").fetchone()


def claimed_levels(conn: sqlite3.Connection, pid: str, season: int) -> list[int]:
    return [int(r[0]) for r in conn.execute(
        "SELECT level FROM battlepass_claims WHERE player_id = ? AND season = ?", (pid, season)).fetchall()]


def profile(row: sqlite3.Row) -> dict:
    out = {
        "id": row["id"], "steam_id": row["steam_id"], "name": row["peer_name"],
        "gold": int(row["gold"]), "wood": int(row["wood"]), "metal": int(row["metal"]),
        "kills": int(row["kills"]), "deaths": int(row["deaths"]),
        "level": int(row["level"]), "xp": int(row["xp"]),
        "season_points": int(row["season_points"]), "season_points_earned": int(row["season_points_earned"]),
        "battlepass_level": bp_level_from(int(row["season_points_earned"])),
        "wins": int(row["wins"]), "losses": int(row["losses"]), "best_wave": int(row["best_wave"]),
        "revives": int(row["revives"]), "boss_kills": int(row["boss_kills"]), "builds": int(row["builds"]),
    }
    out.update(_list_fields(row))
    return out


def catalog_json() -> dict:
    items = [{
        "id": i["id"], "name": i.get("name", ""), "type": i.get("type", "resource"),
        "gold": int(i.get("gold", 0)), "wood": int(i.get("wood", 0)), "metal": int(i.get("metal", 0)),
        "grants": [str(g) for g in i.get("grants", [])], "desc": i.get("desc", ""),
    } for i in SHOP_ITEMS.values()]
    return {"version": 1, "bp_points_per_level": BP_POINTS_PER_LEVEL, "blueprints": BLUEPRINTS, "items": items}


# ----------------------------------------------------------------- routes

def handle(method: str, path: str, fields: dict | None = None, headers: dict | None = None) -> tuple[int, dict]:
    """method: GET/POST, path: '/api/kill' или 'kill', fields: поля запроса."""
    headers = {k.lower(): v for k, v in (headers or {}).items()}
    fields = dict(fields or {})
    path = re.sub(r"^/+api/?", "", str(path).split("?", 1)[0]).strip("/")

    if API_SECRET and headers.get("x-ni-secret", "") != API_SECRET:
        raise ApiError("unauthorized", 401)

    conn = connect()
    try:
        return _route(conn, method.upper(), path, fields, headers)
    finally:
        conn.close()


def _route(conn, method, p, f, h) -> tuple[int, dict]:
    if p in ("", "index.html"):
        if method != "GET":
            raise ApiError("not found", 404)
        return 200, {"message": "Nord Invasion Backend (dev, sqlite)", "version": "2.1",
                     "ok": True, "catalog": HAVE_CATALOG}
    if p == "health":
        return 200, {"ok": True, "db": "sqlite", "time": int(time.time()), "catalog": HAVE_CATALOG}

    # --- игрок ---
    if p == "player/login" and method == "POST":
        pid, steam, name = identity(f)
        row = find_player(conn, pid, steam, name, True)
        out = profile(row)
        out["new"] = int(row["kills"]) == 0 and int(row["wins"]) == 0 and int(row["losses"]) == 0
        return 200, out
    m = re.fullmatch(r"player/([\w.\-]+)", p)
    if m and method == "GET":
        row = conn.execute("SELECT * FROM players WHERE id = ? OR steam_id = ? LIMIT 1", (m.group(1), m.group(1))).fetchone()
        if row is None:
            raise ApiError("Player not found", 404)
        return 200, profile(row)

    # --- бой ---
    if p == "kill" and method == "POST":
        pid, steam, name = identity(f)
        row = find_player(conn, pid, steam, name, True)
        gold, wood, metal = _int(f, "gold_reward", 10), _int(f, "wood"), _int(f, "metal")
        wave = _int(f, "wave")
        troop = str(f.get("killed_troop", ""))[:128]
        is_boss = _bool(f, "is_boss") or "chieftain" in troop or "boss" in troop or "jarl" in troop
        level, xp = apply_xp(row, 10)
        conn.execute("UPDATE players SET gold = gold + ?, kills = kills + 1, wood = wood + ?, metal = metal + ?, "
                     "xp = ?, level = ?, boss_kills = boss_kills + ?, last_seen = ? WHERE id = ?",
                     (gold, wood, metal, xp, level, 1 if is_boss else 0, int(time.time()), row["id"]))
        credit_sp(conn, row, 1)
        conn.execute("INSERT INTO kill_log (player_id, wave, troop, gold, created_at) VALUES (?,?,?,?,?)",
                     (row["id"], wave, troop, gold, int(time.time())))
        out = {"status": "ok", "reward": gold}
        if is_boss:
            titles = grant_titles(conn, row)
            if titles:
                out["titles_earned"] = titles
        return 200, out

    if p == "wave/complete" and method == "POST":
        pid, steam, name = identity(f)
        row = find_player(conn, pid, steam, name, True)
        wave = max(1, _int(f, "wave", 1))
        gold, wood, metal, perk_id = _int(f, "gold"), _int(f, "wood"), _int(f, "metal"), _int(f, "perk_id", -1)
        perks = _json_list(row, "perks")
        if 0 <= perk_id <= 99 and perk_id not in perks:
            perks.append(perk_id)
        level, xp = apply_xp(row, 5 * wave)
        conn.execute("UPDATE players SET gold = gold + ?, wood = wood + ?, metal = metal + ?, xp = ?, level = ?, "
                     "best_wave = MAX(best_wave, ?), perks = ?, last_seen = ? WHERE id = ?",
                     (gold, wood, metal, xp, level, wave, json.dumps(perks), int(time.time()), row["id"]))
        credit_sp(conn, row, 1)
        return 200, {"status": "ok", "wave": wave, "level": level, "xp": xp}

    if p == "perk/record" and method == "POST":
        pid, steam, name = identity(f)
        perk_id = _int(f, "perk_id", -1)
        if not 0 <= perk_id <= 99:
            raise ApiError("bad perk_id")
        row = find_player(conn, pid, steam, name, True)
        perks = _json_list(row, "perks")
        new = perk_id not in perks
        if new:
            perks.append(perk_id)
            conn.execute("UPDATE players SET perks = ? WHERE id = ?", (json.dumps(perks), row["id"]))
        return 200, {"perks": perks, "new": new}

    if p == "run/save" and method == "POST":
        pid, steam, name = identity(f)
        row = find_player(conn, pid, steam, name, True)
        won = _bool(f, "won")
        wave_reached, kills, deaths = _int(f, "wave_reached", 1), _int(f, "kills"), _int(f, "deaths")
        bonus_gold, bonus_sp = (100, 50) if won else (0, 0)
        conn.execute(f"UPDATE players SET {'wins = wins + 1' if won else 'losses = losses + 1'}, "
                     "best_wave = MAX(best_wave, ?), deaths = deaths + ?, gold = gold + ?, last_seen = ? WHERE id = ?",
                     (wave_reached, deaths, bonus_gold, int(time.time()), row["id"]))
        credit_sp(conn, row, bonus_sp)
        out = {"status": "ok", "won": won, "bonus_gold": bonus_gold}
        if (won or wave_reached >= 10) and deaths == 0:
            title = grant_title(conn, conn.execute("SELECT * FROM players WHERE id = ?", (row["id"],)).fetchone(), "wall")
            if title:
                out["titles_earned"] = [title]
        return 200, out

    if p == "blueprint/unlock" and method == "POST":
        pid, steam, name = identity(f)
        bid = str(f.get("blueprint_id", ""))[:128]
        if bid not in BLUEPRINTS:
            raise ApiError(f"unknown blueprint: {bid}")
        row = find_player(conn, pid, steam, name, True)
        bps = _json_list(row, "blueprints")
        new = bid not in bps
        if new:
            bps.append(bid)
            conn.execute("UPDATE players SET blueprints = ? WHERE id = ?", (json.dumps(bps), row["id"]))
        return 200, {"blueprints": bps, "new": new}

    if p == "meta/unlock" and method == "POST":
        pid, steam, name = identity(f)
        node_id = str(f.get("node_id", ""))[:64]
        node = conn.execute("SELECT * FROM skill_nodes WHERE id = ?", (node_id,)).fetchone()
        if node is None:
            raise ApiError(f"unknown skill node: {node_id}")
        row = find_player(conn, pid, steam, name, True)
        meta = _json_list(row, "meta")
        if node_id in meta:
            return 200, {"meta": meta, "already": True}
        if node["requires"] and node["requires"] not in meta:
            raise ApiError(f"requires node: {node['requires']}")
        if int(row["season_points"]) < int(node["cost"]):
            raise ApiError(f"not enough season_points (need {int(node['cost'])})")
        meta.append(node_id)
        conn.execute("UPDATE players SET meta = ?, season_points = season_points - ? WHERE id = ?",
                     (json.dumps(meta), int(node["cost"]), row["id"]))
        return 200, {"meta": meta, "spent": int(node["cost"])}

    if p == "stat/increment" and method == "POST":
        pid, steam, name = identity(f)
        stat = str(f.get("stat", ""))
        if stat not in ("revives", "builds", "boss_kills"):
            raise ApiError("unknown stat")
        row = find_player(conn, pid, steam, name, True)
        conn.execute(f"UPDATE players SET {stat} = {stat} + 1 WHERE id = ?", (row["id"],))
        out = {"status": "ok", stat: int(row[stat]) + 1}
        titles = grant_titles(conn, row)
        if titles:
            out["titles_earned"] = titles
        return 200, out

    # --- кампания ---
    if p == "campaign/villages" and method == "GET":
        season = current_season(conn)
        sid = int(season["id"]) if season else 1
        votes = {int(r[0]): int(r[1]) for r in conn.execute(
            "SELECT village_id, COUNT(*) FROM campaign_votes WHERE season = ? GROUP BY village_id", (sid,)).fetchall()}
        rows = [{
            "id": int(r["id"]), "name": r["name"], "owner": r["owner"], "defense": int(r["defense_level"]),
            "x": int(r["x"]), "y": int(r["y"]), "won": int(r["battles_won"]), "lost": int(r["battles_lost"]),
            "votes": votes.get(int(r["id"]), 0),
        } for r in conn.execute("SELECT * FROM villages ORDER BY id").fetchall()]
        return 200, rows  # type: ignore[return-value]

    if p == "campaign/battle" and method == "POST":
        vid = _int(f, "village_id", -1)
        won = _bool(f, "won")
        village = conn.execute("SELECT * FROM villages WHERE id = ?", (vid,)).fetchone()
        if village is None:
            raise ApiError("unknown village", 404)
        if won:
            conn.execute("UPDATE villages SET battles_won = battles_won + 1, owner = 'swadia', "
                         "defense_level = defense_level + 1 WHERE id = ?", (vid,))
        else:
            conn.execute("UPDATE villages SET battles_lost = battles_lost + 1, owner = 'nords', "
                         "defense_level = MAX(1, defense_level - 1) WHERE id = ?", (vid,))
        for pid_raw in [x.strip() for x in str(f.get("players", "")).split(",") if x.strip()]:
            pr = conn.execute("SELECT * FROM players WHERE id = ? OR steam_id = ? LIMIT 1", (pid_raw, pid_raw)).fetchone()
            if pr is not None:
                conn.execute("UPDATE players SET gold = gold + 200 WHERE id = ?", (pr["id"],))
                credit_sp(conn, pr, 10)
        return 200, {"village_id": vid, "won": won}

    if p == "campaign/vote" and method == "POST":
        vid = _int(f, "village_id", -1)
        voter = str(f.get("voter", ""))[:128]
        season = current_season(conn)
        sid = int(season["id"]) if season else 1
        if conn.execute("SELECT * FROM villages WHERE id = ?", (vid,)).fetchone() is None:
            raise ApiError("unknown village", 404)
        try:
            conn.execute("INSERT INTO campaign_votes (village_id, voter, season, created_at) VALUES (?,?,?,?)",
                         (vid, voter, sid, int(time.time())))
        except sqlite3.IntegrityError:
            raise ApiError("already voted this season", 409)
        return 200, {"village_id": vid, "voter": voter, "season": sid}

    # --- сезон / лидерборд / battlepass ---
    if p == "season/current" and method == "GET":
        season = current_season(conn)
        if season is None:
            raise ApiError("No season", 404)
        return 200, {"id": int(season["id"]), "name": season["name"],
                     "start": int(season["start_time"]), "end": int(season["end_time"])}

    if p == "leaderboard" and method == "GET":
        rows = conn.execute("SELECT peer_name AS name, kills, gold, level, season_points FROM players "
                            "ORDER BY season_points DESC, kills DESC LIMIT 20").fetchall()
        return 200, [dict(r) for r in rows]  # type: ignore[return-value]

    if p == "battlepass/rewards" and method == "GET":
        rows = conn.execute("SELECT * FROM battlepass_rewards ORDER BY level").fetchall()
        return 200, [{"level": int(r["level"]), "type": r["reward_type"], "id": r["reward_id"],
                      "name": r["reward_name"]} for r in rows]

    if p == "battlepass/progress" and method == "GET":
        pid, steam, name = identity(f)
        row = find_player(conn, pid, steam, name, False)
        if row is None:
            raise ApiError("player not found", 404)
        season = current_season(conn)
        sid = int(season["id"]) if season else 1
        earned = int(row["season_points_earned"])
        level = bp_level_from(earned)
        claimed = claimed_levels(conn, row["id"], sid)
        rewards = [{"level": int(r["level"]), "type": r["reward_type"], "id": r["reward_id"], "name": r["reward_name"],
                    "unlocked": int(r["level"]) <= level, "claimed": int(r["level"]) in claimed}
                   for r in conn.execute("SELECT * FROM battlepass_rewards ORDER BY level").fetchall()]
        return 200, {"season": sid, "points": int(row["season_points"]), "points_earned": earned,
                     "level": level, "max_level": BP_MAX_LEVEL, "points_per_level": BP_POINTS_PER_LEVEL,
                     "points_to_next": 0 if level >= BP_MAX_LEVEL else (level + 1) * BP_POINTS_PER_LEVEL - earned,
                     "claimed": claimed, "rewards": rewards}

    if p == "battlepass/claim" and method == "POST":
        pid, steam, name = identity(f)
        level = _int(f, "level", -1)
        if not 1 <= level <= BP_MAX_LEVEL:
            raise ApiError("bad level")
        row = find_player(conn, pid, steam, name, True)
        reward = conn.execute("SELECT * FROM battlepass_rewards WHERE level = ?", (level,)).fetchone()
        if reward is None:
            raise ApiError(f"no reward for level {level}", 404)
        season = current_season(conn)
        sid = int(season["id"]) if season else 1
        if level in claimed_levels(conn, row["id"], sid):
            raise ApiError("already claimed", 409)
        have = bp_level_from(int(row["season_points_earned"]))
        if level > have:
            raise ApiError(f"battlepass level {level} required (you have {have})")
        res = apply_grants(conn, row["id"], [reward_to_grant(reward)])
        conn.execute("INSERT INTO battlepass_claims (player_id, level, season, reward, created_at) VALUES (?,?,?,?,?)",
                     (row["id"], level, sid, str(reward["reward_name"]), int(time.time())))
        out = {"status": "ok", "level": level, "season": sid,
               "reward": {"type": reward["reward_type"], "id": reward["reward_id"], "name": reward["reward_name"]}}
        out.update({"granted": res["applied"], **res["balances"]})
        return 200, out

    # --- магазин ---
    if p == "shop/catalog" and method == "GET":
        if not HAVE_CATALOG:
            raise ApiError(f"shop catalog missing: {CATALOG_PATH}", 503)
        return 200, catalog_json()

    if p == "shop/buy" and method == "POST":
        if not HAVE_CATALOG:
            raise ApiError(f"shop catalog missing: {CATALOG_PATH}", 503)
        pid, steam, name = identity(f)
        item_id = str(f.get("item_id", ""))[:128]
        qty = max(1, min(5, _int(f, "qty", 1)))
        item = SHOP_ITEMS.get(item_id)
        if item is None:
            raise ApiError(f"unknown item: {item_id}")
        row = find_player(conn, pid, steam, name, True)
        cost = {"gold": int(item.get("gold", 0)) * qty, "wood": int(item.get("wood", 0)) * qty,
                "metal": int(item.get("metal", 0)) * qty}
        grants = list(item.get("grants", [])) * qty

        # валидация наград до списания
        for g in item.get("grants", []):
            kind, _, rest = parse_grant(g)
            if kind == "blueprint" and rest not in BLUEPRINTS:
                raise ApiError(f"unknown blueprint in catalog: {rest}", 500)
        if item.get("type") == "blueprint":
            owned = _json_list(row, "blueprints")
            bid = next((parse_grant(g)[2] for g in item.get("grants", []) if parse_grant(g)[0] == "blueprint"), "")
            if bid in owned:
                raise ApiError(f"already unlocked: {bid}", 409)

        if int(row["gold"]) < cost["gold"] or int(row["wood"]) < cost["wood"] or int(row["metal"]) < cost["metal"]:
            raise ApiError(f"not enough resources (need {cost['gold']}g {cost['wood']}w {cost['metal']}m)")

        conn.execute("UPDATE players SET gold = gold - ?, wood = wood - ?, metal = metal - ?, last_seen = ? WHERE id = ?",
                     (cost["gold"], cost["wood"], cost["metal"], int(time.time()), row["id"]))
        res = apply_grants(conn, row["id"], [str(g) for g in grants])
        conn.execute("INSERT INTO shop_purchases (player_id, item_id, qty, gold, wood, metal, created_at) VALUES (?,?,?,?,?,?,?)",
                     (row["id"], item_id, qty, cost["gold"], cost["wood"], cost["metal"], int(time.time())))
        out = {"status": "ok", "item_id": item_id, "qty": qty, "paid": cost, "granted": res["applied"]}
        out.update(res["balances"])
        return 200, out

    if p == "shop/history" and method == "GET":
        pid, steam, name = identity(f)
        row = find_player(conn, pid, steam, name, False)
        if row is None:
            raise ApiError("player not found", 404)
        rows = conn.execute("SELECT item_id, qty, gold, wood, metal, created_at FROM shop_purchases "
                            "WHERE player_id = ? ORDER BY id DESC LIMIT 50", (row["id"],)).fetchall()
        return 200, [dict(r) for r in rows]  # type: ignore[return-value]

    if p == "season/reset" and method == "POST":
        if not ADMIN_SECRET:
            raise ApiError("season reset disabled: set NI_ADMIN_SECRET", 503)
        got = h.get("x-ni-admin", "") or str(f.get("admin_key", ""))
        if got != ADMIN_SECRET:
            raise ApiError("forbidden", 403)
        season = current_season(conn)
        sid = int(season["id"]) if season else 1
        now = int(time.time())
        cur = conn.execute("SELECT * FROM players").fetchall()
        conn.executemany("INSERT INTO season_history (season, player_id, season_points, bp_level, kills, boss_kills, "
                         "best_wave, wins, losses, created_at) VALUES (?,?,?,?,?,?,?,?,?,?)",
                         [(sid, r["id"], int(r["season_points"]), int(r["battlepass_level"]), int(r["kills"]),
                           int(r["boss_kills"]), int(r["best_wave"]), int(r["wins"]), int(r["losses"]), now) for r in cur])
        conn.execute("UPDATE players SET season_points = 0, season_points_earned = 0, battlepass_level = 0, meta = '[]'")
        nxt = sid + 1
        conn.execute("INSERT INTO seasons (id, name, start_time, end_time, rewards) VALUES (?,?,?,?,?)",
                     (nxt, f"Season {nxt}", now, now + 60 * 60 * 24 * 60, "[]"))
        return 200, {"status": "ok", "archived_season": sid, "new_season": nxt, "players_archived": len(cur)}

    raise ApiError(f"not found: {method} /api/{p}", 404)
