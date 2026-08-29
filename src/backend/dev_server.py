"""
Nord Invasion - dev-бэкенд на чистом stdlib (http.server + sqlite3).

Ноль зависимостей: `python3 src/backend/dev_server.py` - и API доступен
на http://127.0.0.1:8080. Контракт идентичен PHP-бэкенду (src/backend-php),
поэтому один и тот же тест проходит против обоих:
    python3 tools/test_backend_api.py                       # in-process (быстро)
    python3 tools/test_backend_api.py --serve               # через HTTP
    python3 tools/test_backend_api.py --base http://host    # против PHP

Переменные окружения:
    NI_DB          путь к sqlite-файлу       (по умолчанию src/backend/ni_dev.db)
    NI_CATALOG     путь к shop_catalog.json  (по умолчанию ../backend-php/shop_catalog.json)
    NI_API_SECRET  если задан - все запросы обязаны нести X-NI-Secret
    NI_ADMIN_SECRET если задан - разрешает POST /api/season/reset с X-NI-Admin
"""

from __future__ import annotations

import argparse
import json
import os
import sys
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qsl, parse_qs, urlparse

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import nidb  # noqa: E402


def read_fields(handler: BaseHTTPRequestHandler) -> dict:
    """Поля запроса: query-string + тело (form-urlencoded или JSON)."""
    query = {k: v[-1] for k, v in parse_qs(urlparse(handler.path).query).items()}
    length = int(handler.headers.get("Content-Length") or 0)
    body = handler.rfile.read(length).decode("utf-8", "replace") if length else ""
    fields = dict(query)
    if body:
        ctype = (handler.headers.get("Content-Type") or "").lower()
        if "json" in ctype or body.lstrip().startswith("{"):
            try:
                data = json.loads(body)
                if isinstance(data, dict):
                    fields.update({k: v for k, v in data.items()})
            except ValueError:
                pass
        else:
            fields.update({k: v for k, v in parse_qsl(body, keep_blank_values=True)})
    return fields


class Handler(BaseHTTPRequestHandler):
    server_version = "NordInvasionDev/2.1"

    def log_message(self, fmt: str, *args) -> None:  # компактнее, чем у BaseHTTPRequestHandler
        sys.stderr.write("%s - %s\n" % (self.address_string(), fmt % args))

    def _write(self, status: int, payload) -> None:
        data = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(data)))
        self.send_header("Access-Control-Allow-Origin", "*")  # для веб-дашборда/лидерборда
        self.end_headers()
        self.wfile.write(data)

    def do_OPTIONS(self) -> None:  # CORS preflight
        self.send_response(204)
        self.send_header("Access-Control-Allow-Headers", "Content-Type,X-NI-Secret,X-NI-Admin")
        self.send_header("Access-Control-Allow-Methods", "GET,POST,OPTIONS")
        self.send_header("Access-Control-Allow-Origin", "*")
        self.end_headers()

    def _run(self, method: str) -> None:
        headers = {k.lower(): v for k, v in self.headers.items()}
        try:
            status, payload = nidb.handle(method, urlparse(self.path).path, read_fields(self), headers)
        except nidb.ApiError as e:
            status, payload = e.status, {"error": e.message}
        except Exception as e:  # не роняем сервер: dev-бэкенд обязан отвечать JSON
            status, payload = 500, {"error": f"{type(e).__name__}: {e}"}
        self._write(status, payload)

    def do_GET(self) -> None:
        self._run("GET")

    def do_POST(self) -> None:
        self._run("POST")


def main() -> int:
    ap = argparse.ArgumentParser(description="Nord Invasion dev backend (stdlib, sqlite)")
    ap.add_argument("--host", default="0.0.0.0")
    ap.add_argument("--port", type=int, default=8080)
    ap.add_argument("--db", default="", help="путь к sqlite (переопределяет NI_DB)")
    ap.add_argument("--catalog", default="", help="путь к shop_catalog.json (переопределяет NI_CATALOG)")
    ap.add_argument("--reset", action="store_true", help="удалить базу перед стартом")
    args = ap.parse_args()

    if args.db:
        nidb.DB_PATH = args.db
        os.environ["NI_DB"] = args.db
    if args.catalog:
        os.environ["NI_CATALOG"] = args.catalog
        nidb.CATALOG_PATH = args.catalog
        nidb.load_catalog(args.catalog)
    if args.reset and os.path.exists(nidb.DB_PATH):
        os.remove(nidb.DB_PATH)

    nidb.init_db()
    srv = ThreadingHTTPServer((args.host, args.port), Handler)
    print(f"Nord Invasion dev backend: http://{args.host}:{args.port}/api/health")
    print(f"  db      = {nidb.DB_PATH}")
    print(f"  catalog = {nidb.CATALOG_PATH} ({'ok' if nidb.HAVE_CATALOG else 'MISSING'})")
    print(f"  secret  = {'включён (NI_API_SECRET)' if nidb.API_SECRET else 'выключен'}")
    try:
        srv.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        srv.server_close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
