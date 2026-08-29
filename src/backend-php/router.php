<?php
/**
 * Router для dev-запуска через встроенный сервер PHP (нужен, потому что
 * php -S без роутера отвечает 404 на /api/*: он ищет файл, а не фронтовик).
 *
 *   php -S 0.0.0.0:8080 -t src/backend-php src/backend-php/router.php
 *
 * Статические файлы (shop_catalog.json, install.php) отдаёт как есть,
 * всё остальное отдаёт в index.php - ровно как nginx-правило
 * "try_files $uri /index.php?$query_string" из README.md.
 */

$path = parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH);
$file = __DIR__ . $path;

if ($path !== '/' && is_file($file)) {
    return false; // пусть сервер сам отдаст/выполнит существующий файл
}

require __DIR__ . '/index.php';
