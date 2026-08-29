<?php
/**
 * Nord Invasion Better Edition - конфигурация бэкенда (PHP + MySQL).
 *
 * Копируй в продакшн и меняй значения. Для dev-теста без MySQL:
 *   DB_DRIVER = "sqlite", DB_PATH = "/tmp/ni_test.db"
 */

// mysql - продакшн (dedicated server host), sqlite - локальный dev-тест
define('DB_DRIVER', 'mysql');            // mysql | sqlite
define('DB_HOST', '127.0.0.1');
define('DB_PORT', '3306');
define('DB_NAME', 'nordinv');
define('DB_USER', 'nordinv');
define('DB_PASS', 'CHANGE_ME');
define('DB_CHARSET', 'utf8mb4');
define('DB_PATH', __DIR__ . '/ni_local.db'); // используется при DB_DRIVER=sqlite

// Совместный секрет: если не пустой, все запросы должны приходить
// с заголовком X-NI-Secret: <API_SECRET> (C# PersistenceManager отправляет).
// Пустой = проверки нет (локальный dev).
define('API_SECRET', '');

// Секрет администратора: нужен для /api/season/reset (сброс сезона).
// Если пусто - маршрут отвечает 503 (сброс отключён).
define('ADMIN_SECRET', '');

// Каталог магазина / сезонные константы - единый источник истины для
// PHP, Python (src/backend/nidb.py) и C# (Models/ShopCatalog.cs).
// Кладётся рядом с index.php при деплое (cp -r src/backend-php/. /var/www/nordinv/).
// NI_CATALOG позволяет указать другой путь (общий каталог на несколько инстансов).
define('CATALOG_FILE', getenv('NI_CATALOG') ?: (__DIR__ . '/shop_catalog.json'));

// Fallback-allowlist чертежей, если JSON-каталог не найден (например, файл
// не скопировали при деплое). price-list тогда недоступен: /api/shop/* = 503.
define('BLUEPRINTS_FALLBACK', [
    'wall_wood', 'wall_door', 'oil_cauldron', 'ballista', 'catapult',
    'stakes', 'brazier', 'shield_wall', 'rock_trap', 'oil_ditch',
    'log_trap', 'spike_trap',
]);

$_NI_CATALOG = null;
if (is_readable(CATALOG_FILE)) {
    $_decoded = json_decode((string)file_get_contents(CATALOG_FILE), true);
    if (is_array($_decoded)) $_NI_CATALOG = $_decoded;
}
define('HAS_CATALOG', $_NI_CATALOG !== null);

$niBlueprints = $_NI_CATALOG['blueprint_ids'] ?? BLUEPRINTS_FALLBACK;
sort($niBlueprints);
define('BLUEPRINTS', $niBlueprints);                       // allowlist выдаваемых чертежей
define('SHOP_ITEMS', array_values($_NI_CATALOG['items'] ?? []));
define('SHOP_ITEM_MAP', array_column(SHOP_ITEMS, null, 'id'));
define('BATTLEPASS_REWARDS', array_values($_NI_CATALOG['battlepass'] ?? []));
define('BP_POINTS_PER_LEVEL', (int)($_NI_CATALOG['bp_points_per_level'] ?? 25));
define('BP_MAX_LEVEL', (int)($_NI_CATALOG['bp_max_level'] ?? 20));
define('START_GOLD', (int)($_NI_CATALOG['new_player_gold'] ?? 500));

// Сетевые заголовки/ответы
define('JSON_ERRORS', true); // true - ошибки как JSON (не HTML)
define('X_DEBUG', false);     // true - в JSON ответов будут детали ошибок

date_default_timezone_set('UTC');
