# module_scripts.py - Nord Invasion core logic
# Add to your module_scripts.py

scripts = [
  # script_nord_invasion_init
  ("nord_invasion_init",
   [
     (assign, "$g_ni_wave_number", 1),
     (assign, "$g_ni_bots_alive", 0),
     (assign, "$g_ni_bots_total", 0),
     (assign, "$g_ni_wave_state", 0), # idle
     (assign, "$g_ni_next_wave_time", 0),
     (assign, "$g_ni_is_respawn_wave", 0),
     (assign, "$g_ni_players_alive", 0),
     
     # Init players gold if not set
     (try_for_range, ":player_no", 0, 200),
       (player_is_active, ":player_no"),
       (player_set_slot, ":player_no", slot_player_ni_gold, 500),
       (player_set_slot, ":player_no", slot_player_ni_is_dead, 0),
       (player_set_slot, ":player_no", slot_player_ni_kills, 0),
     (try_end),
     
     (display_message, "@Nord Invasion initialized! Wave 1 incoming..."),
     (call_script, "script_nord_invasion_setup_wave", "$g_ni_wave_number"),
   ]),

  # script_nord_invasion_setup_wave
  # Input: arg1 = wave number
  ("nord_invasion_setup_wave",
   [
     (store_script_param, ":wave_no", 1),
     
     (assign, "$g_ni_wave_number", ":wave_no"),
     (assign, "$g_ni_wave_state", 1), # preparing
     
     # Check if respawn wave
     (store_mod, ":is_respawn", ":wave_no", 4),
     (try_begin),
       (eq, ":is_respawn", 0),
       (assign, "$g_ni_is_respawn_wave", 1),
     (else_try),
       (assign, "$g_ni_is_respawn_wave", 0),
     (try_end),
     
     # Calculate bot count - scales with players and wave
     (assign, ":num_players", 0),
     (try_for_range, ":player_no", 0, 200),
       (player_is_active, ":player_no"),
       (val_add, ":num_players", 1),
     (try_end),
     
     (store_add, ":base_count", 8, ":wave_no"), # wave 1 = 9 bots, wave 10 = 18
     (val_mul, ":base_count", ":num_players"),
     (val_div, ":base_count", 3), # divide for balance, min 10
     (val_max, ":base_count", 10),
     (val_min, ":base_count", 60), # cap
     
     (assign, "$g_ni_bots_total", ":base_count"),
     (assign, "$g_ni_bots_alive", ":base_count"),
     
     # Determine composition based on wave
     # This is simplified - real NI uses table
     (try_begin),
       (lt, ":wave_no", 4),
       (assign, "$g_ni_bot_tier_0", "trp_ni_nord_peasant"),
       (assign, "$g_ni_bot_tier_1", "trp_ni_nord_footman"),
       (assign, "$g_ni_bot_tier_2", -1),
     (else_try),
       (lt, ":wave_no", 8),
       (assign, "$g_ni_bot_tier_0", "trp_ni_nord_footman"),
       (assign, "$g_ni_bot_tier_1", "trp_ni_nord_archer"),
       (assign, "$g_ni_bot_tier_2", "trp_ni_nord_veteran"),
     (else_try),
       (lt, ":wave_no", 12),
       (assign, "$g_ni_bot_tier_0", "trp_ni_nord_veteran"),
       (assign, "$g_ni_bot_tier_1", "trp_ni_nord_huscarl"),
       (assign, "$g_ni_bot_tier_2", "trp_ni_nord_berserker"),
     (else_try),
       (assign, "$g_ni_bot_tier_0", "trp_ni_nord_huscarl"),
       (assign, "$g_ni_bot_tier_1", "trp_ni_nord_jarl_guard"),
       (assign, "$g_ni_bot_tier_2", "trp_ni_nord_chieftain"),
     (try_end),
     
     # Set timer for next wave spawn
     (store_mission_timer_a, ":cur_time"),
     (store_add, "$g_ni_next_wave_time", ":cur_time", 8), # 8 sec prep
     
     # Announce
     (try_begin),
       (eq, "$g_ni_is_respawn_wave", 1),
       (multiplayer_send_string_to_player, -1, 0, "@RESPAWN WAVE! All dead will be revived!"),
     (try_end),
     
     (assign, reg1, ":wave_no"),
     (assign, reg2, "$g_ni_bots_total"),
     (display_message, "@Wave {reg1} preparing... {reg2} Nords incoming!"),
     
     # For all players show message
     (multiplayer_send_int_to_player, -1, 0, ":wave_no"), # event: wave info
   ]),

  # script_nord_invasion_spawn_bots
  ("nord_invasion_spawn_bots",
   [
     (assign, "$g_ni_wave_state", 2), # spawning
     
     (assign, ":bots_to_spawn", "$g_ni_bots_total"),
     (assign, ":entry_no", 32), # start from attacker entries
     
     (try_for_range, ":i", 0, ":bots_to_spawn"),
       # Choose troop type - weighted
       (store_random_in_range, ":rand", 0, 100),
       (try_begin),
         (lt, ":rand", 60),
         (assign, ":troop_id", "$g_ni_bot_tier_0"),
       (else_try),
         (lt, ":rand", 90),
         (assign, ":troop_id", "$g_ni_bot_tier_1"),
         (try_begin),
           (lt, ":troop_id", 0),
           (assign, ":troop_id", "$g_ni_bot_tier_0"),
         (try_end),
       (else_try),
         (assign, ":troop_id", "$g_ni_bot_tier_2"),
         (try_begin),
           (lt, ":troop_id", 0),
           (assign, ":troop_id", "$g_ni_bot_tier_0"),
         (try_end),
       (try_end),
       
       # Add visitor
       (store_add, ":entry", 32, ":i"),
       (val_mod, ":entry", 32), # 32-63
       (val_add, ":entry", 32),
       
       # Spawn via server operation - WSE enhanced
       (add_visitors_to_current_scene, ":entry", ":troop_id", 1, 1, 0), # team 1, group
     (try_end),
     
     # Boss wave every 5 waves
     (store_mod, ":is_boss", "$g_ni_wave_number", 5),
     (try_begin),
       (eq, ":is_boss", 0),
       (add_visitors_to_current_scene, 64, "trp_ni_nord_chieftain", 1, 1, 0),
       (val_add, "$g_ni_bots_total", 1),
       (val_add, "$g_ni_bots_alive", 1),
       (display_message, "@BOSS SPAWNED!"),
     (try_end),
     
     (assign, "$g_ni_wave_state", 3), # in progress
     (display_message, "@Wave started! Kill all Nords!"),
   ]),

  # script_nord_invasion_on_bot_killed
  # Input: killer player, killed agent
  ("nord_invasion_on_bot_killed",
   [
     (store_script_param, ":killer_player_no", 1),
     (store_script_param, ":killed_agent_no", 2),
     (store_script_param, ":killer_agent_no", 3),
     
     # Decrease alive counter
     (val_sub, "$g_ni_bots_alive", 1),
     (val_max, "$g_ni_bots_alive", 0),
     
     # Reward killer
     (try_begin),
       (player_is_active, ":killer_player_no"),
       (agent_get_troop_id, ":killed_troop", ":killed_agent_no"),
       
       # Determine gold value by troop tier
       (try_begin),
         (eq, ":killed_troop", "trp_ni_nord_peasant"),
         (assign, ":gold", 3),
       (else_try),
         (eq, ":killed_troop", "trp_ni_nord_footman"),
         (assign, ":gold", 6),
       (else_try),
         (eq, ":killed_troop", "trp_ni_nord_archer"),
         (assign, ":gold", 7),
       (else_try),
         (eq, ":killed_troop", "trp_ni_nord_veteran"),
         (assign, ":gold", 10),
       (else_try),
         (eq, ":killed_troop", "trp_ni_nord_huscarl"),
         (assign, ":gold", 15),
       (else_try),
         (eq, ":killed_troop", "trp_ni_nord_berserker"),
         (assign, ":gold", 20),
       (else_try),
         (eq, ":killed_troop", "trp_ni_nord_jarl_guard"),
         (assign, ":gold", 35),
       (else_try),
         (assign, ":gold", 100), # boss
       (try_end),
       
       (player_get_slot, ":cur_gold", ":killer_player_no", slot_player_ni_gold),
       (val_add, ":cur_gold", ":gold"),
       (player_set_slot, ":killer_player_no", slot_player_ni_gold, ":cur_gold"),
       
       (player_get_slot, ":kills", ":killer_player_no", slot_player_ni_kills),
       (val_add, ":kills", 1),
       (player_set_slot, ":killer_player_no", slot_player_ni_kills, ":kills"),
       
       # Show gold message only to killer
       (assign, reg1, ":gold"),
       (assign, reg2, ":cur_gold"),
       (multiplayer_send_string_to_player, ":killer_player_no", 0, "@+{reg1} gold! Total: {reg2}"),
     (try_end),
     
     # Check if wave completed
     (try_begin),
       (le, "$g_ni_bots_alive", 0),
       (call_script, "script_nord_invasion_wave_completed"),
     (try_end),
   ]),

  # script_nord_invasion_wave_completed
  ("nord_invasion_wave_completed",
   [
     (assign, "$g_ni_wave_state", 4), # completed
     (display_message, "@Wave completed!"),
     
     # Reward all alive players bonus
     (try_for_range, ":player_no", 0, 200),
       (player_is_active, ":player_no"),
       (player_get_slot, ":is_dead", ":player_no", slot_player_ni_is_dead),
       (eq, ":is_dead", 0),
       (player_get_slot, ":gold", ":player_no", slot_player_ni_gold),
       (val_add, ":gold", 20), # wave bonus
       (player_set_slot, ":player_no", slot_player_ni_gold, ":gold"),
     (try_end),
     
     # Check max waves -> victory
     (try_begin),
       (ge, "$g_ni_wave_number", 25),
       (display_message, "@VICTORY! All waves defeated!"),
       (assign, "$g_ni_wave_state", 5),
       # End mission after 10 sec
       (store_mission_timer_a, ":time"),
       (store_add, "$g_ni_next_wave_time", ":time", 10),
     (else_try),
       # Setup next wave
       (val_add, "$g_ni_wave_number", 1),
       (call_script, "script_nord_invasion_setup_wave", "$g_ni_wave_number"),
     (try_end),
   ]),

  # script_nord_invasion_check_defeat
  ("nord_invasion_check_defeat",
   [
     # Count alive players
     (assign, ":alive_players", 0),
     (assign, ":total_players", 0),
     (try_for_range, ":player_no", 0, 200),
       (player_is_active, ":player_no"),
       (val_add, ":total_players", 1),
       (player_get_slot, ":is_dead", ":player_no", slot_player_ni_is_dead),
       (eq, ":is_dead", 0),
       (val_add, ":alive_players", 1),
     (try_end),
     
     (assign, "$g_ni_players_alive", ":alive_players"),
     
     # If no alive players and not respawn wave -> defeat
     (try_begin),
       (eq, ":alive_players", 0),
       (gt, ":total_players", 0),
       (neq, "$g_ni_is_respawn_wave", 1),
       (neq, "$g_ni_wave_state", 4), # not already completed
       (display_message, "@All players dead! Defeat!"),
       (assign, "$g_ni_wave_state", 5), # failed
       # TODO: end mission
     (try_end),
   ]),

  # script_nord_invasion_player_respawn
  ("nord_invasion_player_respawn",
   [
     (store_script_param, ":player_no", 1),
     
     (player_get_slot, ":is_dead", ":player_no", slot_player_ni_is_dead),
     (try_begin),
       (eq, ":is_dead", 1),
       # Check if respawn wave or enough time passed
       (try_begin),
         (eq, "$g_ni_is_respawn_wave", 1),
         (player_set_slot, ":player_no", slot_player_ni_is_dead, 0),
         # Respawn agent
         (player_get_team_no, ":team", ":player_no"),
         (player_spawn_new_agent, ":player_no", ":team"),
       (try_end),
     (try_end),
   ]),

  # script_nord_invasion_respawn_all_dead
  ("nord_invasion_respawn_all_dead",
   [
     (try_for_range, ":player_no", 0, 200),
       (player_is_active, ":player_no"),
       (player_get_slot, ":is_dead", ":player_no", slot_player_ni_is_dead),
       (eq, ":is_dead", 1),
       (player_set_slot, ":player_no", slot_player_ni_is_dead, 0),
       (player_get_team_no, ":team", ":player_no"),
       (player_spawn_new_agent, ":player_no", ":team"),
       (multiplayer_send_string_to_player, ":player_no", 0, "@You have been respawned!"),
     (try_end),
     (display_message, "@All dead players respawned!"),
   ]),

  # Shop - buy item
  ("nord_invasion_buy_item",
   [
     (store_script_param, ":player_no", 1),
     (store_script_param, ":item_id", 2),
     (store_script_param, ":price", 3),
     
     (player_get_slot, ":gold", ":player_no", slot_player_ni_gold),
     (try_begin),
       (ge, ":gold", ":price"),
       (val_sub, ":gold", ":price"),
       (player_set_slot, ":player_no", slot_player_ni_gold, ":gold"),
       # Give item to player - via agent equip
       (player_get_agent_id, ":agent_id", ":player_no"),
       (try_begin),
         (agent_is_active, ":agent_id"),
         (agent_is_alive, ":agent_id"),
         (agent_equip_item, ":agent_id", ":item_id"),
       (try_end),
       (assign, reg1, ":price"),
       (display_message, "@Item bought for {reg1} gold!"),
     (else_try),
       (multiplayer_send_string_to_player, ":player_no", 0, "@Not enough gold!"),
     (try_end),
   ]),
]
