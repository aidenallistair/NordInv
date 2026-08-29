"""
Nord Invasion Better Edition Backend
Implements mechanics 12 and 15: Persistence 2.0 + Campaign + Seasons + Blueprints
"""

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import Optional, List, Dict
import sqlite3
import json
import time
import os

app = FastAPI(title="Fianna Nord Invasion Better Edition Backend", version="2.0")

DB_PATH = "ni_better.db"

def init_db():
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    # Players with SteamID, blueprints, season
    c.execute('''CREATE TABLE IF NOT EXISTS players
                 (id TEXT PRIMARY KEY, steam_id TEXT UNIQUE, name TEXT, gold INTEGER DEFAULT 500, 
                  kills INTEGER DEFAULT 0, deaths INTEGER DEFAULT 0, level INTEGER DEFAULT 1,
                  xp INTEGER DEFAULT 0, wood INTEGER DEFAULT 0, metal INTEGER DEFAULT 0,
                  blueprints TEXT DEFAULT '[]', season_points INTEGER DEFAULT 0,
                  battlepass_level INTEGER DEFAULT 0, last_seen INTEGER DEFAULT 0)''')
    # Characters with perks
    c.execute('''CREATE TABLE IF NOT EXISTS characters
                 (player_id TEXT, char_name TEXT, class TEXT, gold INTEGER, 
                  inventory TEXT, perks TEXT, PRIMARY KEY(player_id, char_name))''')
    # Villages for campaign Mechanic 15
    c.execute('''CREATE TABLE IF NOT EXISTS villages
                 (id INTEGER PRIMARY KEY, name TEXT, owner TEXT DEFAULT 'swadia', 
                  defense_level INTEGER DEFAULT 1, x INTEGER, y INTEGER, battles_won INTEGER DEFAULT 0, battles_lost INTEGER DEFAULT 0)''')
    # Seasons
    c.execute('''CREATE TABLE IF NOT EXISTS seasons
                 (id INTEGER PRIMARY KEY, name TEXT, start_time INTEGER, end_time INTEGER, rewards TEXT)''')
    # Battlepass rewards
    c.execute('''CREATE TABLE IF NOT EXISTS battlepass_rewards
                 (level INTEGER PRIMARY KEY, reward_type TEXT, reward_id TEXT, reward_name TEXT)''')
    
    # Init villages if empty
    c.execute("SELECT COUNT(*) FROM villages")
    if c.fetchone()[0] == 0:
        villages = [
            (0, "Village of Jelbegi", "swadia", 1, 100, 200),
            (1, "Forest Hamlet", "swadia", 1, 300, 400),
            (2, "Castle Outpost", "swadia", 2, 500, 100),
            (3, "Bridge Fort", "swadia", 3, 200, 500),
            (4, "Snow Village", "nords", 2, 700, 300),
            (5, "Desert Oasis", "swadia", 1, 400, 600),
            (6, "Mountain Keep", "nords", 3, 600, 700),
            (7, "Coastal Town", "swadia", 2, 100, 700),
        ]
        c.executemany("INSERT INTO villages (id, name, owner, defense_level, x, y) VALUES (?,?,?,?,?,?)", villages)
    
    # Init seasons
    c.execute("SELECT COUNT(*) FROM seasons")
    if c.fetchone()[0] == 0:
        now = int(time.time())
        c.execute("INSERT INTO seasons (id, name, start_time, end_time, rewards) VALUES (1, 'Season 1: Nord Awakening', ?, ?, '[]')", (now, now+60*60*24*60))
    
    # Init battlepass
    c.execute("SELECT COUNT(*) FROM battlepass_rewards")
    if c.fetchone()[0] == 0:
        rewards = [
            (1, "gold", "100", "100 Gold"),
            (2, "blueprint", "wall_wood", "Wooden Wall Blueprint"),
            (3, "title", "defender", "Title: Defender"),
            (5, "blueprint", "oil_cauldron", "Oil Cauldron Blueprint"),
            (10, "skin", "jarl_helmet", "Jarl Helmet Skin"),
            (15, "gold", "1000", "1000 Gold"),
            (20, "title", "nord_slayer", "Title: Nord Slayer"),
        ]
        c.executemany("INSERT INTO battlepass_rewards VALUES (?,?,?,?)", rewards)
    
    conn.commit()
    conn.close()

init_db()

class PlayerKill(BaseModel):
    player_id: str
    steam_id: Optional[str] = None
    player_name: str
    killed_troop: str
    gold_reward: int
    wave: int
    wood: Optional[int] = 0
    metal: Optional[int] = 0

class PlayerLogin(BaseModel):
    player_id: str
    steam_id: Optional[str] = None
    player_name: str

class VillageBattle(BaseModel):
    village_id: int
    won: bool
    players: List[str]
    wave_reached: int

class BlueprintUnlock(BaseModel):
    player_id: str
    blueprint_id: str

@app.get("/")
def root():
    return {"message": "Fianna Nord Invasion Better Edition Backend", "version": "2.0", "mechanics": 15}

