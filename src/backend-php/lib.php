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
        'battlepass_level' => (int)$r['battlepass_level'],
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
    $ins = $pdo->prepare('INSERT INTO players (id, steam_id, peer_name, gold, last_seen, created_at) VALUES (?, ?, ?, 500, ?, ?)');
    $ins->execute([$id, $steam, $name, time(), time()]);
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
