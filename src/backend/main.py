"""
Nord Invasion - dev-бэкенд на FastAPI (опциональная обёртка над nidb).

Логика целиком в `nidb.py` - там же, где у stdlib-сервера `dev_server.py`,
поэтому два входа не расходятся. Нужен только HTTP-фреймворк:

    pip install -r requirements.txt
    uvicorn main:app --host 0.0.0.0 --port 8080          # из папки src/backend
    python3 -m uvicorn src.backend.main:app --port 8080  # из корня репо (нужен __init__)

Если зависимости ставить не хочется - `python3 src/backend/dev_server.py` (stdlib).

Переменные окружения: NI_DB, NI_CATALOG, NI_API_SECRET, NI_ADMIN_SECRET.
"""

from __future__ import annotations

import os
import sys

from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import nidb  # noqa: E402

app = FastAPI(title="Nord Invasion Better Edition Backend (dev)", version="2.1")
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["GET", "POST", "OPTIONS"],
    allow_headers=["Content-Type", "X-NI-Secret", "X-NI-Admin"],
)

nidb.init_db()


async def _fields(request: Request) -> dict:
    """Query + тело: form-urlencoded или JSON (моод отдаёт form - как PHP-бэкенд)."""
    fields: dict = {k: v for k, v in request.query_params.items()}
    body = await request.body()
    if body:
        ctype = (request.headers.get("content-type") or "").lower()
        text = body.decode("utf-8", "replace")
        if "json" in ctype or text.lstrip().startswith("{"):
            import json
            try:
                data = json.loads(text)
                if isinstance(data, dict):
                    fields.update(data)
            except ValueError:
                pass
        else:
            from urllib.parse import parse_qsl
            fields.update({k: v for k, v in parse_qsl(text, keep_blank_values=True)})
    return fields


@app.get("/{path:path}")
async def _get(request: Request, path: str) -> JSONResponse:
    return await _call(request, "GET", path)


@app.post("/{path:path}")
async def _post(request: Request, path: str) -> JSONResponse:
    return await _call(request, "POST", path)


@app.get("/")
async def _root(request: Request) -> JSONResponse:
    return await _call(request, "GET", "")


async def _call(request: Request, method: str, path: str) -> JSONResponse:
    headers = {k.lower(): v for k, v in request.headers.items()}
    try:
        status, payload = nidb.handle(method, "/" + path.lstrip("/"), await _fields(request), headers)
    except nidb.ApiError as e:
        status, payload = e.status, {"error": e.message}
    except Exception as e:  # dev-бэкенд всегда отвечает JSON
        status, payload = 500, {"error": f"{type(e).__name__}: {e}"}
    return JSONResponse(payload, status_code=status)


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=int(os.environ.get("NI_PORT", "8000")))
