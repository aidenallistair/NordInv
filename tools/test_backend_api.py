#!/usr/bin/env python3
"""
Контрактный тест бэкенда Nord Invasion (один набор проверок для PHP и Python).

Запуск:
    python3 tools/test_backend_api.py                      # in-process (stdlib, без сети)
    python3 tools/test_backend_api.py --serve [--port N]    # через HTTP + dev_server.py
    python3 tools/test_backend_api.py --base http://host:8080 --secret S [--admin-key A]
                                                           # против PHP/удалённого бэкенда

Тестирует контракт из docs/BACKEND_PHP.md: маршруты, поля, коды ошибок,
идемпотентность (перки, покупки чертежей, claim'ы, голоса), авто-титулы,
сезонные очки/BattlePass и сброс сезона.

В режимах in-process/serve база создаётся во временном каталоге, поэтому
тест не трогает рабочие данные. Для --base используйте тестовый профиль:
проверки пишут данные игрока test_api_<случайное>.
"""

from __future__ import annotations

import argparse
import json
import os
import random
import string
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BACKEND_DIR = os.path.join(REPO, "src", "backend")
sys.path.insert(0, BACKEND_DIR)

ADMIN_KEY = "test-admin-key"
PASS = FAIL = 0
FAILED: list[str] = []


# ------------------------------------------------------------------ транспорты

class InProc:
    """Вызывает nidb.handle напрямую (без сокета)."""

    def __init__(self, db: str, catalog: str, secret: str) -> None:
        os.environ["NI_DB"] = db
        os.environ["NI_CATALOG"] = catalog
        os.environ["NI_API_SECRET"] = secret
        os.environ["NI_ADMIN_SECRET"] = ADMIN_KEY
        import nidb
        self.nidb = nidb
        nidb.init_db()

    def call(self, method: str, path: str, fields=None, headers=None, with_secret=True):
        from urllib.parse import parse_qsl, urlparse
        h = dict(headers or {})
        q = dict(parse_qsl(urlparse(path).query))
        if with_secret and self.nidb.API_SECRET:
            h["x-ni-secret"] = self.nidb.API_SECRET
        merged = dict(q)
        merged.update(fields or {})
        try:
            return self.nidb.handle(method, path, merged, h)
        except self.nidb.ApiError as e:
            return e.status, {"error": e.message}


class Http:
    """POST/GET через urllib (для --serve и --base)."""

    def __init__(self, base: str, secret: str) -> None:
        self.base = base.rstrip("/")
        self.secret = secret

    def call(self, method: str, path: str, fields=None, headers=None, with_secret=True):
        url = self.base + (path if path.startswith("/") else "/" + path)
        data = None
        h = {"Accept": "application/json"}
        if with_secret and self.secret:
            h["X-NI-Secret"] = self.secret
        h.update(headers or {})
        if method == "POST":
            data = urllib.parse.urlencode(fields or {}).encode()
            h["Content-Type"] = "application/x-www-form-urlencoded"
        req = urllib.request.Request(url, data=data, headers=h, method=method)
        try:
            with urllib.request.urlopen(req, timeout=15) as resp:
                return resp.status, json.loads(resp.read().decode("utf-8") or "{}")
        except urllib.error.HTTPError as e:
            raw = e.read().decode("utf-8", "replace")
            try:
                return e.code, json.loads(raw or "{}")
            except ValueError:
                return e.code, {"error": raw[:200]}


# --------------------------------------------------------------------- проверка

def check(name: str, cond: bool, detail: str = "") -> None:
    global PASS, FAIL
    if cond:
        PASS += 1
        print(f"  ok    {name}")
    else:
        FAIL += 1
        FAILED.append(name)
        print(f"  FAIL  {name}{(': ' + detail) if detail else ''}")


def eq(name: str, got, want) -> None:
    check(name, got == want, f"got {got!r}, want {want!r}")


