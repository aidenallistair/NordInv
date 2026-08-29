<?php
/**
 * Nord Invasion Better Edition - API (PHP + MySQL).
 *
 * Front controller: один вход (nginx/IIS пробрасывает всё сюда).
 * Конфиг: config.php. Установка: php install.php.
 *
 * Контракт: запросы form-encoded или JSON, ответы - JSON.
 * Все маршруты проверяют X-NI-Secret, если API_SECRET задан.
 */

require_once __DIR__ . '/lib.php';

try {
    $pdo = db();
    check_secret();

    $method = $_SERVER['REQUEST_METHOD'];
    $path = parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH);
    // нормализуем: убираем /api и хвосты
    $p = trim(str_replace('\\', '/', $path), '/');
    $p = preg_replace('#^api/?#', '', $p);
    $seg = $p === '' ? [] : explode('/', $p);

    // --- служебные ---
    if ($p === '' && $method === 'GET') {
        out(['message' => 'Nord Invasion Backend (PHP+MySQL)', 'version' => '2.0', 'ok' => true]);
    }
    if ($p === 'health' && $method === 'GET') {
        out(['ok' => true, 'db' => DB_DRIVER, 'time' => time()]);
    }

    // --- игрок ---
    if ($p === 'player/login' && $method === 'POST') {
        $ident = player_identity();
        $row = find_player($pdo, $ident['id'], $ident['steam'], $ident['name'], true);
        out(array_merge(profile_row($row), ['new' => (int)$row['last_seen'] === (int)time() && (int)$row['kills'] === 0 && (int)$row['wins'] === 0 && (int)$row['losses'] === 0]));
    }
    if (preg_match('#^player/([\w.\-]+)$#', $p, $m) && $method === 'GET') {
        $st = $pdo->prepare('SELECT * FROM players WHERE id = ? OR steam_id = ? LIMIT 1');
        $st->execute([$m[1], $m[1]]);
        $row = $st->fetch();
        if (!$row) fail('Player not found', 404);
        out(profile_row($row));
    }

    // --- бой: убитый ---
    if ($p === 'kill' && $method === 'POST') {
        $ident = player_identity();
        $troop = substr(req('killed_troop'), 0, 128);
        $gold = in_int('gold_reward', 10);
        $wood = in_int('wood', 0);
        $metal = in_int('metal', 0);
        $wave = in_int('wave', 0);
        $isBoss = in_int('is_boss', 0) === 1;

        $row = find_player($pdo, $ident['id'], $ident['steam'], $ident['name'], true);

        $lvl = apply_xp($row, 10);
        $up = $pdo->prepare('UPDATE players SET gold = gold + ?, kills = kills + 1, wood = wood + ?, metal = metal + ?, xp = ?, level = ?, season_points = season_points + 1, boss_kills = boss_kills + ?, last_seen = ? WHERE id = ?');
        $up->execute([$gold, $wood, $metal, $lvl[1], $lvl[0], $isBoss ? 1 : 0, time(), $row['id']]);

        $kl = $pdo->prepare('INSERT INTO kill_log (player_id, wave, troop, gold, created_at) VALUES (?,?,?,?,?)');
        $kl->execute([$row['id'], $wave, $troop, $gold, time()]);

        $resp = ['status' => 'ok', 'reward' => $gold];
        if ($isBoss) {
            $fresh = refresh_row($pdo, $row['id']);
            $t = grant_titles($pdo, $fresh);
            if ($t) $resp['titles_earned'] = $t;
        }
        out($resp);
    }

    // --- бой: волна завершена ---
    if ($p === 'wave/complete' && $method === 'POST') {
        $ident = player_identity();
        $wave = in_int('wave', 1);
        $gold = in_int('gold', 0);
        $wood = in_int('wood', 0);
        $metal = in_int('metal', 0);
        $perkId = in_int('perk_id', -1);

        $row = find_player($pdo, $ident['id'], $ident['steam'], $ident['name'], true);

        // перк (id 0-99, дедупликация)
        $perks = json_decode((string)($row['perks'] ?: '[]'), true) ?: [];
        if ($perkId >= 0 && $perkId <= 99 && !in_array($perkId, $perks, true)) {
            $perks[] = $perkId;
        }

        $lvl = apply_xp($row, 5 * max(1, $wave));
        $up = $pdo->prepare('UPDATE players SET gold = gold + ?, wood = wood + ?, metal = metal + ?, xp = ?, level = ?, season_points = season_points + 1, best_wave = CASE WHEN ? > best_wave THEN ? ELSE best_wave END, perks = ?, last_seen = ? WHERE id = ?');
        $up->execute([$gold, $wood, $metal, $lvl[1], $lvl[0], $wave, $wave, json_encode($perks), time(), $row['id']]);

        out(['status' => 'ok', 'wave' => $wave, 'level' => $lvl[0], 'xp' => $lvl[1]]);
    }

    // --- конец забега ---
    if ($p === 'run/save' && $method === 'POST') {
        $ident = player_identity();
        $won = in_int('won', 0) === 1;
        $waveReached = in_int('wave_reached', 1);
        $kills = in_int('kills', 0);
        $deaths = in_int('deaths', 0);

        $row = find_player($pdo, $ident['id'], $ident['steam'], $ident['name'], true);

        $bonusGold = $won ? 100 : 0;
        $bonusSp = $won ? 50 : 0;
        $up = $pdo->prepare('UPDATE players SET ' . ($won ? 'wins = wins + 1' : 'losses = losses + 1')
            . ', best_wave = CASE WHEN ? > best_wave THEN ? ELSE best_wave END, deaths = deaths + ?, gold = gold + ?, season_points = season_points + ?, last_seen = ? WHERE id = ?');
        $up->execute([$waveReached, $waveReached, $deaths, $bonusGold, $bonusSp, time(), $row['id']]);

        $resp = ['status' => 'ok', 'won' => $won, 'bonus_gold' => $bonusGold];
        $fresh = refresh_row($pdo, $row['id']);
        // "The Wall": дожил до 10+ волн без смертей
        if (($won || $waveReached >= 10) && $deaths === 0) {
            $t = grant_title($pdo, $fresh, 'wall');
            if ($t) $resp['titles_earned'] = $t;
        }
        out($resp);
    }

    // --- перк выбран (сохранение, без повторных наград) ---
    if ($p === 'perk/record' && $method === 'POST') {
        $ident = player_identity();
        $perkId = in_int('perk_id', -1);
        if ($perkId < 0 || $perkId > 99) fail('bad perk_id', 400);

        $row = find_player($pdo, $ident['id'], $ident['steam'], $ident['name'], true);
        $perks = json_decode((string)($row['perks'] ?: '[]'), true) ?: [];
        $new = false;
        if (!in_array($perkId, $perks, true)) {
            $perks[] = $perkId;
            $new = true;
            $up = $pdo->prepare('UPDATE players SET perks = ? WHERE id = ?');
            $up->execute([json_encode($perks), $row['id']]);
        }
        out(['perks' => $perks, 'new' => $new]);
    }

    // --- чертеж ---
    if ($p === 'blueprint/unlock' && $method === 'POST') {
        $ident = player_identity();
        $bpId = substr(req('blueprint_id'), 0, 128);
        if (!in_array($bpId, BLUEPRINTS, true)) fail('unknown blueprint: ' . $bpId, 400);

        $row = find_player($pdo, $ident['id'], $ident['steam'], $ident['name'], true);
        $bps = json_decode((string)($row['blueprints'] ?: '[]'), true) ?: [];
        $new = false;
        if (!in_array($bpId, $bps, true)) {
            $bps[] = $bpId;
            $up = $pdo->prepare('UPDATE players SET blueprints = ? WHERE id = ?');
            $up->execute([json_encode($bps), $row['id']]);
            $new = true;
        }
        out(['blueprints' => $bps, 'new' => $new]);
    }

    // --- мета-прокачка (skill tree) ---
    if ($p === 'meta/unlock' && $method === 'POST') {
        $ident = player_identity();
        $nodeId = substr(req('node_id'), 0, 64);

        $st = $pdo->prepare('SELECT * FROM skill_nodes WHERE id = ?');
        $st->execute([$nodeId]);
        $node = $st->fetch();
        if (!$node) fail('unknown skill node: ' . $nodeId, 400);

        $row = find_player($pdo, $ident['id'], $ident['steam'], $ident['name'], true);
        $meta = json_decode((string)($row['meta'] ?: '[]'), true) ?: [];

        if (in_array($nodeId, $meta, true)) out(['meta' => $meta, 'already' => true]);
        if ($node['requires'] !== '' && !in_array($node['requires'], $meta, true)) {
            fail('requires node: ' . $node['requires'], 400);
        }
        if ((int)$row['season_points'] < (int)$node['cost']) {
            fail('not enough season_points (need ' . $node['cost'] . ')', 400);
        }

        $meta[] = $nodeId;
        $up = $pdo->prepare('UPDATE players SET meta = ?, season_points = season_points - ? WHERE id = ?');
        $up->execute([json_encode($meta), $node['cost'], $row['id']]);
        out(['meta' => $meta, 'spent' => $node['cost']]);
    }

    // --- счётчики (revives/builds/boss_kills) + ранги ---
    if ($p === 'stat/increment' && $method === 'POST') {
        $ident = player_identity();
        $stat = req('stat');
        if (!in_array($stat, ['revives', 'builds', 'boss_kills'], true)) fail('unknown stat', 400);

        $row = find_player($pdo, $ident['id'], $ident['steam'], $ident['name'], true);
        $up = $pdo->prepare("UPDATE players SET {$stat} = {$stat} + 1 WHERE id = ?");
        $up->execute([$row['id']]);

        $resp = ['status' => 'ok', $stat => (int)$row[$stat] + 1];
        $fresh = refresh_row($pdo, $row['id']);
        $t = grant_titles($pdo, $fresh);
        if ($t) $resp['titles_earned'] = $t;
        out($resp);
    }

    // --- кампания ---
    if ($p === 'campaign/villages' && $method === 'GET') {
        $rows = $pdo->query('SELECT * FROM villages ORDER BY id')->fetchAll();
        $out = [];
        foreach ($rows as $r) {
            $out[] = [
                'id' => (int)$r['id'], 'name' => $r['name'], 'owner' => $r['owner'],
                'defense' => (int)$r['defense_level'], 'x' => (int)$r['x'], 'y' => (int)$r['y'],
                'won' => (int)$r['battles_won'], 'lost' => (int)$r['battles_lost'],
            ];
        }
        // голоса текущего сезона
        $season = current_season_id($pdo);
        $v = $pdo->prepare('SELECT village_id, COUNT(*) AS c FROM campaign_votes WHERE season = ? GROUP BY village_id');
        $v->execute([$season]);
        $votes = [];
        foreach ($v->fetchAll() as $r) $votes[(int)$r['village_id']] = (int)$r['c'];
        foreach ($out as &$o) $o['votes'] = $votes[$o['id']] ?? 0;
        out($out);
    }

    if ($p === 'campaign/battle' && $method === 'POST') {
        $vid = in_int('village_id', -1);
        $won = in_int('won', 0) === 1;
        $players = array_filter(array_map('trim', explode(',', (string)in_('players', ''))));
        $waveReached = in_int('wave_reached', 0);

        $st = $pdo->prepare('SELECT * FROM villages WHERE id = ?');
        $st->execute([$vid]);
        $v = $st->fetch();
        if (!$v) fail('unknown village', 404);

        if ($won) {
            $pdo->prepare("UPDATE villages SET battles_won = battles_won + 1, owner = 'swadia', defense_level = defense_level + 1 WHERE id = ?")->execute([$vid]);
        } else {
            $pdo->prepare('UPDATE villages SET battles_lost = battles_lost + 1, owner = ?, defense_level = CASE WHEN defense_level > 2 THEN defense_level - 1 ELSE 1 END WHERE id = ?')
                ->execute(['nords', $vid]);
        }
        foreach ($players as $pid) {
            if ($pid === '') continue;
            $f = $pdo->prepare('SELECT id FROM players WHERE id = ? OR steam_id = ? LIMIT 1');
            $f->execute([$pid, $pid]);
            if ($f->fetch()) {
                $pdo->prepare('UPDATE players SET gold = gold + 200, season_points = season_points + 10 WHERE id = ?')
                    ->execute([$pid]);
            }
        }
        out(['village_id' => $vid, 'won' => $won]);
    }

    if ($p === 'campaign/vote' && $method === 'POST') {
        $vid = in_int('village_id', -1);
        $voter = substr(req('voter'), 0, 128);
        $season = current_season_id($pdo);

        $st = $pdo->prepare('SELECT * FROM villages WHERE id = ?');
        $st->execute([$vid]);
        if (!$st->fetch()) fail('unknown village', 404);

        try {
            $pdo->prepare('INSERT INTO campaign_votes (village_id, voter, season, created_at) VALUES (?,?,?,?)')
                ->execute([$vid, $voter, $season, time()]);
        } catch (PDOException $e) {
            fail('already voted this season', 409);
        }
        out(['village_id' => $vid, 'voter' => $voter, 'season' => $season]);
    }

    // --- сезон / лидерборд / battlepass ---
    if ($p === 'season/current' && $method === 'GET') {
        $row = $pdo->query('SELECT * FROM seasons ORDER BY id DESC LIMIT 1')->fetch();
        if (!$row) fail('No season', 404);
        out(['id' => (int)$row['id'], 'name' => $row['name'], 'start' => (int)$row['start_time'], 'end' => (int)$row['end_time']]);
    }

    if ($p === 'leaderboard' && $method === 'GET') {
        $rows = $pdo->query('SELECT peer_name AS name, kills, gold, level, season_points FROM players ORDER BY season_points DESC, kills DESC LIMIT 20')->fetchAll();
        out($rows);
    }

    if ($p === 'battlepass/rewards' && $method === 'GET') {
        $rows = $pdo->query('SELECT * FROM battlepass_rewards ORDER BY level')->fetchAll();
        $out = [];
        foreach ($rows as $r) {
            $out[] = ['level' => (int)$r['level'], 'type' => $r['reward_type'], 'id' => $r['reward_id'], 'name' => $r['reward_name']];
        }
        out($out);
    }

    fail('not found: ' . $method . ' /api/' . $p, 404);
} catch (PDOException $e) {
    fail('db error: ' . $e->getMessage(), 500);
} catch (Throwable $e) {
    fail('error: ' . $e->getMessage(), 500);
}

