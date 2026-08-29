# Better Edition - campaign menu

game_menus = [
  ("ni_campaign_map", 0, "Swadia Campaign Map - Choose where to fight",
   "none",
   [
     (set_background_mesh, "mesh_campaign_map"),
   ],
   [
     ("village_1", [], "Village of Jelbegi (Easy) - Current owner: {s1}",
      [
        (call_script, "script_ni_campaign_vote", 0, 0),
        (jump_to_scene, "scn_mp_ni_village_01"),
        (change_screen_mission),
      ]),
     ("village_2", [], "Forest Hamlet (Medium)",
      [
        (call_script, "script_ni_campaign_vote", 0, 1),
        (jump_to_scene, "scn_mp_ni_forest_01"),
        (change_screen_mission),
      ]),
     ("castle_1", [], "Castle Outpost (Hard)",
      [
        (call_script, "script_ni_campaign_vote", 0, 2),
        (jump_to_scene, "scn_mp_ni_castle_01"),
        (change_screen_mission),
      ]),
     ("bridge", [], "Bridge Fort (Chokepoint) - Best for barricades",
      [
        (call_script, "script_ni_campaign_vote", 0, 3),
        (jump_to_scene, "scn_mp_ni_bridge_01"),
        (change_screen_mission),
      ]),
     ("go_back", [], "Go back", [(change_screen_return)]),
   ]),
]