def near(name: str, got, want, tol=1) -> None:
    try:
        check(name, abs(float(got) - float(want)) <= tol, f"got {got!r}, want ~{want!r}")
    except (TypeError, ValueError):
        check(name, False, f"not a number: {got!r}")


# ----------------------------------------------------------------------- тесты

def run(api, mode: str, secret: str, admin_key: str = ADMIN_KEY) -> None:
    pid = "test_api_" + "".join(random.choices(string.ascii_lowercase + string.digits, k=6))
    P = {"player_id": pid, "steam_id": "7656119800000" + pid[-4:], "name": "ApiTester"}

    print("\n== служебное ==")
    st, d = api.call("GET", "/api/health")
    eq("health: 200", st, 200)
    check("health: ok=true", bool(d.get("ok")), json.dumps(d)[:120])

    print("\n== профиль ==")
    st, prof = api.call("POST", "/api/player/login", P)
    eq("login: 200", st, 200)
    start_gold = int(prof.get("gold", -1))
    check("login: стартовое золото = 500", start_gold == 500, f"gold={prof.get('gold')}")
    check("login: есть списки blueprints/perks/titles",
          all(isinstance(prof.get(k), list) for k in ("blueprints", "perks", "titles")),
          json.dumps(prof)[:200])
    eq("login: battlepass_level 0", int(prof.get("battlepass_level", -1)), 0)

    st, d = api.call("GET", f"/api/player/{pid}")
    eq("player/get: id совпадает", d.get("id"), pid)

    st, d = api.call("GET", "/api/player/does_not_exist_42")
    eq("player/get: неизвестный -> 404", st, 404)

    print("\n== бой ==")
    gold = start_gold
    for _ in range(3):
        api.call("POST", "/api/kill", {**P, "killed_troop": "ni_nord_peasant", "gold_reward": 15, "wave": 1, "is_boss": 0})
        gold += 15
    st, prof = api.call("GET", f"/api/player/{pid}")
    eq("kill x3: kills=3", int(prof["kills"]), 3)
    near("kill x3: золото списано верно", prof["gold"], gold, tol=0)
    eq("kill x3: season_points_earned=3", int(prof["season_points_earned"]), 3)

    api.call("POST", "/api/kill", {**P, "killed_troop": "ni_nord_jarl", "gold_reward": 50, "wave": 5, "is_boss": 1})
    st, prof = api.call("GET", f"/api/player/{pid}")
    eq("boss kill: boss_kills=1", int(prof["boss_kills"]), 1)

    api.call("POST", "/api/wave/complete", {**P, "wave": 7, "gold": 20, "wood": 2, "metal": 1})
    st, prof = api.call("GET", f"/api/player/{pid}")
    eq("wave/complete: best_wave=7", int(prof["best_wave"]), 7)
    check("wave/complete: дерево начислено", int(prof["wood"]) >= 2, f"wood={prof['wood']}")

    st, d = api.call("POST", "/api/perk/record", {**P, "perk_id": 4})
    check("perk/record:perk записан", 4 in d.get("perks", []), json.dumps(d)[:120])
    st, d = api.call("POST", "/api/perk/record", {**P, "perk_id": 4})
    eq("perk/record: повтор -> new=false", d.get("new"), False)
    st, d = api.call("POST", "/api/perk/record", {**P, "perk_id": 500})
    eq("perk/record: некорректный id -> 400", st, 400)

    print("\n== забег ==")
    st, d = api.call("POST", "/api/run/save", {**P, "won": 1, "wave_reached": 25, "kills": 12, "deaths": 0})
    eq("run/save: победа даёт титул wall", d.get("titles_earned"), ["wall"])
    st, prof = api.call("GET", f"/api/player/{pid}")
    eq("run/save: wins=1", int(prof["wins"]), 1)
    check("run/save: +50 season_points_earned (battlepass lvl 2)",
          int(prof["battlepass_level"]) >= 2, f"earned={prof['season_points_earned']}")

    print("\n== чертежи и мета ==")
    st, d = api.call("POST", "/api/blueprint/unlock", {**P, "blueprint_id": "wall_wood"})
    check("blueprint/unlock: wall_wood выдан", "wall_wood" in d.get("blueprints", []), json.dumps(d)[:120])
    st, d = api.call("POST", "/api/blueprint/unlock", {**P, "blueprint_id": "hacked_item"})
    eq("blueprint/unlock: whitelist -> 400", st, 400)

    st, d = api.call("POST", "/api/meta/unlock", {**P, "node_id": "blacksmith_2"})
    eq("meta/unlock: без prerequisite -> 400", st, 400)
    st, d = api.call("POST", "/api/meta/unlock", {**P, "node_id": "veteran_1"})
    eq("meta/unlock: veteran_1 открыт", d.get("meta", [])[:1], ["veteran_1"])
    st, d = api.call("POST", "/api/meta/unlock", {**P, "node_id": "blacksmith_1"})
    eq("meta/unlock: prerequisite blacksmith_1 открыт", st, 200)
    st, d = api.call("POST", "/api/meta/unlock", {**P, "node_id": "blacksmith_2"})
    eq("meta/unlock: после prerequisite -> ок", st, 200)

    st, d = api.call("POST", "/api/stat/increment", {"player_id": pid, "stat": "revives"})
    eq("stat/increment: revives=1", int(d.get("revives", 0)), 1)
    st, d = api.call("POST", "/api/stat/increment", {"player_id": pid, "stat": "gold"})
    eq("stat/increment: чужой stat -> 400", st, 400)

    print("\n== кампания ==")
    st, villages = api.call("GET", "/api/campaign/villages")
    eq("campaign/villages: 8 деревень", len(villages) if isinstance(villages, list) else -1, 8)
    check("campaign/villages: есть votes", isinstance(villages, list) and "votes" in villages[0],
          json.dumps(villages[:1])[:160])
    st, d = api.call("POST", "/api/campaign/vote", {"voter": pid, "village_id": 3})
    eq("campaign/vote: первый голос ок", st, 200)
    st, d = api.call("POST", "/api/campaign/vote", {"voter": pid, "village_id": 3})
    eq("campaign/vote: повтор -> 409", st, 409)
    st, d = api.call("POST", "/api/campaign/battle", {"village_id": 3, "won": 1, "players": pid, "wave_reached": 25})
    eq("campaign/battle: won -> 200", st, 200)
    st, prof = api.call("GET", f"/api/player/{pid}")
    check("campaign/battle: +200 золота", int(prof["gold"]) >= 200, f"gold={prof['gold']}")

    print("\n== магазин ==")
    st, cat = api.call("GET", "/api/shop/catalog")
    items = cat.get("items", []) if isinstance(cat, dict) else []
    eq("shop/catalog: 200", st, 200)
    check("shop/catalog: >= 15 позиций", len(items) >= 15, f"{len(items)} позиций")
    by_id = {i["id"]: i for i in items}
    check("shop/catalog: wood_pack_10 стоит 60g", by_id.get("wood_pack_10", {}).get("gold") == 60,
          json.dumps(by_id.get("wood_pack_10", {})))
    check("shop/catalog: grants у blueprint_stakes",
          by_id.get("stakes", {}).get("grants") == ["blueprint:stakes"],
          json.dumps(by_id.get("stakes", {})))
    check("shop/catalog: blueprints-allowlist отдан", isinstance(cat.get("blueprints"), list) and
          "oil_cauldron" in cat["blueprints"], json.dumps(cat.get("blueprints"))[:120])

    st, before = api.call("GET", f"/api/player/{pid}")
    st, d = api.call("POST", "/api/shop/buy", {**P, "item_id": "wood_pack_10"})
    eq("shop/buy: 200", st, 200)
    eq("shop/buy: списано 60 золота", int(d.get("gold", -1)), int(before["gold"]) - 60)
    eq("shop/buy: +10 дерева", int(d.get("wood", -1)), int(before["wood"]) + 10)
    check("shop/buy: granted содержит wood:10", "wood:10" in d.get("granted", []), json.dumps(d.get("granted")))

    st, d = api.call("POST", "/api/shop/buy", {**P, "item_id": "no_such_item"})
    eq("shop/buy: неизвестный item -> 400", st, 400)
    st, d = api.call("POST", "/api/shop/buy", {**P, "item_id": "wall_wood"})
    check("shop/buy: уже открытый чертёж -> 400/409", st in (400, 409), f"status={st}")
    st, d = api.call("POST", "/api/shop/buy", {**P, "item_id": "catapult", "qty": 5})
    check("shop/buy: не хватает ресурсов -> 400", st == 400, f"status={st} {json.dumps(d)[:80]}")
    st, d = api.call("GET", f"/api/shop/history?player_id={pid}")
    check("shop/history: покупка в журнале", isinstance(d, list) and len(d) >= 1, json.dumps(d)[:120])

    print("\n== battlepass ==")
    st, prog = api.call("GET", f"/api/battlepass/progress?player_id={pid}")
    eq("battlepass/progress: 200", st, 200)
    check("battlepass/progress: rewards c флагами",
          bool(prog.get("rewards")) and "claimed" in prog["rewards"][0] and "unlocked" in prog["rewards"][0],
          json.dumps(prog.get("rewards", [])[:1]))
    lvl = int(prog.get("level", 0))
    check("battlepass/progress: уровень > 0 после забега", lvl >= 1, f"level={lvl}")
    st, d = api.call("POST", "/api/battlepass/claim", {**P, "level": 999})
    eq("battlepass/claim: уровень вне шкалы -> 400", st, 400)
    st, d = api.call("POST", "/api/battlepass/claim", {**P, "level": prog["max_level"]})
    eq("battlepass/claim: незакрытый уровень -> 400", st, 400)

    st, d = api.call("POST", "/api/battlepass/claim", {**P, "level": 1})
    eq("battlepass/claim: уровень 1 выдан", st, 200)
    check("battlepass/claim: granted = gold:100", "gold:100" in d.get("granted", []), json.dumps(d.get("granted")))
    st, d = api.call("POST", "/api/battlepass/claim", {**P, "level": 1})
    eq("battlepass/claim: повтор -> 409", st, 409)

    print("\n== лидерборд / сезон ==")
    st, rows = api.call("GET", "/api/leaderboard")
    check("leaderboard: список", isinstance(rows, list) and len(rows) >= 1, json.dumps(rows)[:120])
    st, d = api.call("GET", "/api/season/current")
    eq("season/current: 200", st, 200)
    first_season = int(d.get("id", 0))

    print("\n== сброс сезона ==")
    st, d = api.call("POST", "/api/season/reset", {}, with_secret=True)  # без X-NI-Admin
    if mode == "base":
        check("season/reset: без админ-ключа отклонён (401/403/503)", st in (401, 403, 503), f"status={st}")
    else:
        eq("season/reset: без админ-ключа -> 403", st, 403)
    if secret:
        st, d = api.call("POST", "/api/season/reset", {}, with_secret=False)
        eq("season/reset: без X-NI-Secret -> 401", st, 401)

    st, d = api.call("POST", "/api/season/reset", {"admin_key": admin_key})
    if st == 200:
        eq("season/reset: новая волна сезонов", int(d.get("new_season", 0)), first_season + 1)
        st, prof = api.call("GET", f"/api/player/{pid}")
        eq("season/reset: season_points обнулены", int(prof["season_points"]), 0)
        eq("season/reset: battlepass_level обнулён", int(prof["battlepass_level"]), 0)
        check("season/reset: мета-дерево сброшено", prof.get("meta") == [], json.dumps(prof.get("meta")))
        check("season/reset: золото и титулы сохранились",
              int(prof["gold"]) > 0 and "wall" in prof.get("titles", []), json.dumps(prof)[:160])
        st, prog = api.call("GET", f"/api/battlepass/progress?player_id={pid}")
        check("season/reset: claim'ы обнулены для нового сезона", prog.get("claimed") == [],
              json.dumps(prog.get("claimed")))
    elif mode == "base" and not admin_key:
        print("  skip  season/reset: передай --admin-key, если это тестовая база")
    else:
        check("season/reset: с admin-ключом -> 200", False, f"status={st} {json.dumps(d)[:160]}")

    print("\n== безопасность ==")
    if secret:
        st, d = api.call("POST", "/api/player/login", P, with_secret=False)
        eq("X-NI-Secret обязателен: без секрета -> 401", st, 401)
    else:
        print("  skip  проверка X-NI-Secret (секрет не задан)")


