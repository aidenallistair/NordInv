# Better Edition presentations - all 15 mechanics UI

presentations = [
  ("ni_shop", 0, "mesh_load_window", [
    (ti_on_presentation_load, [
      (set_fixed_point_multiplier, 1000), (presentation_set_duration, 999999),
      (create_mesh_overlay, reg1, "mesh_mp_ingame_menu"), (position_set_x, pos1, 250), (position_set_y, pos1, 100), (overlay_set_position, reg1, pos1), (position_set_x, pos1, 1000), (position_set_y, pos1, 1000), (overlay_set_size, reg1, pos1),
      (create_text_overlay, reg1, "@Nord Invasion Shop - Gold: {reg1} | Wood: {reg2} | Metal: {reg3}", 1), (assign, reg1, 500), (position_set_x, pos1, 500), (position_set_y, pos1, 650), (overlay_set_position, reg1, pos1),
      (create_button_overlay, "$g_presentation_obj_1", "@Buy Sword (50g)", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 550), (overlay_set_position, "$g_presentation_obj_1", pos1),
      (create_button_overlay, "$g_presentation_obj_2", "@Buy Bow (80g)", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 500), (overlay_set_position, "$g_presentation_obj_2", pos1),
      (create_button_overlay, "$g_presentation_obj_3", "@Buy Armor (100g)", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 450), (overlay_set_position, "$g_presentation_obj_3", pos1),
      (create_button_overlay, "$g_presentation_obj_4", "@Buy Barricade Kit (150g + 5 wood)", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 400), (overlay_set_position, "$g_presentation_obj_4", pos1),
      (create_button_overlay, "$g_presentation_obj_6", "@Buy Stakes vs Cav (100g)", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 350), (overlay_set_position, "$g_presentation_obj_6", pos1),
      (create_button_overlay, "$g_presentation_obj_7", "@Buy Oil Cauldron (200g)", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 300), (overlay_set_position, "$g_presentation_obj_7", pos1),
      (create_button_overlay, "$g_presentation_obj_5", "@Close (ESC)", 1), (position_set_x, pos1, 500), (position_set_y, pos1, 150), (overlay_set_position, "$g_presentation_obj_5", pos1),
    ]),
    (ti_on_presentation_event_state_change, [
      (store_trigger_param_1, ":object"), (try_begin), (eq, ":object", "$g_presentation_obj_5"), (presentation_set_duration, 0), (try_end),
    ]),
  ]),

  # Mechanic 1: Perk choice
  ("ni_perk_choice", 0, "mesh_load_window", [
    (ti_on_presentation_load, [
      (set_fixed_point_multiplier, 1000), (presentation_set_duration, 999999),
      (create_text_overlay, reg1, "@Choose a Perk! Wave {reg1}", 1), (assign, reg1, "$g_ni_wave_number"), (position_set_x, pos1, 500), (position_set_y, pos1, 650), (overlay_set_position, reg1, pos1),
      (create_button_overlay, "$g_presentation_obj_1", "@Iron Skin +15% HP", 1), (position_set_x, pos1, 200), (position_set_y, pos1, 400), (overlay_set_position, "$g_presentation_obj_1", pos1),
      (create_button_overlay, "$g_presentation_obj_2", "@Bloodlust - Damage when wounded", 1), (position_set_x, pos1, 500), (position_set_y, pos1, 400), (overlay_set_position, "$g_presentation_obj_2", pos1),
      (create_button_overlay, "$g_presentation_obj_3", "@Engineer - Barricades +30% HP", 1), (position_set_x, pos1, 800), (position_set_y, pos1, 400), (overlay_set_position, "$g_presentation_obj_3", pos1),
      (create_text_overlay, reg1, "@You have 15 sec to choose!", 1), (position_set_x, pos1, 500), (position_set_y, pos1, 200), (overlay_set_position, reg1, pos1),
    ]),
    (ti_on_presentation_event_state_change, [
      (store_trigger_param_1, ":object"),
      (try_begin), (eq, ":object", "$g_presentation_obj_1"), (call_script, "script_ni_apply_perk", 0, 0), (presentation_set_duration, 0),
      (else_try), (eq, ":object", "$g_presentation_obj_2"), (call_script, "script_ni_apply_perk", 0, 10), (presentation_set_duration, 0),
      (else_try), (eq, ":object", "$g_presentation_obj_3"), (call_script, "script_ni_apply_perk", 0, 20), (presentation_set_duration, 0),
      (try_end),
    ]),
    (ti_on_presentation_run, [(store_trigger_param_1, ":cur_time"), (try_begin), (gt, ":cur_time", 15000), (presentation_set_duration, 0), (try_end)]),
  ]),

  # HUD with wave, mutator, weather, director
  ("ni_wave_info", 0x00000001|0x00000002, 0, [
    (ti_on_presentation_load, [
      (set_fixed_point_multiplier, 1000), (presentation_set_duration, 999999),
      (create_text_overlay, "$g_ni_hud_wave", "@Wave: {reg1} | Nords: {reg2} | Alive: {reg3}", 1|2), (position_set_x, pos1, 500), (position_set_y, pos1, 700), (overlay_set_position, "$g_ni_hud_wave", pos1), (position_set_x, pos1, 800), (position_set_y, pos1, 800), (overlay_set_size, "$g_ni_hud_wave", pos1),
      (create_text_overlay, "$g_ni_hud_mutator", "@Mutator: {s1} | Weather: {s2} | Stress: {reg4}", 1|2), (position_set_x, pos1, 500), (position_set_y, pos1, 670), (overlay_set_position, "$g_ni_hud_mutator", pos1),
      (create_text_overlay, "$g_ni_hud_objective", "@Objective: {s3}", 1|2), (position_set_x, pos1, 500), (position_set_y, pos1, 640), (overlay_set_position, "$g_ni_hud_objective", pos1),
    ]),
    (ti_on_presentation_run, [
      (store_trigger_param_1, ":cur_time"), (try_begin), (gt, ":cur_time", 500),
        (assign, reg1, "$g_ni_wave_number"), (assign, reg2, "$g_ni_bots_alive"), (assign, reg3, "$g_ni_players_alive"), (assign, reg4, "$g_ni_director_stress"),
        (overlay_set_text, "$g_ni_hud_wave", "@Wave: {reg1} | Nords: {reg2} | Alive: {reg3} | Stress: {reg4}"),
        # Mutator name
        (try_begin), (eq, "$g_ni_mutator", 0), (str_store_string, s1, "@None"), (else_try), (eq, "$g_ni_mutator", 1), (str_store_string, s1, "@Thor's Fury - Berserk"), (else_try), (str_store_string, s1, "@Cursed"), (try_end),
        (try_begin), (eq, "$g_ni_weather", 0), (str_store_string, s2, "@Clear"), (else_try), (eq, "$g_ni_weather", 1), (str_store_string, s2, "@Fog"), (else_try), (str_store_string, s2, "@Rain/Snow"), (try_end),
        (try_begin), (eq, "$g_ni_wave_objective", 0), (str_store_string, s3, "@Kill all"), (else_try), (eq, "$g_ni_wave_objective", 1), (str_store_string, s3, "@Destroy Ram"), (else_try), (str_store_string, s3, "@Special"), (try_end),
        (overlay_set_text, "$g_ni_hud_mutator", "@Mutator: {s1} | Weather: {s2} | Stress: {reg4}"),
        (overlay_set_text, "$g_ni_hud_objective", "@Objective: {s3}"),
      (try_end),
    ]),
  ]),

  # Build menu Mechanic 2
  ("ni_build_menu", 0, "mesh_load_window", [
    (ti_on_presentation_load, [
      (set_fixed_point_multiplier, 1000), (presentation_set_duration, 999999),
      (create_text_overlay, reg1, "@Fortress Builder - Wood: {reg1}", 1), (position_set_x, pos1, 500), (position_set_y, pos1, 650), (overlay_set_position, reg1, pos1),
      (create_button_overlay, "$g_presentation_obj_1", "@Foundation (5 wood)", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 500), (overlay_set_position, "$g_presentation_obj_1", pos1),
      (create_button_overlay, "$g_presentation_obj_2", "@Wall (Foundation + 3 wood)", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 450), (overlay_set_position, "$g_presentation_obj_2", pos1),
      (create_button_overlay, "$g_presentation_obj_3", "@Door (5 wood + 2 metal)", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 400), (overlay_set_position, "$g_presentation_obj_3", pos1),
      (create_button_overlay, "$g_presentation_obj_4", "@Stakes vs Cav (4 wood)", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 350), (overlay_set_position, "$g_presentation_obj_4", pos1),
      (create_button_overlay, "$g_presentation_obj_5", "@Oil Cauldron (10 wood + 5 metal)", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 300), (overlay_set_position, "$g_presentation_obj_5", pos1),
      (create_button_overlay, "$g_presentation_obj_6", "@Close", 1), (position_set_x, pos1, 500), (position_set_y, pos1, 150), (overlay_set_position, "$g_presentation_obj_6", pos1),
    ]),
    (ti_on_presentation_event_state_change, [
      (store_trigger_param_1, ":object"), (try_begin), (eq, ":object", "$g_presentation_obj_6"), (presentation_set_duration, 0), (try_end),
    ]),
  ]),

  # Class selection Mechanic 3
  ("ni_class_select", 0, "mesh_load_window", [
    (ti_on_presentation_load, [
      (set_fixed_point_multiplier, 1000), (presentation_set_duration, 999999),
      (create_text_overlay, reg1, "@Choose Class", 1), (position_set_x, pos1, 500), (position_set_y, pos1, 650), (overlay_set_position, reg1, pos1),
      (create_button_overlay, "$g_presentation_obj_1", "@Infantry - Tank", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 500), (overlay_set_position, "$g_presentation_obj_1", pos1),
      (create_button_overlay, "$g_presentation_obj_2", "@Archer - DPS", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 450), (overlay_set_position, "$g_presentation_obj_2", pos1),
      (create_button_overlay, "$g_presentation_obj_3", "@Medic - Heal/Revive", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 400), (overlay_set_position, "$g_presentation_obj_3", pos1),
      (create_button_overlay, "$g_presentation_obj_4", "@Engineer - Build/Repair", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 350), (overlay_set_position, "$g_presentation_obj_4", pos1),
      (create_button_overlay, "$g_presentation_obj_5", "@Banner - Buff team", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 300), (overlay_set_position, "$g_presentation_obj_5", pos1),
    ]),
  ]),

  # Campaign map Mechanic 15
  ("ni_campaign_map", 0, "mesh_load_window", [
    (ti_on_presentation_load, [
      (set_fixed_point_multiplier, 1000), (presentation_set_duration, 999999),
      (create_text_overlay, reg1, "@Swadia Campaign - Choose Village to Defend", 1), (position_set_x, pos1, 500), (position_set_y, pos1, 700), (overlay_set_position, reg1, pos1),
      (create_button_overlay, "$g_presentation_obj_1", "@Village 1 - Plain (Easy)", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 550), (overlay_set_position, "$g_presentation_obj_1", pos1),
      (create_button_overlay, "$g_presentation_obj_2", "@Village 2 - Forest (Medium)", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 500), (overlay_set_position, "$g_presentation_obj_2", pos1),
      (create_button_overlay, "$g_presentation_obj_3", "@Castle 1 - Hard", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 450), (overlay_set_position, "$g_presentation_obj_3", pos1),
      (create_button_overlay, "$g_presentation_obj_4", "@Bridge - Chokepoint", 1), (position_set_x, pos1, 300), (position_set_y, pos1, 400), (overlay_set_position, "$g_presentation_obj_4", pos1),
    ]),
  ]),
]
