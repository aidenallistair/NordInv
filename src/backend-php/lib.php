<?php
/**
 * Общие утилиты: PDO-подключение, JSON-ответы, авторизация, чтение тела.
 * Требует PHP 7.4+ с pdo_mysql (или pdo_sqlite для dev).
 */

require_once __DIR__ . '/config.php';

/** @return PDO */
function db(): PDO
{
    static $pdo = null;
    if ($pdo === null) {
        if (DB_DRIVER === 'sqlite') {
            $dsn = 'sqlite:' . DB_PATH;
            $pdo = new PDO($dsn, null, null, [
                PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
                PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            ]);
            $pdo->exec('PRAGMA foreign_keys = ON');
        } else {
            $dsn = sprintf('mysql:host=%s;port=%s;dbname=%s;charset=%s', DB_HOST, DB_PORT, DB_NAME, DB_CHARSET);
            $pdo = new PDO($dsn, DB_USER, DB_PASS, [
                PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
                PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
                PDO::ATTR_EMULATE_PREPARES => false,
            ]);
        }
    }
    return $pdo;
}

/** JSON-ответ и выход. */
function out(array $data, int $code = 200): void
{
    http_response_code($code);
    header('Content-Type: application/json; charset=utf-8');
    echo json_encode($data, JSON_UNESCAPED_SLASHES);
    exit;
}

/** Ошибка (JSON). */
function fail(string $msg, int $code = 400): void
{
    $data = ['error' => $msg];
    if (X_DEBUG) $data['debug'] = debug_backtrace(DEBUG_BACKTRACE_IGNORE_ARGS, 5);
    out($data, $code);
}

/** Поле запроса: POST form/JSON + query, со дефолтом. */
function in_(string $key, $default = null)
{
    if (isset($_POST[$key])) return $_POST[$key];
    $body = json_decode(file_get_contents('php://input'), true);
    if (is_array($body) && array_key_exists($key, $body)) return $body[$key];
    if (isset($_GET[$key])) return $_GET[$key];
    return $default;
}

/** Обязательное поле. */
function req(string $key): string
{
    $v = in_($key);
    if ($v === null || $v === '') fail('missing field: ' . $key);
    return (string)$v;
}

/** Целое поле (0 по умолчанию). */
function in_int(string $key, int $default = 0): int
{
    $v = in_($key);
    return ($v === null || $v === '') ? $default : (int)$v;
}

/** Проверка X-NI-Secret (если API_SECRET задан). */
function check_secret(): void
{
    if (API_SECRET === '') return;
    $h = function_exists('getallheaders') ? getallheaders() : [];
    $got = '';
    foreach ($h as $k => $v) {
        if (strtolower($k) === 'x-ni-secret') { $got = $v; break; }
    }
    if ($got === '') $got = (string)getenv('HTTP_X_NI_SECRET');
    if (!hash_equals(API_SECRET, $got)) fail('unauthorized', 401);
}

/**
 * Идентификатор игрока: player_id (если прислан клиентом - используем как есть),
 * иначе steam_id -> "steam_<id>", иначе name -> "name_<md5>".
 * Клиент (C#) шлёт player_id, вычисленный той же формулой (NIPeers.MakePlayerId).
 */
function player_identity(): array
{
    $steam = trim((string)in_('steam_id', ''));
    $name = trim((string)in_('name'));
    if ($name === '') $name = trim((string)in_('player_name', '')); // совместимость
    $id = trim((string)in_('player_id', ''));
    if ($id === '') $id = $steam !== '' ? 'steam_' . $steam : ($name !== '' ? 'name_' . md5($name) : '');
    if ($id === '') fail('missing player identity');
    return ['id' => $id, 'steam' => $steam, 'name' => $name];
}

/** Профиль игрока из строки БД (с индексами по именам колонок). */
function profile_row(array $r): array
{
    $earned = (int)($r['season_points_earned'] ?? 0);
    return [
        'id' => $r['id'],
        'steam_id' => $r['steam_id'],
        'name' => $r['peer_name'],
        'gold' => (int)$r['gold'],
        'wood' => (int)$r['wood'],
        'metal' => (int)$r['metal'],
        'kills' => (int)$r['kills'],
        'deaths' => (int)$r['deaths'],
        'level' => (int)$r['level'],
        'xp' => (int)$r['xp'],
        'season_points' => (int)$r['season_points'],
        'season_points_earned' => $earned,
        'battlepass_level' => bp_level_from($earned),
        'cosmetics' => json_decode((string)($r['cosmetics'] ?? '[]'), true) ?: [],
        'wins' => (int)$r['wins'],
        'losses' => (int)$r['losses'],
        'best_wave' => (int)$r['best_wave'],
        'revives' => (int)$r['revives'],
        'boss_kills' => (int)$r['boss_kills'],
        'builds' => (int)$r['builds'],
        'blueprints' => json_decode((string)($r['blueprints'] ?? '[]'), true) ?: [],
        'perks' => json_decode((string)($r['perks'] ?? '[]'), true) ?: [],
        'meta' => json_decode((string)($r['meta'] ?? '[]'), true) ?: [],
        'titles' => json_decode((string)($r['titles'] ?? '[]'), true) ?: [],
    ];
}

