# Nord Invasion - Fianna edition - module_constants.py
# Add these to your existing module_constants.py or use as standalone

# Game Types
multiplayer_game_type_deathmatch           = 0
multiplayer_game_type_team_deathmatch      = 1
multiplayer_game_type_battle               = 2
multiplayer_game_type_destroy              = 3
multiplayer_game_type_capture_the_flag     = 4
multiplayer_game_type_headquarters         = 5
multiplayer_game_type_siege                = 6
multiplayer_game_type_duel                 = 7
# Custom
multiplayer_game_type_nord_invasion        = 10

# Teams
ni_team_defenders = 0
ni_team_nords     = 1
ni_team_spectator = 2

# Wave states
ni_wave_state_idle        = 0
ni_wave_state_preparing   = 1
ni_wave_state_spawning    = 2
ni_wave_state_in_progress = 3
ni_wave_state_completed   = 4
ni_wave_state_failed      = 5

# Global slots for $g_ variables simulation via globals
# Using global variables defined in module_scripts
# $g_ni_wave_number
# $g_ni_bots_alive
# $g_ni_bots_total
# $g_ni_next_wave_time
# $g_ni_is_respawn_wave

# Player slots (use 100+ to avoid conflicts with Native)
slot_player_ni_gold              = 100
slot_player_ni_kills             = 101
slot_player_ni_deaths            = 102
slot_player_ni_is_dead           = 103
slot_player_ni_spawned_this_wave = 104
slot_player_ni_assist_xp         = 105
slot_player_ni_bought_items      = 106

# Agent slots (200+)
slot_agent_ni_is_bot             = 200
slot_agent_ni_bot_tier           = 201
slot_agent_ni_gold_value         = 202

# Scene prop slots (for barricades)
slot_scene_prop_ni_health        = 300
slot_scene_prop_ni_max_health    = 301
slot_scene_prop_ni_is_barricade  = 302

# Economy
ni_start_gold = 500
ni_gold_per_kill = {
  "peasant": 3,
  "footman": 6,
  "archer": 7,
  "veteran": 10,
  "huscarl": 15,
  "berserker": 20,
  "jarl": 35,
  "boss": 100,
}

ni_respawn_interval = 4  # every 4 waves
ni_max_waves = 25
ni_wave_prepare_time = 8  # seconds between waves
ni_max_bots_per_wave_base = 10

# Bot tiers definition
ni_bot_tiers = [
  "trp_ni_nord_peasant",
  "trp_ni_nord_footman",
  "trp_ni_nord_archer",
  "trp_ni_nord_veteran",
  "trp_ni_nord_huscarl",
  "trp_ni_nord_berserker",
  "trp_ni_nord_jarl_guard",
  "trp_ni_nord_chieftain", # boss
]

# Map entry points
ni_entry_defenders_begin = 0
ni_entry_defenders_end   = 32
ni_entry_nords_begin     = 32
ni_entry_nords_end       = 64
ni_entry_boss            = 64

# Presentation IDs
prsnt_ni_shop = "prsnt_ni_shop"
prsnt_ni_wave_info = "prsnt_ni_wave_info"
