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

// Allowlist: что можно выдать/снять (защита от произвольных id)
define('BLUEPRINTS', [
    'wall_wood', 'wall_door', 'oil_cauldron', 'ballista', 'catapult',
    'stakes', 'brazier', 'shield_wall', 'rock_trap', 'oil_ditch',
]);

// Сетевые заголовки/ответы
define('JSON_ERRORS', true); // true - ошибки как JSON (не HTML)
define('X_DEBUG', false);     // true - в JSON ответов будут детали ошибок

date_default_timezone_set('UTC');