/** Перечитать строку игрока из БД. */
function refresh_row(PDO $pdo, string $id): array
{
    $st = $pdo->prepare('SELECT * FROM players WHERE id = ?');
    $st->execute([$id]);
    $row = $st->fetch();
    if (!$row) fail('player vanished', 500);
    return $row;
}

/** ID текущего сезона (последний). */
function current_season_id(PDO $pdo): int
{
    $row = $pdo->query('SELECT id FROM seasons ORDER BY id DESC LIMIT 1')->fetch();
    return $row ? (int)$row['id'] : 1;
}

/**
 * Ранги по счётчикам. Возвращает список вновь полученных титулов.
 * wall: 10 волн без смертей (проверяется в run/save)
 * savior: 50 реанимаций, jarl_slayer: 10 боссов, engineer_master: 100 построек
 */
function grant_titles(PDO $pdo, array $row): array
{
    $earned = [];
    $titles = json_decode((string)($row['titles'] ?: '[]'), true) ?: [];
    $rules = [
        ['savior', (int)$row['revives'] >= 50],
        ['jarl_slayer', (int)$row['boss_kills'] >= 10],
        ['engineer_master', (int)$row['builds'] >= 100],
    ];
    foreach ($rules as [$t, $ok]) {
        if ($ok && !in_array($t, $titles, true)) {
            $titles[] = $t;
            $earned[] = $t;
        }
    }
    if ($earned) {
        $up = $pdo->prepare('UPDATE players SET titles = ? WHERE id = ?');
        $up->execute([json_encode($titles), $row['id']]);
    }
    return $earned;
}

function grant_title(PDO $pdo, array $row, string $title): ?string
{
    $titles = json_decode((string)($row['titles'] ?: '[]'), true) ?: [];
    if (in_array($title, $titles, true)) return null;
    $titles[] = $title;
    $up = $pdo->prepare('UPDATE players SET titles = ? WHERE id = ?');
    $up->execute([json_encode($titles), $row['id']]);
    return $title;
}
