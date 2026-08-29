# module_scene_props.py - shop chests and barricades

scene_props = [
  ("ni_armory_chest", 0, "armory_chest", "bo_armory_chest", [
    (ti_on_scene_prop_init,
     [
       (store_trigger_param_1, ":instance_no"),
       # Mark as shop
       (scene_prop_set_slot, ":instance_no", 300, 1),
     ]),
    (ti_on_scene_prop_use,
     [
       (store_trigger_param_1, ":agent_id"),
       (store_trigger_param_2, ":instance_id"),
       (agent_get_player_id, ":player_no", ":agent_id"),
       (try_begin),
         (player_is_active, ":player_no"),
         # Trigger shop presentation via script
         (multiplayer_send_int_to_player, ":player_no", 10, 1), # open shop
       (try_end),
     ]),
  ]),

  ("ni_barricade_wood", sokf_moveable|sokf_show_hit_point_bar|sokf_destructible, "barricade_wood", "bo_barricade_wood", [
    (ti_on_scene_prop_init,
     [
       (store_trigger_param_1, ":instance_no"),
       (scene_prop_set_slot, ":instance_no", slot_scene_prop_ni_is_barricade, 1),
       (scene_prop_set_slot, ":instance_no", slot_scene_prop_ni_max_health, 800),
       (scene_prop_set_slot, ":instance_no", slot_scene_prop_ni_health, 800),
       (scene_prop_set_hit_points, ":instance_no", 800),
     ]),
    (ti_on_scene_prop_hit,
     [
       (store_trigger_param_1, ":instance_no"),
       (store_trigger_param_2, ":damage"),
       (scene_prop_get_slot, ":health", ":instance_no", slot_scene_prop_ni_health),
       (val_sub, ":health", ":damage"),
       (scene_prop_set_slot, ":instance_no", slot_scene_prop_ni_health, ":health"),
       (try_begin),
         (le, ":health", 0),
         # Destroy
         (scene_prop_set_hit_points, ":instance_no", 0),
       (try_end),
     ]),
  ]),

  ("ni_barricade_shield_wall", sokf_moveable|sokf_show_hit_point_bar|sokf_destructible, "shield_wall", "bo_shield_wall", [
    (ti_on_scene_prop_init,
     [
       (store_trigger_param_1, ":instance_no"),
       (scene_prop_set_slot, ":instance_no", slot_scene_prop_ni_is_barricade, 1),
       (scene_prop_set_slot, ":instance_no", slot_scene_prop_ni_max_health, 1500),
       (scene_prop_set_slot, ":instance_no", slot_scene_prop_ni_health, 1500),
       (scene_prop_set_hit_points, ":instance_no", 1500),
     ]),
  ]),

  ("ni_shop_sign", 0, "shop_sign", "bo_shop_sign", []),
]