@app.post("/api/player/login")
def player_login(data: PlayerLogin):
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    c.execute("SELECT * FROM players WHERE id=? OR steam_id=?", (data.player_id, data.steam_id))
    row = c.fetchone()
    now = int(time.time())
    if not row:
        c.execute("INSERT INTO players (id, steam_id, name, last_seen) VALUES (?, ?, ?, ?)", (data.player_id, data.steam_id or data.player_id, data.player_name, now))
        conn.commit()
        conn.close()
        return {"id": data.player_id, "steam_id": data.steam_id, "name": data.player_name, "gold": 500, "wood": 0, "metal": 0, "kills": 0, "level": 1, "blueprints": [], "season_points": 0, "battlepass_level": 0, "new": True}
    else:
        c.execute("UPDATE players SET last_seen=?, name=? WHERE id=?", (now, data.player_name, row[0]))
        conn.commit()
        conn.close()
        blueprints = json.loads(row[8]) if row[8] else []
        return {"id": row[0], "steam_id": row[1], "name": row[2], "gold": row[3], "wood": row[6], "metal": row[7], "kills": row[4], "level": row[5], "xp": row[6], "blueprints": blueprints, "season_points": row[9], "battlepass_level": row[10], "new": False}

@app.get("/api/player/{player_id}")
def get_player(player_id: str):
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    c.execute("SELECT * FROM players WHERE id=? OR steam_id=?", (player_id, player_id))
    row = c.fetchone()
    conn.close()
    if not row:
        raise HTTPException(404, "Player not found")
    return {"id": row[0], "steam_id": row[1], "name": row[2], "gold": row[3], "kills": row[4], "deaths": row[5], "level": row[6], "xp": row[7], "wood": row[8], "metal": row[9], "blueprints": json.loads(row[10] or "[]"), "season_points": row[11], "battlepass_level": row[12]}

@app.post("/api/kill")
def register_kill(kill: PlayerKill):
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    c.execute("UPDATE players SET gold = gold + ?, kills = kills + 1, wood = wood + ?, metal = metal + ?, xp = xp + 10, season_points = season_points + 1 WHERE id=? OR steam_id=?", (kill.gold_reward, kill.wood or 0, kill.metal or 0, kill.player_id, kill.steam_id or kill.player_id))
    if c.rowcount == 0:
        c.execute("INSERT INTO players (id, steam_id, name, gold, kills, wood, metal) VALUES (?, ?, ?, ?, 1, ?, ?)", (kill.player_id, kill.steam_id or kill.player_id, kill.player_name, 500 + kill.gold_reward, kill.wood or 0, kill.metal or 0))
    # Check level up
    c.execute("SELECT xp, level FROM players WHERE id=? OR steam_id=?", (kill.player_id, kill.steam_id or kill.player_id))
    row = c.fetchone()
    if row and row[0] >= row[1]*100:
        c.execute("UPDATE players SET level = level + 1 WHERE id=? OR steam_id=?", (kill.player_id, kill.steam_id or kill.player_id))
    conn.commit()
    conn.close()
    return {"status": "ok", "reward": kill.gold_reward}

@app.post("/api/blueprint/unlock")
def unlock_blueprint(data: BlueprintUnlock):
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    c.execute("SELECT blueprints FROM players WHERE id=?", (data.player_id,))
    row = c.fetchone()
    if not row:
        raise HTTPException(404, "Player not found")
    bps = json.loads(row[0] or "[]")
    if data.blueprint_id not in bps:
        bps.append(data.blueprint_id)
        c.execute("UPDATE players SET blueprints=? WHERE id=?", (json.dumps(bps), data.player_id))
        conn.commit()
    conn.close()
    return {"blueprints": bps}

# Mechanic 15: Campaign
@app.get("/api/campaign/villages")
def get_villages():
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    c.execute("SELECT * FROM villages")
    rows = c.fetchall()
    conn.close()
    return [{"id": r[0], "name": r[1], "owner": r[2], "defense": r[3], "x": r[4], "y": r[5], "won": r[6], "lost": r[7]} for r in rows]

@app.post("/api/campaign/battle")
def campaign_battle(battle: VillageBattle):
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    if battle.won:
        c.execute("UPDATE villages SET battles_won = battles_won + 1, owner='swadia', defense_level = defense_level + 1 WHERE id=?", (battle.village_id,))
        # Reward players
        for pid in battle.players:
            c.execute("UPDATE players SET gold = gold + 200, season_points = season_points + 10 WHERE id=?", (pid,))
    else:
        c.execute("UPDATE villages SET battles_lost = battles_lost + 1, owner='nords', defense_level = MAX(1, defense_level -1) WHERE id=?", (battle.village_id,))
    conn.commit()
    conn.close()
    return {"village_id": battle.village_id, "won": battle.won}

@app.get("/api/season/current")
def current_season():
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    c.execute("SELECT * FROM seasons ORDER BY id DESC LIMIT 1")
    row = c.fetchone()
    conn.close()
    if not row:
        raise HTTPException(404, "No season")
    return {"id": row[0], "name": row[1], "start": row[2], "end": row[3]}

@app.get("/api/leaderboard")
def leaderboard():
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    c.execute("SELECT name, kills, gold, level, season_points FROM players ORDER BY season_points DESC LIMIT 20")
    rows = c.fetchall()
    conn.close()
    return [{"name": r[0], "kills": r[1], "gold": r[2], "level": r[3], "season_points": r[4]} for r in rows]

@app.get("/api/battlepass/rewards")
def bp_rewards():
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    c.execute("SELECT * FROM battlepass_rewards ORDER BY level")
    rows = c.fetchall()
    conn.close()
    return [{"level": r[0], "type": r[1], "id": r[2], "name": r[3]} for r in rows]

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
