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
        $earned = (int)($row['season_points_earned'] ?? 0) + 1;
        $up = $pdo->prepare('UPDATE players SET gold = gold + ?, kills = kills + 1, wood = wood + ?, metal = metal + ?, xp = ?, level = ?, season_points = season_points + 1, season_points_earned = ?, battlepass_level = ?, boss_kills = boss_kills + ?, last_seen = ? WHERE id = ?');
        $up->execute([$gold, $wood, $metal, $lvl[1], $lvl[0], $earned, bp_level_from($earned), $isBoss ? 1 : 0, time(), $row['id']]);

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
        $earned = (int)($row['season_points_earned'] ?? 0) + 1;
        $up = $pdo->prepare('UPDATE players SET gold = gold + ?, wood = wood + ?, metal = metal + ?, xp = ?, level = ?, season_points = season_points + 1, season_points_earned = ?, battlepass_level = ?, best_wave = CASE WHEN ? > best_wave THEN ? ELSE best_wave END, perks = ?, last_seen = ? WHERE id = ?');
        $up->execute([$gold, $wood, $metal, $lvl[1], $lvl[0], $earned, bp_level_from($earned), $wave, $wave, json_encode($perks), time(), $row['id']]);

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
        $earned = (int)($row['season_points_earned'] ?? 0) + $bonusSp;
        $up = $pdo->prepare('UPDATE players SET ' . ($won ? 'wins = wins + 1' : 'losses = losses + 1')
            . ', best_wave = CASE WHEN ? > best_wave THEN ? ELSE best_wave END, deaths = deaths + ?, gold = gold + ?, season_points = season_points + ?, season_points_earned = ?, battlepass_level = ?, last_seen = ? WHERE id = ?');
        $up->execute([$waveReached, $waveReached, $deaths, $bonusGold, $bonusSp, $earned, bp_level_from($earned), time(), $row['id']]);

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
                $pr = $pdo->prepare('SELECT season_points_earned FROM players WHERE id = ?');
                $pr->execute([$pid]);
                $earned = (int)$pr->fetchColumn() + 10;
                $pdo->prepare('UPDATE players SET gold = gold + 200, season_points = season_points + 10, season_points_earned = ?, battlepass_level = ? WHERE id = ?')
                    ->execute([$earned, bp_level_from($earned), $pid]);
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

    // --- магазин (цены и whitelist - из shop_catalog.json) ---
    if ($p === 'shop/catalog' && $method === 'GET') {
        if (!have_catalog()) fail('shop catalog missing: ' . CATALOG_FILE, 503);
        out([
            'version' => 1,
            'bp_points_per_level' => BP_POINTS_PER_LEVEL,
            'blueprints' => BLUEPRINTS,
            'items' => array_map(function (array $i) {
                return [
                    'id' => (string)($i['id'] ?? ''),
                    'name' => (string)($i['name'] ?? ''),
                    'type' => (string)($i['type'] ?? 'resource'),
                    'gold' => (int)($i['gold'] ?? 0),
                    'wood' => (int)($i['wood'] ?? 0),
                    'metal' => (int)($i['metal'] ?? 0),
                    'grants' => array_values(array_map('strval', $i['grants'] ?? [])),
                    'desc' => (string)($i['desc'] ?? ''),
                ];
            }, SHOP_ITEMS),
        ]);
    }

    if ($p === 'shop/buy' && $method === 'POST') {
        if (!have_catalog()) fail('shop catalog missing: ' . CATALOG_FILE, 503);
        $ident = player_identity();
        $itemId = substr(req('item_id'), 0, 128);
        $qty = max(1, min(5, in_int('qty', 1)));

        $item = shop_item($itemId);
        if (!$item) fail('unknown item: ' . $itemId, 400);

        $row = find_player($pdo, $ident['id'], $ident['steam'], $ident['name'], true);
        $cost = ['gold' => (int)($item['gold'] ?? 0) * $qty,
                 'wood' => (int)($item['wood'] ?? 0) * $qty,
                 'metal' => (int)($item['metal'] ?? 0) * $qty];

        // чертёж, который уже открыт, покупать незачем
        $bps = json_decode((string)($row['blueprints'] ?: '[]'), true) ?: [];
        if (($item['type'] ?? '') === 'blueprint') {
            $bid = substr((string)preg_replace('#^blueprint:#', '', (string)($item['grants'][0] ?? '')), 0, 128);
            if (in_array($bid, $bps, true)) fail('already unlocked: ' . $bid, 409);
        }

        if ((int)$row['gold'] < $cost['gold'] || (int)$row['wood'] < $cost['wood'] || (int)$row['metal'] < $cost['metal']) {
            fail('not enough resources (need ' . $cost['gold'] . 'g ' . $cost['wood'] . 'w ' . $cost['metal'] . 'm)', 400);
        }

        // награды валидируем ДО списания: битый каталог не должен съедать золото
        foreach (item_grants($item) as $g) {
            if (parse_grant((string)$g)[0] === 'blueprint') {
                $bid = substr(trim((string)(explode(':', (string)$g, 2)[1] ?? '')), 0, 128);
                if ($bid === '' || !in_array($bid, BLUEPRINTS, true)) fail('unknown blueprint in catalog: ' . $bid, 500);
            }
        }

        $pay = $pdo->prepare('UPDATE players SET gold = gold - ?, wood = wood - ?, metal = metal - ?, last_seen = ? WHERE id = ?');
        $pay->execute([$cost['gold'], $cost['wood'], $cost['metal'], time(), $row['id']]);

        $grants = [];
        for ($i = 0; $i < $qty; $i++) foreach (item_grants($item) as $g) $grants[] = $g;
        $res = apply_grants($pdo, $row['id'], $grants);

        $lg = $pdo->prepare('INSERT INTO shop_purchases (player_id, item_id, qty, gold, wood, metal, created_at) VALUES (?,?,?,?,?,?,?)');
        $lg->execute([$row['id'], $itemId, $qty, $cost['gold'], $cost['wood'], $cost['metal'], time()]);

        out([
            'status' => 'ok',
            'item_id' => $itemId,
            'qty' => $qty,
            'paid' => $cost,
            'granted' => $res['applied'],
            'gold' => $res['balances']['gold'],
            'wood' => $res['balances']['wood'],
            'metal' => $res['balances']['metal'],
            'season_points' => $res['balances']['season_points'],
            'blueprints' => $res['balances']['blueprints'],
            'titles' => $res['balances']['titles'],
            'cosmetics' => $res['balances']['cosmetics'],
        ]);
    }

    if ($p === 'shop/history' && $method === 'GET') {
        $ident = player_identity();
        $row = find_player($pdo, $ident['id'], $ident['steam'], $ident['name'], false);
        if (!$row) fail('player not found', 404);
        $st = $pdo->prepare('SELECT item_id, qty, gold, wood, metal, created_at FROM shop_purchases WHERE player_id = ? ORDER BY id DESC LIMIT 50');
        $st->execute([$row['id']]);
        out(array_map(function (array $r) {
            return ['item_id' => $r['item_id'], 'qty' => (int)$r['qty'], 'gold' => (int)$r['gold'],
                    'wood' => (int)$r['wood'], 'metal' => (int)$r['metal'], 'created_at' => (int)$r['created_at']];
        }, $st->fetchAll()));
    }

    // --- BattlePass: прогресс и выдача наград ---
    if ($p === 'battlepass/progress' && $method === 'GET') {
        $ident = player_identity();
        $row = find_player($pdo, $ident['id'], $ident['steam'], $ident['name'], false);
        if (!$row) fail('player not found', 404);
        $season = current_season_id($pdo);
        $earned = (int)($row['season_points_earned'] ?? 0);
        $level = bp_level_from($earned);
        $claimed = claimed_levels($pdo, $row['id'], $season);
        $rewards = [];
        foreach ($pdo->query('SELECT * FROM battlepass_rewards ORDER BY level')->fetchAll() as $r) {
            $rewards[] = [
                'level' => (int)$r['level'], 'type' => $r['reward_type'], 'id' => $r['reward_id'],
                'name' => $r['reward_name'],
                'unlocked' => (int)$r['level'] <= $level,
                'claimed' => in_array((int)$r['level'], $claimed, true),
            ];
        }
        out([
            'season' => $season,
            'points' => (int)$row['season_points'],
            'points_earned' => $earned,
            'level' => $level,
            'max_level' => BP_MAX_LEVEL,
            'points_per_level' => BP_POINTS_PER_LEVEL,
            'points_to_next' => $level >= BP_MAX_LEVEL ? 0 : ($level + 1) * BP_POINTS_PER_LEVEL - $earned,
            'claimed' => $claimed,
            'rewards' => $rewards,
        ]);
    }

    if ($p === 'battlepass/claim' && $method === 'POST') {
        $ident = player_identity();
        $level = in_int('level', -1);
        if ($level < 1 || $level > BP_MAX_LEVEL) fail('bad level', 400);

        $row = find_player($pdo, $ident['id'], $ident['steam'], $ident['name'], true);
        $st = $pdo->prepare('SELECT * FROM battlepass_rewards WHERE level = ?');
        $st->execute([$level]);
        $reward = $st->fetch();
        if (!$reward) fail('no reward for level ' . $level, 404);

        $season = current_season_id($pdo);
        if (in_array($level, claimed_levels($pdo, $row['id'], $season), true)) {
            fail('already claimed', 409);
        }
        $have = bp_level_from((int)($row['season_points_earned'] ?? 0));
        if ($level > $have) fail('battlepass level ' . $level . ' required (you have ' . $have . ')', 400);

        $grant = reward_to_grant($reward);
        $res = apply_grants($pdo, $row['id'], [$grant]);

        $cl = $pdo->prepare('INSERT INTO battlepass_claims (player_id, level, season, reward, created_at) VALUES (?,?,?,?,?)');
        $cl->execute([$row['id'], $level, $season, (string)$reward['reward_name'], time()]);

        out([
            'status' => 'ok', 'level' => $level, 'season' => $season,
            'reward' => ['type' => $reward['reward_type'], 'id' => $reward['reward_id'], 'name' => $reward['reward_name']],
            'granted' => $res['applied'],
            'gold' => $res['balances']['gold'],
            'wood' => $res['balances']['wood'],
            'metal' => $res['balances']['metal'],
            'season_points' => $res['balances']['season_points'],
            'blueprints' => $res['balances']['blueprints'],
            'titles' => $res['balances']['titles'],
            'cosmetics' => $res['balances']['cosmetics'],
        ]);
    }

    // --- сброс сезона (админ; требует ADMIN_SECRET) ---
    if ($p === 'season/reset' && $method === 'POST') {
        check_admin();
        $season = current_season_id($pdo);
        $now = time();

        $arch = $pdo->prepare('INSERT INTO season_history (season, player_id, season_points, bp_level, kills, boss_kills, best_wave, wins, losses, created_at) SELECT ?, id, season_points, battlepass_level, kills, boss_kills, best_wave, wins, losses, ? FROM players');
        $arch->execute([$season, $now]);
        $archived = $arch->rowCount();

        $pdo->exec("UPDATE players SET season_points = 0, season_points_earned = 0, battlepass_level = 0, meta = '[]'");

        $next = $season + 1;
        $ins = $pdo->prepare('INSERT INTO seasons (id, name, start_time, end_time, rewards) VALUES (?,?,?,?,?)');
        $ins->execute([$next, 'Season ' . $next, $now, $now + 60 * 60 * 24 * 60, json_encode([])]);

        out(['status' => 'ok', 'archived_season' => $season, 'new_season' => $next, 'players_archived' => $archived]);
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
