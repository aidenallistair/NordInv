-- Nord Invasion Better Edition - схема MySQL 5.7+/8.x
-- Создание: база nordinv (utf8mb4), затем этот файл.
-- Или CLI: php install.php (создаст схему + начальные данные сам).

CREATE TABLE IF NOT EXISTS players (
  id             VARCHAR(128) NOT NULL PRIMARY KEY,
  steam_id       VARCHAR(64)  NOT NULL DEFAULT '',
  peer_name      VARCHAR(128) NOT NULL DEFAULT '',
  gold           INT          NOT NULL DEFAULT 500,
  wood           INT          NOT NULL DEFAULT 0,
  metal          INT          NOT NULL DEFAULT 0,
  kills          INT          NOT NULL DEFAULT 0,
  deaths         INT          NOT NULL DEFAULT 0,
  level          INT          NOT NULL DEFAULT 1,
  xp             INT          NOT NULL DEFAULT 0,
  season_points  INT          NOT NULL DEFAULT 0,
  season_points_earned INT     NOT NULL DEFAULT 0,
  battlepass_level INT          NOT NULL DEFAULT 0,
  wins           INT          NOT NULL DEFAULT 0,
  losses         INT          NOT NULL DEFAULT 0,
  best_wave      INT          NOT NULL DEFAULT 0,
  revives        INT          NOT NULL DEFAULT 0,
  boss_kills     INT          NOT NULL DEFAULT 0,
  builds         INT          NOT NULL DEFAULT 0,
  perks          TEXT,
  blueprints     TEXT,
  meta           TEXT,
  titles         TEXT,
  cosmetics      TEXT,
  last_seen      INT          NOT NULL DEFAULT 0,
  created_at     INT          NOT NULL DEFAULT 0,
  KEY idx_players_steam (steam_id),
  KEY idx_players_sp    (season_points)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS kill_log (
  id          BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  player_id   VARCHAR(128) NOT NULL,
  wave        INT NOT NULL DEFAULT 0,
  troop       VARCHAR(128) NOT NULL DEFAULT '',
  gold        INT NOT NULL DEFAULT 0,
  created_at  INT NOT NULL DEFAULT 0,
  KEY idx_kill_player (player_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS villages (
  id            INT NOT NULL PRIMARY KEY,
  name          VARCHAR(128) NOT NULL,
  owner         VARCHAR(32)  NOT NULL DEFAULT 'swadia',
  defense_level INT          NOT NULL DEFAULT 1,
  x             INT NOT NULL DEFAULT 0,
  y             INT NOT NULL DEFAULT 0,
  battles_won   INT NOT NULL DEFAULT 0,
  battles_lost  INT NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS seasons (
  id         INT NOT NULL PRIMARY KEY,
  name       VARCHAR(128) NOT NULL,
  start_time INT NOT NULL,
  end_time   INT NOT NULL,
  rewards    TEXT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS battlepass_rewards (
  level       INT NOT NULL PRIMARY KEY,
  reward_type VARCHAR(32)  NOT NULL,
  reward_id   VARCHAR(128) NOT NULL,
  reward_name VARCHAR(255) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS skill_nodes (
  id       VARCHAR(64) NOT NULL PRIMARY KEY,
  name     VARCHAR(128) NOT NULL,
  cost     INT NOT NULL DEFAULT 0,
  requires VARCHAR(64) NOT NULL DEFAULT ''
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS campaign_votes (
  id         BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  village_id INT NOT NULL,
  voter      VARCHAR(128) NOT NULL,
  season     INT NOT NULL DEFAULT 1,
  created_at INT NOT NULL DEFAULT 0,
  UNIQUE KEY uniq_vote (village_id, voter, season)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- BattlePass: выданные награды (1 награда на уровень за сезон)
CREATE TABLE IF NOT EXISTS battlepass_claims (
  id          BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  player_id   VARCHAR(128) NOT NULL,
  level       INT NOT NULL,
  season      INT NOT NULL DEFAULT 1,
  reward      VARCHAR(255) NOT NULL DEFAULT '',
  created_at  INT NOT NULL DEFAULT 0,
  UNIQUE KEY uniq_claim (player_id, level, season),
  KEY idx_claim_player (player_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Журнал покупок в магазине (аудит экономики / античит)
CREATE TABLE IF NOT EXISTS shop_purchases (
  id          BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  player_id   VARCHAR(128) NOT NULL,
  item_id     VARCHAR(128) NOT NULL,
  qty         INT NOT NULL DEFAULT 1,
  gold        INT NOT NULL DEFAULT 0,
  wood        INT NOT NULL DEFAULT 0,
  metal       INT NOT NULL DEFAULT 0,
  created_at  INT NOT NULL DEFAULT 0,
  KEY idx_shop_player (player_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Архив сезонов (пишется при /api/season/reset)
CREATE TABLE IF NOT EXISTS season_history (
  id             BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  season         INT NOT NULL,
  player_id      VARCHAR(128) NOT NULL,
  season_points  INT NOT NULL DEFAULT 0,
  bp_level       INT NOT NULL DEFAULT 0,
  kills          INT NOT NULL DEFAULT 0,
  boss_kills     INT NOT NULL DEFAULT 0,
  best_wave      INT NOT NULL DEFAULT 0,
  wins           INT NOT NULL DEFAULT 0,
  losses         INT NOT NULL DEFAULT 0,
  created_at     INT NOT NULL DEFAULT 0,
  KEY idx_history_season (season)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
