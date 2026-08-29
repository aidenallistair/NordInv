# Nord Invasion Better Edition - module_constants.py
# Complete constants for all 15 mechanics

# Game Types
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

# Player slots (100+)
slot_player_ni_gold              = 100
slot_player_ni_kills             = 101
slot_player_ni_deaths            = 102
slot_player_ni_is_dead           = 103
slot_player_ni_spawned_this_wave = 104
slot_player_ni_assist_xp         = 105
slot_player_ni_class             = 106  # 3. Roles
slot_player_ni_wood              = 107  # 2,9 resources
slot_player_ni_metal             = 108
slot_player_ni_cloth             = 109
slot_player_ni_fallen_count      = 110  # 13 wounds
slot_player_ni_stamina           = 111  # 13 stamina
slot_player_ni_perk_1            = 112  # 1 perks, 10 slots
slot_player_ni_perk_2            = 113
slot_player_ni_perk_3            = 114
slot_player_ni_perk_4            = 115
slot_player_ni_perk_5            = 116
slot_player_ni_perk_6            = 117
slot_player_ni_perk_7            = 118
slot_player_ni_perk_8            = 119
slot_player_ni_blueprints        = 120  # 12 persistence
slot_player_ni_steam_id          = 121
slot_player_ni_is_carrying_loot  = 122  # 8 loot bag

# Agent slots (200+)
slot_agent_ni_is_bot             = 200
slot_agent_ni_bot_tier           = 201
slot_agent_ni_gold_value         = 202
slot_agent_ni_squad_id           = 203  # 11 squads
slot_agent_ni_is_squad_leader    = 204
slot_agent_ni_wounds             = 205  # 13
slot_agent_ni_stamina            = 206
slot_agent_ni_is_fallen          = 207
slot_agent_ni_perk_damage_mod    = 208  # 1 perks applied to agent
slot_agent_ni_perk_hp_mod        = 209
slot_agent_ni_carrying_loot      = 210  # 8
slot_agent_ni_class              = 211  # 3

# Scene prop slots (300+)
slot_scene_prop_ni_health        = 300
slot_scene_prop_ni_max_health    = 301
slot_scene_prop_ni_is_barricade  = 302
slot_scene_prop_ni_owner         = 303
slot_scene_prop_ni_type          = 304  # 2 fortress types
slot_scene_prop_ni_is_burning    = 305  # 14 fire
slot_scene_prop_ni_burn_time     = 306
slot_scene_prop_ni_gold_value    = 307  # 8 loot
slot_scene_prop_ni_is_loot       = 308
slot_scene_prop_ni_is_treasury   = 309
slot_scene_prop_ni_is_ram        = 310  # 4 objectives
slot_scene_prop_ni_is_stakes     = 311  # 7 anti-cav
slot_scene_prop_ni_is_oil        = 312  # 2 oil cauldron

# Classes (Mechanic 3)
ni_class_infantry   = 0
ni_class_archer     = 1
ni_class_medic      = 2
ni_class_engineer   = 3
ni_class_banner     = 4

# Fortress types (Mechanic 2)
ni_fort_foundation  = 0
ni_fort_wall_wood   = 1
ni_fort_wall_door   = 2
ni_fort_wall_window = 3
ni_fort_stakes      = 4
ni_fort_oil_cauldron= 5
ni_fort_brazier     = 6
ni_fort_spike_trap  = 7
ni_fort_shield_wall = 8

# Wave objectives (Mechanic 4)
ni_objective_kill_all       = 0
ni_objective_destroy_ram    = 1
ni_objective_escort         = 2
ni_objective_burn_camps     = 3
ni_objective_defend_treasury= 4

# Weather (Mechanic 6)
ni_weather_clear    = 0
ni_weather_fog      = 1
ni_weather_rain     = 2
ni_weather_snow     = 3
ni_weather_night    = 4

# Mutators (Mechanic 10) - Gods curses
ni_mutator_none             = 0
ni_mutator_berserk          = 1  # Thor - all berserk, no block, +50% speed
ni_mutator_hidden_archers   = 2  # Skadi - archers invisible in fog
ni_mutator_greedy           = 3  # Loki - gold x2 but hit steals gold
ni_mutator_marked           = 4  # Odin - one player marked, all bots chase him
ni_mutator_shieldwall       = 5  # All nords in shieldwall squads
ni_mutator_cavalry_rush     = 6  # Only cavalry
ni_mutator_poison           = 7  # All attacks poison, need medic
ni_mutator_darkness         = 8  # Night, only torches
ni_mutator_fortified        = 9  # Nords have their own barricades
ni_mutator_boss_rush        = 10 # 3 bosses at once
ni_mutator_no_ammo          = 11 # No ammo resupply
ni_mutator_heavy_rain       = 12 # Bows -50%

# Squads (Mechanic 11)
ni_squad_shieldwall         = 0
ni_squad_berserk_wedge      = 1
ni_squad_archer_cover       = 2
ni_squad_cavalry_flank      = 3

# Economy
ni_start_gold = 500
ni_respawn_interval = 4
ni_max_waves = 25
ni_wave_prepare_time = 8
ni_max_bots_per_wave_base = 10

# Perks (Mechanic 1) - 30 perks, 3 branches
# Branch Survivor
ni_perk_iron_skin_1      = 0  # +15% HP
ni_perk_iron_skin_2      = 1  # +30% HP
ni_perk_regen            = 2  # regen outside combat
ni_perk_second_wind      = 3  # second chance when fallen
ni_perk_tough            = 4  # -20% damage taken
# Branch Berserk
ni_perk_bloodlust        = 10 # damage +10% per 20% lost HP
ni_perk_vampirism        = 11 # 5% lifesteal
ni_perk_frenzy           = 12 # attack speed +20% after kill
ni_perk_executioner      = 13 # +50% damage to bosses below 30% HP
# Branch Tactician
ni_perk_engineer_1       = 20 # barricades +30% HP
ni_perk_engineer_2       = 21 # barricades +50% HP, repair faster
ni_perk_gold_hunter      = 22 # +20% gold team
ni_perk_banner_master    = 23 # banner radius x2
ni_perk_scavenger        = 24 # +50% resources from scavenging

# Wounds (Mechanic 13)
ni_wound_healthy         = 0
ni_wound_injured         = 1
ni_wound_fallen          = 2
ni_wound_dead            = 3

# Campaign (Mechanic 15)
ni_campaign_village_1 = 0
ni_campaign_village_2 = 1
ni_campaign_village_3 = 2
ni_campaign_village_4 = 3
ni_campaign_village_5 = 4
ni_campaign_village_6 = 5
ni_campaign_village_7 = 6
ni_campaign_village_8 = 7

# Global variables (used as $g_...)
# $g_ni_wave_number
# $g_ni_bots_alive
# $g_ni_bots_total
# $g_ni_next_wave_time
# $g_ni_is_respawn_wave
# $g_ni_players_alive
# $g_ni_wave_state
# $g_ni_wave_objective
# $g_ni_weather
# $g_ni_mutator
# $g_ni_director_stress
# $g_ni_marked_player
# $g_ni_ram_instance
# $g_ni_treasury_instance
