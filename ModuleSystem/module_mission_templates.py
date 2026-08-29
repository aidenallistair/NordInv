# module_mission_templates.py - Nord Invasion mission template
# Add this to your module_mission_templates.py

from module_constants import *

# Triggers common
ni_common_triggers = [
  # Before mission start
  (ti_before_mission_start, 0, 0, [],
   [
     (call_script, "script_nord_invasion_init"),
     (assign, "$g_multiplayer_ready_for_spawning_agent", 1),
   ]),

  # On agent spawn - equip bots, set slots
  (ti_on_agent_spawn, 0, 0, [],
   [
     (store_trigger_param_1, ":agent_no"),
     (agent_get_troop_id, ":troop_id", ":agent_no"),
     
     # Check if nord bot
     (try_begin),
       (is_between, ":troop_id", "trp_ni_nord_peasant", "trp_ni_nord_berserker_chief"),
       (agent_set_slot, ":agent_no", slot_agent_ni_is_bot, 1),
       (agent_set_team, ":agent_no", ni_team_nords),
       # Make bots charge
       (agent_ai_set_simple_behaviour, ":agent_no", 0), # charge
       (agent_set_is_alarmed, ":agent_no", 1),
     (else_try),
       (agent_set_slot, ":agent_no", slot_agent_ni_is_bot, 0),
       (agent_set_team, ":agent_no", ni_team_defenders),
     (try_end),
   ]),

  # On agent killed
  (ti_on_agent_killed_or_wounded, 0, 0, [],
   [
     (store_trigger_param_1, ":dead_agent_no"),
     (store_trigger_param_2, ":killer_agent_no"),
     (store_trigger_param_3, ":is_wounded"),
     
     (try_begin),
       (eq, ":is_wounded", 0), # actually killed, not wounded
       
       (agent_get_slot, ":is_bot", ":dead_agent_no", slot_agent_ni_is_bot),
       
       (try_begin),
         # Bot killed by player
         (eq, ":is_bot", 1),
         (agent_get_player_id, ":killer_player", ":killer_agent_no"),
         (try_begin),
           (player_is_active, ":killer_player"),
           (call_script, "script_nord_invasion_on_bot_killed", ":killer_player", ":dead_agent_no", ":killer_agent_no"),
         (else_try),
           # No killer player - still decrease counter
           (val_sub, "$g_ni_bots_alive", 1),
           (val_max, "$g_ni_bots_alive", 0),
           (try_begin),
             (le, "$g_ni_bots_alive", 0),
             (call_script, "script_nord_invasion_wave_completed"),
           (try_end),
         (try_end),
       (else_try),
         # Player killed
         (agent_is_human, ":dead_agent_no"),
         (agent_get_player_id, ":dead_player", ":dead_agent_no"),
         (try_begin),
           (player_is_active, ":dead_player"),
           (player_set_slot, ":dead_player", slot_player_ni_is_dead, 1),
           (call_script, "script_nord_invasion_check_defeat"),
         (try_end),
       (try_end),
     (try_end),
   ]),

  # Player joined
  (ti_server_player_joined, 0, 0, [],
   [
     (store_trigger_param_1, ":player_no"),
     (player_set_slot, ":player_no", slot_player_ni_gold, 500),
     (player_set_slot, ":player_no", slot_player_ni_is_dead, 0),
     (player_set_slot, ":player_no", slot_player_ni_kills, 0),
   ]),

  # Main loop - check wave timers every 1 sec
  (1, 0, 0, [],
   [
     (store_mission_timer_a, ":cur_time"),
     
     # If preparing and time reached -> spawn
     (try_begin),
       (eq, "$g_ni_wave_state", 1), # preparing
       (ge, ":cur_time", "$g_ni_next_wave_time"),
       (call_script, "script_nord_invasion_spawn_bots"),
     (try_end),
     
     # If respawn wave and wave completed -> respawn all dead
     (try_begin),
       (eq, "$g_ni_wave_state", 1),
       (eq, "$g_ni_is_respawn_wave", 1),
       (ge, ":cur_time", "$g_ni_next_wave_time"),
       (call_script, "script_nord_invasion_respawn_all_dead"),
     (try_end),
     
     # Update HUD for all players
     # In real mod you would use presentations or multiplayer_send_*
     # Simplified: server messages
   ]),

  # Check defeat every 2 sec
  (2, 0, 0, [],
   [
     (call_script, "script_nord_invasion_check_defeat"),
   ]),

  # Common battle triggers (from Native, needed)
  (ti_on_multiplayer_mission_end, 0, 0, [],
   [
     (assign, "$g_multiplayer_ready_for_spawning_agent", 0),
   ]),

  # Allow spawning
  (ti_server_player_joined, 0, 0, [],
   [
     (assign, "$g_multiplayer_ready_for_spawning_agent", 1),
   ]),
]

# Main mission template
mission_templates = [
  (
    "mp_nord_invasion", mtf_battle_mode, -1,
    "Nord Invasion - defend against waves",
    [
      # Defenders - players
      (0, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (1, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (2, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (3, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (4, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (5, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (6, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (7, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (8, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (9, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (10, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (11, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (12, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (13, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (14, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (15, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (16, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (17, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (18, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (19, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (20, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (21, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (22, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (23, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (24, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (25, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (26, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (27, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (28, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (29, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (30, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      (31, mtef_defenders|mtef_use_exact_number, 0, aif_start_alarmed, 32, []),
      
      # Attackers - Nords bots - 32 entry points
      (32, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (33, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (34, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (35, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (36, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (37, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (38, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (39, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (40, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (41, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (42, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (43, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (44, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (45, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (46, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (47, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (48, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (49, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (50, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (51, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (52, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (53, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (54, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (55, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (56, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (57, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (58, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (59, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (60, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (61, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (62, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      (63, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 5, []),
      # Boss entry
      (64, mtef_attackers|mtef_use_exact_number, af_override_horse, aif_start_alarmed, 1, []),
    ],
    ni_common_triggers +
    [
      # Presentation trigger for shop - when player presses F on chest
      (ti_on_scene_prop_use, 0, 0,
       [
         (store_trigger_param_1, ":agent_id"),
         (store_trigger_param_2, ":prop_id"),
         (scene_prop_get_slot, ":is_armory", ":prop_id", slot_scene_prop_ni_is_barricade),
         # Actually check if prop is armory chest - simplified
       ],
       [
         (store_trigger_param_1, ":agent_id"),
         (agent_get_player_id, ":player_no", ":agent_id"),
         (try_begin),
           (player_is_active, ":player_no"),
           # Open shop presentation
           (multiplayer_send_int_to_player, ":player_no", 1, 0), # open shop event
         (try_end),
       ]),
    ]
  ),
]
