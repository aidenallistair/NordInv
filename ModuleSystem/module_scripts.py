# Better Edition - all 15 mechanics scripts

scripts = [
  # Core init
  ("nord_invasion_init", [
    (assign, "$g_ni_wave_number", 1), (assign, "$g_ni_bots_alive", 0), (assign, "$g_ni_bots_total", 0),
    (assign, "$g_ni_wave_state", 0), (assign, "$g_ni_next_wave_time", 0), (assign, "$g_ni_is_respawn_wave", 0),
    (assign, "$g_ni_players_alive", 0), (assign, "$g_ni_wave_objective", 0), (assign, "$g_ni_weather", 0),
    (assign, "$g_ni_mutator", 0), (assign, "$g_ni_director_stress", 50), (assign, "$g_ni_marked_player", -1),
    (try_for_range, ":player_no", 0, 200), (player_is_active, ":player_no"),
      (player_set_slot, ":player_no", 100, 500), (player_set_slot, ":player_no", 103, 0),
      (player_set_slot, ":player_no", 101, 0), (player_set_slot, ":player_no", 106, 0),
      (player_set_slot, ":player_no", 107, 0), (player_set_slot, ":player_no", 108, 0),
      (player_set_slot, ":player_no", 110, 0), (player_set_slot, ":player_no", 122, 0),
    (try_end),
    (display_message, "@Nord Invasion Better Edition initialized!"),
    (call_script, "script_nord_invasion_setup_wave", 1),
  ]),

  # Setup wave with objectives, mutators, weather, director
  ("nord_invasion_setup_wave", [
    (store_script_param, ":wave_no", 1),
    (assign, "$g_ni_wave_number", ":wave_no"), (assign, "$g_ni_wave_state", 1),
    (store_mod, ":is_respawn", ":wave_no", 4), (try_begin), (eq, ":is_respawn", 0), (assign, "$g_ni_is_respawn_wave", 1), (else_try), (assign, "$g_ni_is_respawn_wave", 0), (try_end),

    # Mechanic 4: Objective every 3rd wave
    (store_mod, ":obj_mod", ":wave_no", 3), (try_begin), (eq, ":obj_mod", 0), (store_random_in_range, "$g_ni_wave_objective", 1, 5), (else_try), (assign, "$g_ni_wave_objective", 0), (try_end),

    # Mechanic 10: Mutator every 4th wave
    (store_mod, ":mut_mod", ":wave_no", 4), (try_begin), (eq, ":mut_mod", 0), (store_random_in_range, "$g_ni_mutator", 1, 13), (else_try), (assign, "$g_ni_mutator", 0), (try_end),
    # Mechanic 10: Marked player for Odin
    (try_begin), (eq, "$g_ni_mutator", 4), (store_random_in_range, "$g_ni_marked_player", 0, 200), (try_end),

    # Mechanic 6: Weather every 5 waves
    (store_mod, ":weath_mod", ":wave_no", 5), (try_begin), (eq, ":weath_mod", 0), (store_random_in_range, "$g_ni_weather", 0, 5), (call_script, "script_ni_set_weather", "$g_ni_weather"), (try_end),

    # Mechanic 5: Director affects bot count
    (assign, ":num_players", 0), (try_for_range, ":player_no", 0, 200), (player_is_active, ":player_no"), (val_add, ":num_players", 1), (try_end),
    (store_add, ":base_count", 8, ":wave_no"), (val_mul, ":base_count", ":num_players"), (val_div, ":base_count", 3), (val_max, ":base_count", 10), (val_min, ":base_count", 60),
    # Director stress modifier
    (try_begin), (gt, "$g_ni_director_stress", 80), (val_mul, ":base_count", 12), (val_div, ":base_count", 10), (else_try), (lt, "$g_ni_director_stress", 30), (val_mul, ":base_count", 8), (val_div, ":base_count", 10), (try_end),
    # Mutator boss rush
    (try_begin), (eq, "$g_ni_mutator", 10), (val_add, ":base_count", 5), (try_end),

    (assign, "$g_ni_bots_total", ":base_count"), (assign, "$g_ni_bots_alive", ":base_count"),

    # Tier selection
    (try_begin), (lt, ":wave_no", 4), (assign, "$g_ni_bot_tier_0", "trp_ni_nord_peasant"), (assign, "$g_ni_bot_tier_1", "trp_ni_nord_footman"), (assign, "$g_ni_bot_tier_2", -1),
    (else_try), (lt, ":wave_no", 8), (assign, "$g_ni_bot_tier_0", "trp_ni_nord_footman"), (assign, "$g_ni_bot_tier_1", "trp_ni_nord_archer"), (assign, "$g_ni_bot_tier_2", "trp_ni_nord_veteran"),
    (else_try), (lt, ":wave_no", 12), (assign, "$g_ni_bot_tier_0", "trp_ni_nord_veteran"), (assign, "$g_ni_bot_tier_1", "trp_ni_nord_huscarl"), (assign, "$g_ni_bot_tier_2", "trp_ni_nord_berserker"),
    (else_try), (assign, "$g_ni_bot_tier_0", "trp_ni_nord_huscarl"), (assign, "$g_ni_bot_tier_1", "trp_ni_nord_jarl_guard"), (assign, "$g_ni_bot_tier_2", "trp_ni_nord_chieftain"),
    (try_end),

    (store_mission_timer_a, ":cur_time"), (store_add, "$g_ni_next_wave_time", ":cur_time", 8),
    (assign, reg1, ":wave_no"), (assign, reg2, "$g_ni_bots_total"), (display_message, "@Wave {reg1} preparing... {reg2} Nords! Objective: {reg3} Mutator: {reg4}"),
    (assign, reg3, "$g_ni_wave_objective"), (assign, reg4, "$g_ni_mutator"),
  ]),

  # Spawn bots with squads, cav, mutators
  ("nord_invasion_spawn_bots", [
    (assign, "$g_ni_wave_state", 2),
    (assign, ":bots_to_spawn", "$g_ni_bots_total"),

    # Mechanic 11: If shieldwall mutator or random squad wave, spawn squads
    (store_mod, ":squad_wave", "$g_ni_wave_number", 3), (try_begin), (eq, ":squad_wave", 0), (eq, "$g_ni_wave_objective", 0),
      (call_script, "script_ni_spawn_squad", 0, 32), (call_script, "script_ni_spawn_squad", 0, 40),
      (val_sub, ":bots_to_spawn", 16),
    (try_end),

    (try_for_range, ":i", 0, ":bots_to_spawn"),
      (store_random_in_range, ":rand", 0, 100),
      (try_begin), (lt, ":rand", 60), (assign, ":troop_id", "$g_ni_bot_tier_0"),
      (else_try), (lt, ":rand", 90), (assign, ":troop_id", "$g_ni_bot_tier_1"), (try_begin), (lt, ":troop_id", 0), (assign, ":troop_id", "$g_ni_bot_tier_0"), (try_end),
      (else_try), (assign, ":troop_id", "$g_ni_bot_tier_2"), (try_begin), (lt, ":troop_id", 0), (assign, ":troop_id", "$g_ni_bot_tier_0"), (try_end),
      (try_end),

      # Mechanic 7: Cavalry 20% after wave 10
      (try_begin), (ge, "$g_ni_wave_number", 10), (store_random_in_range, ":cav_rand", 0, 100), (lt, ":cav_rand", 20),
        (store_random_in_range, ":cav_type", 0, 2), (try_begin), (eq, ":cav_type", 0), (assign, ":troop_id", "trp_ni_nord_raider_mounted"), (else_try), (assign, ":troop_id", "trp_ni_nord_horse_archer"), (try_end),
      (try_end),

      # Mechanic 10: Mutator overrides
      (try_begin), (eq, "$g_ni_mutator", 1), (assign, ":troop_id", "trp_ni_nord_berserker"), (else_try), (eq, "$g_ni_mutator", 6), (assign, ":troop_id", "trp_ni_nord_raider_mounted"), (try_end),

      (store_add, ":entry", 32, ":i"), (val_mod, ":entry", 32), (val_add, ":entry", 32),
      (add_visitors_to_current_scene, ":entry", ":troop_id", 1, 1, 0),
    (try_end),

    # Boss
    (store_mod, ":is_boss", "$g_ni_wave_number", 5), (try_begin), (eq, ":is_boss", 0), (add_visitors_to_current_scene, 64, "trp_ni_nord_chieftain", 1, 1, 0), (val_add, "$g_ni_bots_total", 1), (val_add, "$g_ni_bots_alive", 1), (display_message, "@BOSS SPAWNED!"), (try_end),
    (try_begin), (eq, "$g_ni_mutator", 10), (add_visitors_to_current_scene, 64, "trp_ni_nord_chieftain", 1, 1, 0), (add_visitors_to_current_scene, 64, "trp_ni_nord_berserker_chief", 1, 1, 0), (val_add, "$g_ni_bots_total", 2), (val_add, "$g_ni_bots_alive", 2), (try_end),

    # Mechanic 4: Objectives props
    (try_begin), (eq, "$g_ni_wave_objective", 1), (set_spawn_position, 64), (spawn_scene_prop, "spr_ni_ram"), (assign, "$g_ni_ram_instance", reg0), (display_message, "@OBJECTIVE: Destroy enemy ram!"),
    (else_try), (eq, "$g_ni_wave_objective", 2), (add_visitors_to_current_scene, 0, "trp_ni_villager", 1, 0, 0), (display_message, "@OBJECTIVE: Escort villager!"),
    (else_try), (eq, "$g_ni_wave_objective", 3), (set_spawn_position, 32), (spawn_scene_prop, "spr_ni_camp_nord"), (set_spawn_position, 40), (spawn_scene_prop, "spr_ni_camp_nord"), (set_spawn_position, 48), (spawn_scene_prop, "spr_ni_camp_nord"), (display_message, "@OBJECTIVE: Burn 3 nord camps!"),
    (try_end),

    (assign, "$g_ni_wave_state", 3), (display_message, "@Wave started!"),
  ]),

  # Mechanic 11: Spawn squad
  ("ni_spawn_squad", [
    (store_script_param, ":squad_type", 1), (store_script_param, ":entry_start", 2),
    (try_begin), (eq, ":squad_type", 0), # shieldwall
      (add_visitors_to_current_scene, ":entry_start", "trp_ni_nord_shield_leader", 1, 1, 0),
      (store_add, ":e1", ":entry_start", 1), (add_visitors_to_current_scene, ":e1", "trp_ni_nord_huscarl", 1, 1, 0),
      (store_add, ":e2", ":entry_start", 2), (add_visitors_to_current_scene, ":e2", "trp_ni_nord_huscarl", 1, 1, 0),
      (store_add, ":e3", ":entry_start", 3), (add_visitors_to_current_scene, ":e3", "trp_ni_nord_huscarl", 1, 1, 0),
      (store_add, ":e4", ":entry_start", 4), (add_visitors_to_current_scene, ":e4", "trp_ni_nord_veteran", 1, 1, 0),
      (store_add, ":e5", ":entry_start", 5), (add_visitors_to_current_scene, ":e5", "trp_ni_nord_archer", 1, 1, 0),
      (store_add, ":e6", ":entry_start", 6), (add_visitors_to_current_scene, ":e6", "trp_ni_nord_archer", 1, 1, 0),
      (store_add, ":e7", ":entry_start", 7), (add_visitors_to_current_scene, ":e7", "trp_ni_nord_archer", 1, 1, 0),
    (try_end),
  ]),

  # On bot killed with loot, scavenging, director
  ("nord_invasion_on_bot_killed", [
    (store_script_param, ":killer_player_no", 1), (store_script_param, ":killed_agent_no", 2), (store_script_param, ":killer_agent_no", 3),
    (val_sub, "$g_ni_bots_alive", 1), (val_max, "$g_ni_bots_alive", 0),

    # Director: kill increases stress relief
    (val_sub, "$g_ni_director_stress", 1), (val_max, "$g_ni_director_stress", 0),

    (try_begin), (player_is_active, ":killer_player_no"),
      (agent_get_troop_id, ":killed_troop", ":killed_agent_no"),
      (try_begin), (eq, ":killed_troop", "trp_ni_nord_peasant"), (assign, ":gold", 3),
      (else_try), (eq, ":killed_troop", "trp_ni_nord_footman"), (assign, ":gold", 6),
      (else_try), (eq, ":killed_troop", "trp_ni_nord_archer"), (assign, ":gold", 7),
      (else_try), (eq, ":killed_troop", "trp_ni_nord_veteran"), (assign, ":gold", 10),
      (else_try), (eq, ":killed_troop", "trp_ni_nord_huscarl"), (assign, ":gold", 15),
      (else_try), (eq, ":killed_troop", "trp_ni_nord_berserker"), (assign, ":gold", 20),
      (else_try), (eq, ":killed_troop", "trp_ni_nord_jarl_guard"), (assign, ":gold", 35),
      (else_try), (assign, ":gold", 100),
      (try_end),

      # Mechanic 10: Greedy mutator x2 gold
      (try_begin), (eq, "$g_ni_mutator", 3), (val_mul, ":gold", 2), (try_end),
      # Mechanic 1: Gold hunter perk
      (player_get_slot, ":perk", ":killer_player_no", 112), (try_begin), (eq, ":perk", 22), (val_mul, ":gold", 12), (val_div, ":gold", 10), (try_end),

      (player_get_slot, ":cur_gold", ":killer_player_no", 100), (val_add, ":cur_gold", ":gold"), (player_set_slot, ":killer_player_no", 100, ":cur_gold"),
      (player_get_slot, ":kills", ":killer_player_no", 101), (val_add, ":kills", 1), (player_set_slot, ":killer_player_no", 101, ":kills"),

      # Mechanic 9: Scavenging chance
      (store_random_in_range, ":scav_rand", 0, 100), (try_begin), (lt, ":scav_rand", 20),
        (player_get_slot, ":metal", ":killer_player_no", 108), (val_add, ":metal", 1), (player_set_slot, ":killer_player_no", 108, ":metal"),
        (multiplayer_send_string_to_player, ":killer_player_no", 0, "@+1 Scrap Metal!"),
      (try_end),

      # Backend call if WSE
      # (wse_http_post, reg2, "@http://localhost:8000/api/kill", "@player_id={reg1}&gold={reg2}"),
    (try_end),

    # Mechanic 8: Boss loot
    (try_begin),
      (is_between, ":killed_troop", "trp_ni_nord_chieftain", "trp_ni_nord_berserker_chief"),
      (agent_get_position, 1, ":killed_agent_no"), (set_spawn_position, 1), (spawn_scene_prop, "spr_ni_loot_bag_gold"),
    (try_end),

    (try_begin), (le, "$g_ni_bots_alive", 0), (call_script, "script_nord_invasion_wave_completed"), (try_end),
  ]),

  ("nord_invasion_wave_completed", [
    (assign, "$g_ni_wave_state", 4), (display_message, "@Wave completed!"),
    (try_for_range, ":player_no", 0, 200), (player_is_active, ":player_no"), (player_get_slot, ":is_dead", ":player_no", 103), (eq, ":is_dead", 0),
      (player_get_slot, ":gold", ":player_no", 100), (val_add, ":gold", 20), (player_set_slot, ":player_no", 100, ":gold"),
    (try_end),

    # Mechanic 1: Perk choice every 3 waves
    (store_mod, ":perk_mod", "$g_ni_wave_number", 3), (try_begin), (eq, ":perk_mod", 0),
      (try_for_range, ":player_no", 0, 200), (player_is_active, ":player_no"), (multiplayer_send_int_to_player, ":player_no", 20, 1), (try_end), # open perk choice
    (try_end),

    # Mechanic 5: Director relief on wave complete
    (val_sub, "$g_ni_director_stress", 5),

    (try_begin), (ge, "$g_ni_wave_number", 25), (display_message, "@VICTORY!"), (assign, "$g_ni_wave_state", 5),
      (store_mission_timer_a, ":time"), (store_add, "$g_ni_next_wave_time", ":time", 10),
    (else_try), (val_add, "$g_ni_wave_number", 1), (call_script, "script_nord_invasion_setup_wave", "$g_ni_wave_number"),
    (try_end),
  ]),

  ("nord_invasion_check_defeat", [
    (assign, ":alive_players", 0), (assign, ":total_players", 0),
    (try_for_range, ":player_no", 0, 200), (player_is_active, ":player_no"), (val_add, ":total_players", 1),
      (player_get_slot, ":is_dead", ":player_no", 103), (eq, ":is_dead", 0), (val_add, ":alive_players", 1),
    (try_end),
    (assign, "$g_ni_players_alive", ":alive_players"),
    # Director stress increase when players die
    (try_begin), (eq, ":alive_players", 0), (gt, ":total_players", 0), (neq, "$g_ni_is_respawn_wave", 1), (neq, "$g_ni_wave_state", 4),
      (display_message, "@All players dead! Defeat!"), (assign, "$g_ni_wave_state", 5),
    (else_try), (lt, ":alive_players", ":total_players"), (val_add, "$g_ni_director_stress", 2), (val_min, "$g_ni_director_stress", 100),
    (try_end),
  ]),

  # Mechanic 3: Medic heal
  ("ni_class_medic_heal", [
    (store_script_param, ":healer_agent", 1), (store_script_param, ":target_agent", 2),
    (agent_is_active, ":healer_agent"), (agent_is_active, ":target_agent"), (agent_is_alive, ":target_agent"),
    (agent_get_position, 1, ":healer_agent"), (agent_get_position, 2, ":target_agent"), (get_distance_between_positions, ":dist", 1, 2), (lt, ":dist", 200),
    (store_agent_hit_points, ":hp", ":target_agent", 0), (val_add, ":hp", 30), (val_min, ":hp", 100), (agent_set_hit_points, ":target_agent", ":hp", 0),
    (agent_get_player_id, ":healer_player", ":healer_agent"), (player_get_slot, ":gold", ":healer_player", 100), (val_add, ":gold", 5), (player_set_slot, ":healer_player", 100, ":gold"),
  ]),

  # Mechanic 3: Engineer repair
  ("ni_class_engineer_repair", [
    (store_script_param, ":engineer_agent", 1), (store_script_param, ":prop_instance", 2),
    (scene_prop_get_slot, ":health", ":prop_instance", 300), (scene_prop_get_slot, ":max_health", ":prop_instance", 301),
    (lt, ":health", ":max_health"), (val_add, ":health", 20), (val_min, ":health", ":max_health"),
    (scene_prop_set_slot, ":prop_instance", 300, ":health"), (scene_prop_set_hit_points, ":prop_instance", ":health"),
  ]),

  # Mechanic 3: Banner buff
  ("ni_banner_buff_tick", [
    (try_for_agents, ":agent"), (agent_is_alive, ":agent"), (agent_get_slot, ":is_bot", ":agent", 200), (eq, ":is_bot", 0),
      (agent_get_position, 1, ":agent"),
      # Find nearby banner
      (assign, ":has_banner", 0),
      (try_for_prop_instances, ":prop"), (prop_instance_is_valid, ":prop"), (scene_prop_get_slot, ":type", ":prop", 304), (eq, ":type", 8),
        (prop_instance_get_position, 2, ":prop"), (get_distance_between_positions, ":dist", 1, 2), (lt, ":dist", 1500), (assign, ":has_banner", 1),
      (try_end),
      (try_begin), (eq, ":has_banner", 1), (agent_set_damage_modifier, ":agent", 110), (else_try), (agent_set_damage_modifier, ":agent", 100), (try_end),
    (try_end),
  ]),

  # Mechanic 6: Weather
  ("ni_set_weather", [
    (store_script_param, ":weather", 1),
    (try_begin), (eq, ":weather", 1), (set_fog_distance, 30, 0x888888), (display_message, "@Fog rolls in! Archers blinded!"),
    (else_try), (eq, ":weather", 2), (set_fog_distance, 100, 0x555555), (display_message, "@Rain! Fire arrows useless!"),
    (else_try), (eq, ":weather", 3), (set_fog_distance, 50, 0xFFFFFF), (display_message, "@Snowstorm! Movement slowed!"),
      (try_for_agents, ":agent"), (agent_is_alive, ":agent"), (agent_set_speed_modifier, ":agent", 90), (try_end),
    (else_try), (eq, ":weather", 4), (set_fog_distance, 10, 0x000000), (display_message, "@Night! Light torches!"),
    (else_try), (set_fog_distance, 100, 0xFFFFFF),
    (try_end),
  ]),

  # Mechanic 1: Apply perk
  ("ni_apply_perk", [
    (store_script_param, ":player_no", 1), (store_script_param, ":perk_id", 2),
    # Find free slot
    (try_for_range, ":slot", 112, 120), (player_get_slot, ":cur", ":player_no", ":slot"), (eq, ":cur", 0), (player_set_slot, ":player_no", ":slot", ":perk_id"), (try_end),
    # Apply immediate
    (player_get_agent_id, ":agent", ":player_no"), (try_begin), (agent_is_active, ":agent"),
      (try_begin), (eq, ":perk_id", 0), (agent_get_slot, ":mod", ":agent", 209), (val_add, ":mod", 15), (agent_set_slot, ":agent", 209, ":mod"), (agent_set_max_hit_points, ":agent", 115),
      (else_try), (eq, ":perk_id", 10), (agent_set_slot, ":agent", 208, 10),
      (try_end),
    (try_end),
  ]),

  # Mechanic 13: Wound system
  ("ni_wound_system_on_hit", [
    (store_script_param, ":hit_agent", 1), (store_script_param, ":damage", 2),
    (agent_get_slot, ":wounds", ":hit_agent", 205), (agent_get_slot, ":stamina", ":hit_agent", 206),
    (val_sub, ":stamina", 5), (val_max, ":stamina", 0), (agent_set_slot, ":hit_agent", 206, ":stamina"),
    (try_begin), (lt, ":stamina", 20), (agent_set_damage_modifier, ":hit_agent", 50), (else_try), (agent_set_damage_modifier, ":hit_agent", 100), (try_end),
  ]),

  # Mechanic 2: Place foundation
  ("ni_place_foundation", [
    (store_script_param, ":player_no", 1),
    (player_get_agent_id, ":agent_id", ":player_no"), (agent_get_position, 1, ":agent_id"), (position_move_forward, 1, 150), (set_spawn_position, 1),
    (player_get_slot, ":wood", ":player_no", 107), (ge, ":wood", 5), (val_sub, ":wood", 5), (player_set_slot, ":player_no", 107, ":wood"),
    (spawn_scene_prop, "spr_ni_foundation_wood"),
  ]),

  # Mechanic 5: Director
  ("ni_director_tick", [
    (try_begin), (gt, "$g_ni_director_stress", 80), (display_message, "@Director: Pressure increased!"), (val_add, "$g_ni_bots_total", 2),
    (else_try), (lt, "$g_ni_director_stress", 20), (display_message, "@Director: Relief - ammo box spawned!"), (set_spawn_position, 0), (spawn_scene_prop, "spr_ni_armory_chest"),
    (try_end),
  ]),

  # Mechanic 15: Campaign vote
  ("ni_campaign_vote", [
    (store_script_param, ":player_no", 1), (store_script_param, ":village_id", 2),
    # Store vote in global
    (try_begin), (eq, ":village_id", 0), (val_add, "$g_ni_campaign_votes_village_1", 1), (try_end),
    # If majority, set next map
  ]),

  # Backend login
  ("ni_backend_login", [
    (store_script_param, ":player_no", 1),
    # WSE: player_get_unique_id, http post
    # (player_get_unique_id, ":steam_id", ":player_no"),
    # (str_store_string, s1, "@http://localhost:8000/api/player/login"),
    # (wse_http_post, reg0, s1, "@player_id={reg1}"),
  ]),
]