/** Найти игрока по id/steam, при желании создать. */
function find_player(PDO $pdo, string $id, string $steam, string $name, bool $create = true): ?array
{
    $st = $pdo->prepare('SELECT * FROM players WHERE id = ? OR (steam_id <> "" AND steam_id = ?) LIMIT 1');
    $st->execute([$id, $steam]);
    $row = $st->fetch();
    if ($row) {
        if ($name !== '') {
            $u = $pdo->prepare('UPDATE players SET peer_name = ?, last_seen = ? WHERE id = ?');
            $u->execute([$name, time(), $row['id']]);
            $row['peer_name'] = $name;
        }
        return $row;
    }
    if (!$create) return null;
    $ins = $pdo->prepare('INSERT INTO players (id, steam_id, peer_name, gold, last_seen, created_at) VALUES (?, ?, ?, ?, ?, ?)');
    $ins->execute([$id, $steam, $name, START_GOLD, time(), time()]);
    return find_player($pdo, $id, $steam, $name, false);
}

/** XP -> уровень (level up когда xp >= level*100), возвращает новый уровень/xp. */
function apply_xp(array $row, int $gain): array
{
    $xp = (int)$row['xp'] + $gain;
    $level = (int)$row['level'];
    while ($xp >= $level * 100) {
        $xp -= $level * 100;
        $level++;
    }
    return [$level, $xp];
}

/* =========================================================================
 * Каталог магазина, BattlePass, выдача наград, сезон
 * (все списки/цены - из shop_catalog.json; C# сверяется tools/validate_module.py)
 * ========================================================================= */

/** Каталог доступен? (иначе /api/shop/* отвечает 503) */
function have_catalog(): bool
{
    return HAS_CATALOG && count(SHOP_ITEMS) > 0;
}

/** Позиция каталога по id (null, если нет). */
function shop_item(string $id): ?array
{
    return SHOP_ITEM_MAP[$id] ?? null;
}

/** Награды позиции: явные grants + ресурсы, уплаченные как "скидка" не возвращаются. */
function item_grants(array $item): array
{
    $grants = array_values(array_map('strval', $item['grants'] ?? []));
    return $grants;
}

/** "wood:10" -> ["wood", 10]; "ammo" -> ["ammo", 0]. */
function parse_grant(string $g): array
{
    $parts = explode(':', $g, 2);
    $kind = strtolower(trim($parts[0]));
    $value = isset($parts[1]) ? (int)trim($parts[1]) : 0;
    return [$kind, $value];
}

/** Граниды: что обрабатывает сервер, а что отдаём клиенту (heal/ammo/repair). */
function grant_is_server_side(string $kind): bool
{
    return in_array($kind, ['gold', 'wood', 'metal', 'blueprint', 'title', 'skin', 'season_points'], true);
}

/**
 * Применяет награды к профику на сервере.
 * Возвращает ['applied' => [...], 'balances' => ['gold'=>..,'wood'=>..,'metal'=>..,'season_points'=>..]]
 * Клиент сам применяет heal/ammo/repair (они проксируются в 'applied').
 */
