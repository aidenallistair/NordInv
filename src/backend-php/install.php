<?php
/**
 * CLI-установщик: php install.php
 * Создаёт схему и начальные данные (деревни, сезон, battlepass, skill nodes).
 * Работает и с MySQL (продакшн), и с SQLite (dev, DB_DRIVER=sqlite в config.php).
 */

require_once __DIR__ . '/lib.php';

$pdo = db();
$mysql = (DB_DRIVER !== 'sqlite');

function ddl(string $sql, bool $mysql): string
{
    if ($mysql) return $sql;
    // SQLite-варианты
    $sql = str_replace(' BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY', ' INTEGER PRIMARY KEY AUTOINCREMENT', $sql);
    $sql = str_replace(') ENGINE=InnoDB DEFAULT CHARSET=utf8mb4', ' )', $sql);
    // UNIQUE KEY name (cols) -> табличное ограничение UNIQUE (cols) (валидно в SQLite)
    $sql = preg_replace('/^[ \t]*,?[ \t]*UNIQUE[ \t]+KEY[ \t]+\w+[ \t]*\(([^)]*)\),?[ \t]*\r?\n/m', ' UNIQUE ($1)\n', $sql);
    // обычные индексы KEY name (cols) - убираем строки целиком
    $sql = preg_replace('/^[ \t]*,?[ \t]*KEY[ \t]+\w+[ \t]*\([^)]*\),?[ \t]*\r?\n/m', '', $sql);
    $sql = preg_replace('/,\s*\)/', ')', $sql); // висящая запятая перед )
    $sql = str_replace('  ', ' ', $sql);
    return $sql;
}

$schema = file_get_contents(__DIR__ . '/schema.sql');
// сначала убираем комментарии (в них могут быть ";"), потом режем на statements
$clean = implode("\n", array_filter(array_map(
    fn($l) => strpos(ltrim($l), '--') === 0 ? null : $l,
    explode("\n", $schema)
)));
foreach (array_filter(array_map('trim', explode(';', $clean))) as $stmt) {
    $pdo->exec(ddl($stmt, $mysql));
    echo "OK  schema: " . substr(trim(explode("\n", $stmt)[0]), 0, 60) . "...\n";
}

function seed(PDO $pdo): void
{
    $count = (int)$pdo->query('SELECT COUNT(*) FROM villages')->fetchColumn();
    if ($count === 0) {
        $villages = [
            [0, 'Village of Jelbegi', 'swadia', 1, 100, 200],
            [1, 'Forest Hamlet',      'swadia', 1, 300, 400],
            [2, 'Castle Outpost',     'swadia', 2, 500, 100],
            [3, 'Bridge Fort',        'swadia', 3, 200, 500],
            [4, 'Snow Village',       'nords',  2, 700, 300],
            [5, 'Desert Oasis',       'swadia', 1, 400, 600],
            [6, 'Mountain Keep',      'nords',  3, 600, 700],
            [7, 'Coastal Town',       'swadia', 2, 100, 700],
        ];
        $st = $pdo->prepare('INSERT INTO villages (id, name, owner, defense_level, x, y) VALUES (?,?,?,?,?,?)');
        foreach ($villages as $v) $st->execute($v);
        echo "OK  villages: 8\n";
    }

    $count = (int)$pdo->query('SELECT COUNT(*) FROM seasons')->fetchColumn();
    if ($count === 0) {
        $now = time();
        $st = $pdo->prepare('INSERT INTO seasons (id, name, start_time, end_time, rewards) VALUES (1, ?, ?, ?, ?)');
        $st->execute(['Season 1: Nord Awakening', $now, $now + 60 * 60 * 24 * 60, json_encode([])]);
        echo "OK  season: 1\n";
    }

    $count = (int)$pdo->query('SELECT COUNT(*) FROM battlepass_rewards')->fetchColumn();
    if ($count === 0) {
        // Источник истины - "battlepass" в shop_catalog.json. Fallback ниже -
        // на случай, если каталог не скопировали при деплое.
        $bp = [];
        if (have_catalog()) {
            foreach (BATTLEPASS_REWARDS as $r) {
                $bp[] = [(int)($r['level'] ?? 0), (string)($r['type'] ?? 'gold'),
                         (string)($r['id'] ?? ''), (string)($r['name'] ?? '')];
            }
        }
        if (!$bp) $bp = [
            [1,  'gold',      '100',        '100 Gold'],
            [2,  'blueprint', 'wall_wood',  'Wooden Wall Blueprint'],
            [3,  'title',     'defender',   'Title: Defender'],
            [5,  'blueprint', 'oil_cauldron', 'Oil Cauldron Blueprint'],
            [10, 'skin',      'jarl_helmet','Jarl Helmet Skin'],
            [15, 'gold',      '1000',       '1000 Gold'],
            [20, 'title',     'nord_slayer','Title: Nord Slayer'],
        ];
        $st = $pdo->prepare('INSERT INTO battlepass_rewards (level, reward_type, reward_id, reward_name) VALUES (?,?,?,?)');
        foreach ($bp as $r) $st->execute($r);
        echo "OK  battlepass: " . count($bp) . "\n";
    }

    $count = (int)$pdo->query('SELECT COUNT(*) FROM skill_nodes')->fetchColumn();
    if ($count === 0) {
        $nodes = [
            ['blacksmith_1', 'Apprentice Blacksmith', 10, ''],
            ['blacksmith_2', 'Master Blacksmith',     20, 'blacksmith_1'],
            ['veteran_1',    'Veteran',               10, ''],
            ['veteran_2',    'Elite Veteran',         25, 'veteran_1'],
            ['engineer_1',   'Engineer Basics',       15, ''],
            ['engineer_2',   'Fortress Architect',    30, 'engineer_1'],
            ['leader_1',     'Squad Leader',          20, ''],
        ];
        $st = $pdo->prepare('INSERT INTO skill_nodes (id, name, cost, requires) VALUES (?,?,?,?)');
        foreach ($nodes as $n) $st->execute($n);
        echo "OK  skill_nodes: " . count($nodes) . "\n";
    }
}

/** Колонки таблицы (для идемпотентной миграции существующих баз). */
function columns_of(PDO $pdo, string $table): array
{
    if (DB_DRIVER === 'sqlite') {
        $rows = $pdo->query('PRAGMA table_info(' . $table . ')')->fetchAll();
        return array_column($rows, 'name');
    }
    $st = $pdo->prepare('SELECT COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = ? AND TABLE_NAME = ?');
    $st->execute([DB_NAME, $table]);
    return array_column($st->fetchAll(), 'COLUMN_NAME');
}

/**
 * Идемпотентная доводка схемы баз, созданных до v2.1 (сезоны/BattlePass/магазин):
 * добавляет отсутствующие колонки players. Новые таблицы создаёт schema.sql выше.
 */
function migrate(PDO $pdo): void
{
    $have = columns_of($pdo, 'players');
    $need = [
        'season_points_earned' => 'INT NOT NULL DEFAULT 0',
        'cosmetics'            => 'TEXT',
    ];
    foreach ($need as $col => $colDdl) {
        if (!in_array($col, $have, true)) {
            $pdo->exec('ALTER TABLE players ADD COLUMN ' . $col . ' ' . $colDdl);
            echo "OK  migrate: players.{$col}\n";
        }
    }
}

migrate($pdo);

seed($pdo);
echo "Готово. База: " . (DB_DRIVER === 'sqlite' ? DB_PATH : DB_NAME . " @ " . DB_HOST) . "\n";
