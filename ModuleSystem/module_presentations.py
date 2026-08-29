# module_presentations.py - Shop and HUD

presentations = [
  ("ni_shop", 0, mesh_load_window, [
    (ti_on_presentation_load,
     [
       (set_fixed_point_multiplier, 1000),
       (presentation_set_duration, 999999),
       
       # Background
       (create_mesh_overlay, reg1, "mesh_mp_ingame_menu"),
       (position_set_x, pos1, 250),
       (position_set_y, pos1, 100),
       (overlay_set_position, reg1, pos1),
       (position_set_x, pos1, 1000),
       (position_set_y, pos1, 1000),
       (overlay_set_size, reg1, pos1),
       
       # Title
       (create_text_overlay, reg1, "@Nord Invasion Shop - Gold: {reg1}", tf_center_justify),
       (store_add, ":gold", "$g_ni_player_gold", 0),
       (assign, reg1, ":gold"),
       (position_set_x, pos1, 500),
       (position_set_y, pos1, 600),
       (overlay_set_position, reg1, pos1),
       
       # Buttons - weapons
       (create_button_overlay, "$g_presentation_obj_1", "@Buy Sword (50g)", tf_center_justify),
       (position_set_x, pos1, 300),
       (position_set_y, pos1, 500),
       (overlay_set_position, "$g_presentation_obj_1", pos1),
       
       (create_button_overlay, "$g_presentation_obj_2", "@Buy Bow + Arrows (80g)", tf_center_justify),
       (position_set_x, pos1, 300),
       (position_set_y, pos1, 450),
       (overlay_set_position, "$g_presentation_obj_2", pos1),
       
       (create_button_overlay, "$g_presentation_obj_3", "@Buy Armor (100g)", tf_center_justify),
       (position_set_x, pos1, 300),
       (position_set_y, pos1, 400),
       (overlay_set_position, "$g_presentation_obj_3", pos1),
       
       (create_button_overlay, "$g_presentation_obj_4", "@Buy Barricade (150g)", tf_center_justify),
       (position_set_x, pos1, 300),
       (position_set_y, pos1, 350),
       (overlay_set_position, "$g_presentation_obj_4", pos1),
       
       (create_button_overlay, "$g_presentation_obj_5", "@Close Shop (ESC)", tf_center_justify),
       (position_set_x, pos1, 500),
       (position_set_y, pos1, 150),
       (overlay_set_position, "$g_presentation_obj_5", pos1),
     ]),
    
    (ti_on_presentation_event_state_change,
     [
       (store_trigger_param_1, ":object"),
       (store_trigger_param_2, ":value"),
       
       (try_begin),
         (eq, ":object", "$g_presentation_obj_1"),
         # Buy sword - call script via multiplayer_send?
         (multiplayer_send_int_to_player, 0, 2, "itm_sword_medieval_a"), # simplified
         (presentation_set_duration, 0),
       (else_try),
         (eq, ":object", "$g_presentation_obj_5"),
         (presentation_set_duration, 0),
       (try_end),
     ]),
  ]),

  ("ni_wave_info", prsntf_read_only|prsntf_manual_end_only, 0, [
    (ti_on_presentation_load,
     [
       (set_fixed_point_multiplier, 1000),
       (presentation_set_duration, 999999),
       
       # Top center HUD - wave info
       (create_text_overlay, "$g_ni_hud_wave", "@Wave: {reg1} | Nords left: {reg2} | Alive: {reg3}", tf_center_justify|tf_with_outline),
       (position_set_x, pos1, 500),
       (position_set_y, pos1, 700),
       (overlay_set_position, "$g_ni_hud_wave", pos1),
       (position_set_x, pos1, 800),
       (position_set_y, pos1, 800),
       (overlay_set_size, "$g_ni_hud_wave", pos1),
       (overlay_set_color, "$g_ni_hud_wave", 0xFFFFFF),
     ]),
    
    (ti_on_presentation_run,
     [
       (store_trigger_param_1, ":cur_time"),
       # Update every 0.5 sec
       (try_begin),
         (gt, ":cur_time", 500),
         (assign, reg1, "$g_ni_wave_number"),
         (assign, reg2, "$g_ni_bots_alive"),
         (assign, reg3, "$g_ni_players_alive"),
         (overlay_set_text, "$g_ni_hud_wave", "@Wave: {reg1} | Nords left: {reg2} | Alive: {reg3}"),
       (try_end),
     ]),
  ]),
]