function apply_grants(PDO $pdo, string $playerId, array $grants): array
{
    $dGold = $dWood = $dMetal = $dSp = 0;
    $newBlueprints = $newTitles = $newCosmetics = [];
    $applied = [];

    foreach ($grants as $g) {
        [$kind, $value] = parse_grant((string)$g);
        if ($kind === '') continue;

        switch ($kind) {
            case 'gold':  $dGold  += max(0, $value); $applied[] = 'gold:' . max(0, $value); break;
            case 'wood':  $dWood  += max(0, $value); $applied[] = 'wood:' . max(0, $value); break;
            case 'metal': $dMetal += max(0, $value); $applied[] = 'metal:' . max(0, $value); break;
            case 'season_points':
                $dSp += max(0, $value);
                $applied[] = 'season_points:' . max(0, $value);
                break;

            case 'blueprint': {
                $bid = substr(trim((string)(explode(':', $g, 2)[1] ?? '')), 0, 128);
                if ($bid === '' || !in_array($bid, BLUEPRINTS, true)) fail('unknown blueprint: ' . $bid, 400);
                if (!in_array($bid, $newBlueprints, true)) {
                    $newBlueprints[] = $bid;
                    $applied[] = 'blueprint:' . $bid;
                }
                break;
            }
            case 'title': {
                $tid = substr(trim((string)(explode(':', $g, 2)[1] ?? '')), 0, 64);
                if ($tid === '') break;
                if (!in_array($tid, $newTitles, true)) {
                    $newTitles[] = $tid;
                    $applied[] = 'title:' . $tid;
                }
                break;
            }
            case 'skin': {
                $sid = substr(trim((string)(explode(':', $g, 2)[1] ?? '')), 0, 64);
                if ($sid === '') break;
                if (!in_array($sid, $newCosmetics, true)) {
                    $newCosmetics[] = $sid;
                    $applied[] = 'skin:' . $sid;
                }
                break;
            }
            default:
                // heal:N / ammo / repair:N - игровые сервисы, применяет клиент
                if (!grant_is_server_side($kind)) $applied[] = trim((string)$g);
                break;
        }
    }

    $st = $pdo->prepare('SELECT gold, wood, metal, blueprints, titles, cosmetics, season_points FROM players WHERE id = ?');
    $st->execute([$playerId]);
    $row = $st->fetch();
    if (!$row) fail('player vanished', 500);

    $bps = json_decode((string)($row['blueprints'] ?: '[]'), true) ?: [];
    foreach ($newBlueprints as $b) if (!in_array($b, $bps, true)) $bps[] = $b;
    $titles = json_decode((string)($row['titles'] ?: '[]'), true) ?: [];
    foreach ($newTitles as $t) if (!in_array($t, $titles, true)) $titles[] = $t;
    $cosm = json_decode((string)($row['cosmetics'] ?: '[]'), true) ?: [];
    foreach ($newCosmetics as $c) if (!in_array($c, $cosm, true)) $cosm[] = $c;

    $up = $pdo->prepare('UPDATE players SET gold = CASE WHEN gold + ? < 0 THEN 0 ELSE gold + ? END,
        wood = wood + ?, metal = metal + ?, season_points = season_points + ?, season_points_earned = season_points_earned + ?,
        blueprints = ?, titles = ?, cosmetics = ? WHERE id = ?');
    $up->execute([$dGold, $dGold, $dWood, $dMetal, $dSp, $dSp,
        json_encode($bps), json_encode($titles), json_encode($cosm), $playerId]);

    return [
        'applied' => $applied,
        'balances' => [
            'gold' => max(0, (int)$row['gold'] + $dGold),
            'wood' => (int)$row['wood'] + $dWood,
            'metal' => (int)$row['metal'] + $dMetal,
            'season_points' => (int)$row['season_points'] + $dSp,
            'blueprints' => $bps,
            'titles' => $titles,
            'cosmetics' => $cosm,
        ],
    ];
}

/** BattlePass-уровень по ЗАРАБОТАННЫМ (не потраченным) очкам сезона. */
function bp_level_from(int $seasonPointsEarned): int
{
    $lvl = intdiv(max(0, $seasonPointsEarned), max(1, BP_POINTS_PER_LEVEL));
    return min(BP_MAX_LEVEL, $lvl);
}

/** Уже полученные уровни battlepass в текущем сезоне. */
function claimed_levels(PDO $pdo, string $playerId, int $season): array
{
    $st = $pdo->prepare('SELECT level FROM battlepass_claims WHERE player_id = ? AND season = ?');
    $st->execute([$playerId, $season]);
    return array_map('intval', array_column($st->fetchAll(), 'level'));
}

/** Начисления season_points: всегда парно растят баланс и "заработано" (для BattlePass). */
function credit_sql(): string
{
    return 'season_points = season_points + ?, season_points_earned = season_points_earned + ?';
}

/** Строка battlepass_rewards -> grant-токен (тот же grammar, что и grants в каталоге). */
function reward_to_grant(array $reward): string
{
    $type = (string)$reward['reward_type'];
    $id = (string)$reward['reward_id'];
    switch ($type) {
        case 'gold':
        case 'wood':
        case 'metal':
        case 'season_points':
            return $type . ':' . (int)$id;
        case 'blueprint':
            if (!in_array($id, BLUEPRINTS, true)) fail('unknown blueprint in battlepass: ' . $id, 500);
            return 'blueprint:' . $id;
        case 'title':
            return 'title:' . $id;
        case 'skin':
            return 'skin:' . $id;
        default:
            fail('unknown reward type: ' . $type, 500);
    }
}

/** Админский доступ (season/reset): заголовок X-NI-Admin или поле admin_key. */
function check_admin(): void
{
    if (ADMIN_SECRET === '') fail('season reset disabled: set ADMIN_SECRET in config.php', 503);
    $got = '';
    foreach ((function_exists('getallheaders') ? getallheaders() : []) as $k => $v) {
        if (strtolower((string)$k) === 'x-ni-admin') { $got = (string)$v; break; }
    }
    if ($got === '') $got = (string)in_('admin_key', '');
    if (!hash_equals(ADMIN_SECRET, $got)) fail('forbidden', 403);
}
