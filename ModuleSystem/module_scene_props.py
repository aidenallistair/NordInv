# Better Edition scene props - all 15 mechanics

scene_props = [
  # Core - shop chest
  ("ni_armory_chest", 0, "armory_chest", "bo_armory_chest", [
    (ti_on_scene_prop_init, [(store_trigger_param_1, ":instance_no"), (scene_prop_set_slot, ":instance_no", 302, 1)]),
    (ti_on_scene_prop_use, [
      (store_trigger_param_1, ":agent_id"), (store_trigger_param_2, ":instance_id"),
      (agent_get_player_id, ":player_no", ":agent_id"),
      (try_begin), (player_is_active, ":player_no"), (multiplayer_send_int_to_player, ":player_no", 10, 1), (try_end),
    ]),
  ]),

  # Mechanic 2: Fortress system
  ("ni_foundation_wood", 0, "foundation_wood", "bo_foundation_wood", [
    (ti_on_scene_prop_init, [
      (store_trigger_param_1, ":instance_no"),
      (scene_prop_set_slot, ":instance_no", 300, 500), (scene_prop_set_slot, ":instance_no", 301, 500),
      (scene_prop_set_slot, ":instance_no", 304, 0), (scene_prop_set_hit_points, ":instance_no", 500),
    ]),
  ]),
  ("ni_wall_wood", 0x00000001|0x00000002|0x00000100, "wall_wood", "bo_wall_wood", [ # moveable|destructible|show_hp
    (ti_on_scene_prop_init, [
      (store_trigger_param_1, ":instance_no"),
      (scene_prop_set_slot, ":instance_no", 300, 800), (scene_prop_set_slot, ":instance_no", 301, 800),
      (scene_prop_set_slot, ":instance_no", 304, 1), (scene_prop_set_hit_points, ":instance_no", 800),
      (scene_prop_set_slot, ":instance_no", 302, 1),
    ]),
    (ti_on_scene_prop_hit, [
      (store_trigger_param_1, ":instance_no"), (store_trigger_param_2, ":damage"),
      (scene_prop_get_slot, ":health", ":instance_no", 300), (val_sub, ":health", ":damage"),
      (scene_prop_set_slot, ":instance_no", 300, ":health"),
      (try_begin), (le, ":health", 0), (scene_prop_set_hit_points, ":instance_no", 0), (try_end),
    ]),
  ]),
  ("ni_wall_door", 0x00000001|0x00000002|0x00000100, "wall_door", "bo_wall_door", [
    (ti_on_scene_prop_init, [
      (store_trigger_param_1, ":instance_no"),
      (scene_prop_set_slot, ":instance_no", 300, 1000), (scene_prop_set_slot, ":instance_no", 301, 1000),
      (scene_prop_set_slot, ":instance_no", 304, 2), (scene_prop_set_hit_points, ":instance_no", 1000),
    ]),
  ]),
  ("ni_stakes", 0x00000002|0x00000100, "stakes", "bo_stakes", [
    (ti_on_scene_prop_init, [
      (store_trigger_param_1, ":instance_no"),
      (scene_prop_set_slot, ":instance_no", 300, 400), (scene_prop_set_slot, ":instance_no", 311, 1),
      (scene_prop_set_hit_points, ":instance_no", 400),
    ]),
    # Mechanic 7: anti-cav - horse hits stakes
    (ti_on_scene_prop_hit, [
      (store_trigger_param_1, ":instance_no"), (store_trigger_param_2, ":damage"),
      (store_trigger_param_3, ":agent_id"),
      (try_begin), (agent_is_active, ":agent_id"), (agent_get_horse, ":horse", ":agent_id"),
      (gt, ":horse", 0), (agent_set_hit_points, ":horse", 0, 0), (agent_set_hit_points, ":agent_id", 0, 0),
      (display_message, "@Horse impaled on stakes!"), (try_end),
    ]),
  ]),
  ("ni_oil_cauldron", 0, "oil_cauldron", "bo_oil_cauldron", [
    (ti_on_scene_prop_init, [
      (store_trigger_param_1, ":instance_no"),
      (scene_prop_set_slot, ":instance_no", 312, 1), (scene_prop_set_slot, ":instance_no", 300, 600),
    ]),
    (ti_on_scene_prop_use, [
      (store_trigger_param_1, ":agent_id"), (store_trigger_param_2, ":instance_id"),
      # Pour oil - damage in area
      (agent_get_position, 1, ":agent_id"), (position_move_forward, 1, 200),
      (particle_system_burst, "psys_oil_fire", 1, 100),
      # Damage all nords in radius 300
      (try_for_agents, ":other_agent"), (agent_is_alive, ":other_agent"),
      (agent_get_slot, ":is_bot", ":other_agent", 200), (eq, ":is_bot", 1),
      (agent_get_position, 2, ":other_agent"), (get_distance_between_positions, ":dist", 1, 2),
      (lt, ":dist", 300), (agent_set_hit_points, ":other_agent", 0, 0), (try_end),
    ]),
  ]),
  ("ni_brazier", 0, "brazier", "bo_brazier", [
    (ti_on_scene_prop_init, [(store_trigger_param_1, ":instance_no"), (particle_system_add_new, "psys_torch_fire"),]),
  ]),
  ("ni_spike_trap", 0, "spike_trap", "bo_spike_trap", [
    (ti_on_scene_prop_init, [(store_trigger_param_1, ":instance_no"), (scene_prop_set_slot, ":instance_no", 300, 200)]),
    (ti_on_scene_prop_hit, [
      (store_trigger_param_1, ":instance_no"), (store_trigger_param_2, ":damage"), (store_trigger_param_3, ":agent_id"),
      (agent_set_hit_points, ":agent_id", 0, 0),
    ]),
  ]),

  # Mechanic 8: Loot
  ("ni_loot_bag_gold", 0, "sack", "bo_sack", [
    (ti_on_scene_prop_init, [
      (store_trigger_param_1, ":instance_no"),
      (scene_prop_set_slot, ":instance_no", 308, 1), (scene_prop_set_slot, ":instance_no", 307, 500),
    ]),
    (ti_on_scene_prop_use, [
      (store_trigger_param_1, ":agent_id"), (store_trigger_param_2, ":instance_no"),
      (agent_get_player_id, ":player_no", ":agent_id"),
      (try_begin), (player_is_active, ":player_no"),
      (player_get_slot, ":is_carrying", ":player_no", 122), (eq, ":is_carrying", 0),
      (player_set_slot, ":player_no", 122, 1), (agent_set_slot, ":agent_id", 210, 1),
      (scene_prop_set_visibility, ":instance_no", 0),
      (display_message, "@You picked up gold bag! Carry to treasury!"),
      (agent_set_speed_modifier, ":agent_id", 70), # slower
      (try_end),
    ]),
  ]),
  ("ni_treasury_chest", 0, "treasury_chest", "bo_treasury_chest", [
    (ti_on_scene_prop_init, [(store_trigger_param_1, ":instance_no"), (scene_prop_set_slot, ":instance_no", 309, 1)]),
    (ti_on_scene_prop_use, [
      (store_trigger_param_1, ":agent_id"), (store_trigger_param_2, ":instance_no"),
      (agent_get_player_id, ":player_no", ":agent_id"),
      (player_get_slot, ":is_carrying", ":player_no", 122), (eq, ":is_carrying", 1),
      (player_set_slot, ":player_no", 122, 0), (agent_set_slot, ":agent_id", 210, 0),
      (player_get_slot, ":gold", ":player_no", 100), (val_add, ":gold", 500),
      (player_set_slot, ":player_no", 100, ":gold"),
      (agent_set_speed_modifier, ":agent_id", 100),
      (display_message, "@Gold delivered! +500!"),
    ]),
  ]),

  # Mechanic 4: Objectives
  ("ni_ram", 0x00000002|0x00000100, "battering_ram", "bo_battering_ram", [
    (ti_on_scene_prop_init, [
      (store_trigger_param_1, ":instance_no"),
      (scene_prop_set_slot, ":instance_no", 310, 1), (scene_prop_set_slot, ":instance_no", 300, 2000),
      (scene_prop_set_hit_points, ":instance_no", 2000),
    ]),
  ]),
  ("ni_camp_nord", 0x00000002, "camp_nord", "bo_camp_nord", [
    (ti_on_scene_prop_init, [(store_trigger_param_1, ":instance_no"), (scene_prop_set_slot, ":instance_no", 300, 300)]),
    (ti_on_scene_prop_hit, [
      (store_trigger_param_1, ":instance_no"), (store_trigger_param_2, ":damage"), (store_trigger_param_3, ":agent_id"),
      (scene_prop_get_slot, ":health", ":instance_no", 300), (val_sub, ":health", ":damage"),
      (scene_prop_set_slot, ":instance_no", 300, ":health"),
      (try_begin), (le, ":health", 0), (particle_system_burst, "psys_campfire", 1, 100), (scene_prop_set_hit_points, ":instance_no", 0), (try_end),
    ]),
  ]),

  # Mechanic 14: Fire & destructible
  ("ni_tree_oak", 0x00000002|0x00000100, "tree_oak", "bo_tree_oak", [
    (ti_on_scene_prop_init, [(store_trigger_param_1, ":instance_no"), (scene_prop_set_slot, ":instance_no", 300, 400), (scene_prop_set_hit_points, ":instance_no", 400)]),
    (ti_on_scene_prop_hit, [
      (store_trigger_param_1, ":instance_no"), (store_trigger_param_2, ":damage"), (store_trigger_param_3, ":agent_id"),
      (agent_get_wielded_item, ":item", ":agent_id", 0), (eq, ":item", "itm_ni_torch"),
      (scene_prop_set_slot, ":instance_no", 305, 1), (particle_system_add_new, "psys_torch_fire"),
    ]),
  ]),
  ("ni_powder_barrel", 0x00000002|0x00000100, "barrel", "bo_barrel", [
    (ti_on_scene_prop_init, [(store_trigger_param_1, ":instance_no"), (scene_prop_set_slot, ":instance_no", 300, 100)]),
    (ti_on_scene_prop_hit, [
      (store_trigger_param_1, ":instance_no"), (store_trigger_param_2, ":damage"),
      (prop_instance_get_position, 1, ":instance_no"),
      (particle_system_burst, "psys_explosion", 1, 200),
      (try_for_agents, ":agent"), (agent_is_alive, ":agent"), (agent_get_position, 2, ":agent"),
      (get_distance_between_positions, ":dist", 1, 2), (lt, ":dist", 500),
      (store_agent_hit_points, ":hp", ":agent", 0), (val_sub, ":hp", 80), (agent_set_hit_points, ":agent", ":hp", 0),
      (try_end),
      (scene_prop_set_hit_points, ":instance_no", 0),
    ]),
  ]),

  # Mechanic 6: Campfire for warmth and crafting
  ("ni_campfire", 0, "campfire", "bo_campfire", [
    (ti_on_scene_prop_init, [(store_trigger_param_1, ":instance_no"), (particle_system_add_new, "psys_campfire")]),
    (ti_on_scene_prop_use, [
      (store_trigger_param_1, ":agent_id"),
      (agent_get_player_id, ":player_no", ":agent_id"),
      # Craft arrows if has wood
      (player_get_slot, ":wood", ":player_no", 107), (ge, ":wood", 3),
      (val_sub, ":wood", 3), (player_set_slot, ":player_no", 107, ":wood"),
      (agent_equip_item, ":agent_id", "itm_barbed_arrows"),
      (display_message, "@Crafted arrows!"),
    ]),
  ]),
]
