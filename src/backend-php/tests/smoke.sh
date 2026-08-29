#!/usr/bin/env bash
# Smoke-тест Nord Invasion Backend (PHP + MySQL).
# Использование: bash tests/smoke.sh http://nordinv.example.com [secret]
set -u

BASE="${1:-http://localhost:8080}"
SECRET="${2:-}"
H=(-s -w "\n%{http_code}")
[ -n "$SECRET" ] && H+=(-H "X-NI-Secret: $SECRET")

PASS=0; FAIL=0
ok()   { echo "OK    $1"; PASS=$((PASS+1)); }
bad()  { echo "FAIL  $1: $2"; FAIL=$((FAIL+1)); }

# запрос, ожидаем 200 без "error"
req() { # имя, путь, data
  local out code body
  out=$(curl "${H[@]}" -d "$3" "$BASE/api/$2")
  code="${out##*$'\n'}"; body="${out%$'\n'*}"
  if [ "$code" = "200" ] && ! echo "$body" | grep -q '"error"'; then
    ok "$1"
  else
    bad "$1 (http $code)" "$body"
  fi
}

# запрос, ожидаем 400 (неизвестный предмет/чертёж/нехватка ресурсов)
req400() {
  local out code body
  out=$(curl "${H[@]}" -d "$3" "$BASE/api/$2")
  code="${out##*$'\n'}"; body="${out%$'\n'*}"
  if [ "$code" = "400" ]; then ok "$1 (400 как ожидается)"
  else bad "$1 (ожидали 400, было $code)" "$body"; fi
}

get() {
  local out code body
  out=$(curl "${H[@]}" "$BASE/api/$2")
  code="${out##*$'\n'}"; body="${out%$'\n'*}"
  if [ "$code" = "200" ] && ! echo "$body" | grep -q '"error"'; then ok "$1"
  else bad "$1 (http $code)" "$body"; fi
}

P="player_id=test_smoke_1&steam_id=76561110000000001&name=SmokeTester"

echo "=== Nord Invasion Backend smoke: $BASE ==="

out=$(curl "${H[@]}" "$BASE/health"); code="${out##*$'\n'}"
[ "$code" = "200" ] && ok "health" || bad "health (http $code)" "$out"

req  "player/login"    "player/login" "$P"
get  "player/get"      "player/test_smoke_1"
req  "kill"            "kill" "$P&killed_troop=ni_nord_peasant&gold_reward=15&wood=1&metal=0&wave=1&is_boss=0"
req  "kill boss"       "kill" "$P&killed_troop=ni_nord_jarl&gold_reward=50&wood=0&metal=2&wave=5&is_boss=1"
req  "wave/complete"   "wave/complete" "$P&wave=1&gold=20&wood=1&metal=0&perk_id=0"
req  "run/save win"    "run/save" "$P&won=1&wave_reached=25&kills=10&deaths=0"
req  "blueprint/unlock" "blueprint/unlock" "$P&blueprint_id=wall_wood"
req400 "blueprint/bad"  "blueprint/unlock" "$P&blueprint_id=hacked"
req  "meta/unlock"     "meta/unlock" "$P&node_id=veteran_1"
req  "stat/revives"    "stat/increment" "player_id=test_smoke_1&stat=revives"
req  "stat/builds"     "stat/increment" "player_id=test_smoke_1&stat=builds"
get  "campaign/villages" "campaign/villages"
req  "campaign/vote"   "campaign/vote" "voter=test_smoke_1&village_id=0"
req  "campaign/battle" "campaign/battle" "village_id=0&won=1&players=test_smoke_1&wave_reached=5"
get  "season/current"  "season/current"
get  "leaderboard"     "leaderboard"
get  "battlepass/rewards" "battlepass/rewards"

# --- магазин (каталог из shop_catalog.json) ---
get  "shop/catalog" "shop/catalog"
req  "shop/buy wood_pack_10" "shop/buy" "$P&item_id=wood_pack_10"
req400 "shop/buy unknown"     "shop/buy" "$P&item_id=hacked_item"
req  "shop/buy blueprint"     "shop/buy" "$P&item_id=brazier"
out=$(curl "${H[@]}" -d "$P&item_id=brazier" "$BASE/api/shop/buy")
code="${out##*$'\n'}"
case "$code" in
  409|400) ok "shop/buy blueprint повторно -> $code (не даёт купить дважды)";;
  *)       bad "shop/buy blueprint повторно (ожидали 400/409)" "$out";;
esac
req400 "shop/buy no money"    "shop/buy" "player_id=test_smoke_nomoney&item_id=catapult&qty=5"
get  "shop/history" "shop/history?player_id=test_smoke_1"

# --- battlepass: прогресс / выдача / идемпотентность ---
get  "battlepass/progress" "battlepass/progress?player_id=test_smoke_1"
out=$(curl "${H[@]}" -d "player_id=test_smoke_1&level=1" "$BASE/api/battlepass/claim")
code="${out##*$'\n'}"; body="${out%$'\n'*}"
case "$code" in
  200) ok "battlepass/claim lvl1 (200)";;
  400) ok "battlepass/claim lvl1 (400: уровень ещё не открыт - ок для чистого БД)";;
  *)   bad "battlepass/claim (ожидали 200/400)" "$body";;
esac
out=$(curl "${H[@]}" -d "player_id=test_smoke_1&level=999" "$BASE/api/battlepass/claim")
code="${out##*$'\n'}"
[ "$code" = "400" ] && ok "battlepass/claim bad level (400)" || bad "battlepass/claim bad level (ожидали 400, было $code)"

# --- сброс сезона (админский, по умолчанию выключен) ---
out=$(curl "${H[@]}" -d "admin_key=wrong" "$BASE/api/season/reset")
code="${out##*$'\n'}"
case "$code" in
  503) ok "season/reset: ADMIN_SECRET не задан -> 503 (как и должно быть на проде по умолчанию)";;
  403) ok "season/reset: неверный ключ отклонён (403)";;
  200) ok "season/reset: выполнен (проверь, что это тестовая база!)";;
  *)   bad "season/reset (ожидали 200/403/503)" "$out";;
esac

echo "=== Итог: $PASS ok, $FAIL fail ==="
[ "$FAIL" -eq 0 ]
