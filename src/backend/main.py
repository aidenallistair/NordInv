"""
Nord Invasion Backend - persistence API
Fianna-style simplified persistence
Original NI used PHP + MySQL, here FastAPI + SQLite for simplicity

WSE on server can call:
  wse_http_get "http://backend:8000/api/player/123" -> gets player data
  wse_http_post "http://backend:8000/api/kill" with data

This is optional - Fianna version can work without backend (session-only gold)
"""

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import Optional
import sqlite3
import os

app = FastAPI(title="Fianna Nord Invasion Backend")

DB_PATH = "ni_players.db"

def init_db():
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    c.execute('''CREATE TABLE IF NOT EXISTS players
                 (id TEXT PRIMARY KEY, name TEXT, gold INTEGER DEFAULT 500, 
                  kills INTEGER DEFAULT 0, deaths INTEGER DEFAULT 0, level INTEGER DEFAULT 1,
                  xp INTEGER DEFAULT 0)''')
    c.execute('''CREATE TABLE IF NOT EXISTS characters
                 (player_id TEXT, char_name TEXT, class TEXT, gold INTEGER, 
                  inventory TEXT, PRIMARY KEY(player_id, char_name))''')
    conn.commit()
    conn.close()

init_db()

class PlayerKill(BaseModel):
    player_id: str
    player_name: str
    killed_troop: str
    gold_reward: int
    wave: int

class PlayerLogin(BaseModel):
    player_id: str
    player_name: str

@app.get("/")
def root():
    return {"message": "Fianna Nord Invasion Backend", "version": "1.004"}

@app.post("/api/player/login")
def player_login(data: PlayerLogin):
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    c.execute("SELECT * FROM players WHERE id=?", (data.player_id,))
    row = c.fetchone()
    if not row:
        c.execute("INSERT INTO players (id, name) VALUES (?, ?)", (data.player_id, data.player_name))
        conn.commit()
        conn.close()
        return {"id": data.player_id, "name": data.player_name, "gold": 500, "kills": 0, "level": 1, "new": True}
    else:
        conn.close()
        return {"id": row[0], "name": row[1], "gold": row[2], "kills": row[3], "level": row[5], "new": False}

@app.get("/api/player/{player_id}")
def get_player(player_id: str):
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    c.execute("SELECT * FROM players WHERE id=?", (player_id,))
    row = c.fetchone()
    conn.close()
    if not row:
        raise HTTPException(404, "Player not found")
    return {"id": row[0], "name": row[1], "gold": row[2], "kills": row[3], "deaths": row[4], "level": row[5], "xp": row[6]}

@app.post("/api/kill")
def register_kill(kill: PlayerKill):
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    # Add gold
    c.execute("UPDATE players SET gold = gold + ?, kills = kills + 1 WHERE id=?", (kill.gold_reward, kill.player_id))
    # If player not exists, create
    if c.rowcount == 0:
        c.execute("INSERT INTO players (id, name, gold, kills) VALUES (?, ?, ?, 1)",
                  (kill.player_id, kill.player_name, 500 + kill.gold_reward))
    conn.commit()
    conn.close()
    return {"status": "ok", "reward": kill.gold_reward}

@app.post("/api/wave_complete")
def wave_complete(wave: int, players: str):  # players = comma-separated ids
    # Bonus for all alive
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    for pid in players.split(","):
        if pid:
            c.execute("UPDATE players SET gold = gold + 20 WHERE id=?", (pid,))
    conn.commit()
    conn.close()
    return {"wave": wave, "bonus": 20}

@app.get("/api/leaderboard")
def leaderboard():
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    c.execute("SELECT name, kills, gold, level FROM players ORDER BY kills DESC LIMIT 20")
    rows = c.fetchall()
    conn.close()
    return [{"name": r[0], "kills": r[1], "gold": r[2], "level": r[3]} for r in rows]

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