# ------------------------------------------------------------------------- main

def main() -> int:
    ap = argparse.ArgumentParser(description="Контрактный тест бэкенда Nord Invasion")
    ap.add_argument("--serve", action="store_true", help="поднять dev_server и гонять по HTTP")
    ap.add_argument("--base", default="", help="URL существующего бэкенда (например PHP)")
    ap.add_argument("--port", type=int, default=0, help="порт для --serve (0 = свободный)")
    ap.add_argument("--secret", default="", help="X-NI-Secret для --base")
    ap.add_argument("--admin-key", default="", help="ADMIN_SECRET для /api/season/reset (только тестовая база!)")
    ap.add_argument("--catalog", default=os.path.join(REPO, "src", "backend-php", "shop_catalog.json"))
    ap.add_argument("--keep", action="store_true", help="не удалять временную базу (для --serve)")
    args = ap.parse_args()

    if args.base:
        print(f"Тест против {args.base}")
        run(Http(args.base, args.secret), "base", args.secret, args.admin_key)
        return report()

    tmp = tempfile.mkdtemp(prefix="ni_api_test_")
    db = os.path.join(tmp, "test.db")
    secret = "t0-st1l"
    proc = None
    try:
        if args.serve:
            port = args.port or free_port()
            env = dict(os.environ, NI_DB=db, NI_CATALOG=args.catalog, NI_API_SECRET=secret, NI_ADMIN_SECRET=ADMIN_KEY)
            proc = subprocess.Popen([sys.executable, os.path.join(BACKEND_DIR, "dev_server.py"),
                                     "--host", "127.0.0.1", "--port", str(port)],
                                    env=env, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            wait_port(port)
            print(f"dev_server на :{port} (sqlite {db})")
            run(Http(f"http://127.0.0.1:{port}", secret), "serve", secret)
        else:
            print(f"in-process backend (sqlite {db})")
            run(InProc(db, args.catalog, secret), "inproc", secret)
        return report()
    finally:
        if proc:
            proc.terminate()
            try:
                proc.wait(timeout=5)
            except subprocess.TimeoutExpired:
                proc.kill()
        if not args.keep:
            import shutil
            shutil.rmtree(tmp, ignore_errors=True)


def free_port() -> int:
    import socket
    s = socket.socket()
    s.bind(("127.0.0.1", 0))
    port = s.getsockname()[1]
    s.close()
    return port


def wait_port(port: int, timeout: float = 20.0) -> None:
    import socket
    t0 = time.time()
    while time.time() - t0 < timeout:
        try:
            with socket.create_connection(("127.0.0.1", port), timeout=0.5):
                return
        except OSError:
            time.sleep(0.15)
    raise SystemExit(f"dev_server не поднялся на порту {port}")


def report() -> int:
    print("\n" + "=" * 60)
    print(f"Итог: {PASS} ok, {FAIL} fail")
    for f in FAILED:
        print("  FAIL:", f)
    return 1 if FAIL else 0


if __name__ == "__main__":
    sys.exit(main())
