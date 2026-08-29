# module_troops.py addon for Nord Invasion
# Add these troops to your module_troops.py

from module_constants import *

# Example troop definitions - syntax for Module System 1.174
# Format: [id, name, plural, flags, menu, faction, inventory, attributes, wp, skills, face1, face2]

ni_troops = [
  # Tier 0 - мясо, волна 1-3
  ["ni_nord_peasant", "Nord Peasant", "Nord Peasants", 
   tf_guarantee_boots|tf_guarantee_armor, 0, 0, 
   fac_nords, 
   [itm_hatchet, itm_nordic_village_boots, itm_shirt, itm_hide_covered],
   def_attrib|level(4), wp(40), knows_common|knows_ironflesh_1, 0, 0],

  ["ni_nord_footman", "Nord Footman", "Nord Footmen",
   tf_guarantee_boots|tf_guarantee_armor|tf_guarantee_helmet|tf_guarantee_shield, 0, 0,
   fac_nords,
   [itm_nordic_sword, itm_nordic_footman_helmet, itm_byrnie, itm_nordic_footman_boots, itm_tab_shield_round_a],
   def_attrib|level(12), wp(90), knows_common|knows_shield_2|knows_power_strike_2, 0, 0],

  ["ni_nord_archer", "Nord Archer", "Nord Archers",
   tf_guarantee_boots|tf_guarantee_armor|tf_guarantee_ranged, 0, 0,
   fac_nords,
   [itm_short_bow, itm_barbed_arrows, itm_nordic_archer_helmet, itm_leather_jacket, itm_hunter_boots, itm_dagger],
   def_attrib|level(10), wp(80), knows_common|knows_power_draw_3, 0, 0],

  # Tier 1 - волна 4-7
  ["ni_nord_veteran", "Nord Veteran", "Nord Veterans",
   tf_guarantee_all, 0, 0,
   fac_nords,
   [itm_nordic_warrior_helmet, itm_mail_with_surcoat, itm_mail_chausses, itm_nordic_shield, itm_one_handed_battle_axe_a],
   def_attrib|level(18), wp(120), knows_common|knows_shield_3|knows_ironflesh_2|knows_power_strike_3, 0, 0],

  ["ni_nord_huscarl", "Nord Huscarl", "Nord Huscarls",
   tf_guarantee_all, 0, 0,
   fac_nords,
   [itm_nordic_huscarl_helmet, itm_nordic_huscarl_armor, itm_iron_greaves, itm_nordic_huscarl_shield, itm_nordic_war_sword],
   def_attrib|level(25), wp(150), knows_common|knows_shield_4|knows_ironflesh_3|knows_power_strike_4, 0, 0],

  ["ni_nord_berserker", "Nord Berserker", "Nord Berserkers",
   tf_guarantee_all|tf_guarantee_no_parry, 0, 0,
   fac_nords,
   [itm_berserker_helmet, itm_berserker_armor, itm_berserker_boots, itm_two_handed_battle_axe],
   def_attrib|level(28), wp(170), knows_common|knows_ironflesh_4|knows_power_strike_5, 0, 0],

  # Tier 2 - элита, волна 8+
  ["ni_nord_jarl_guard", "Jarl's Guard", "Jarl's Guards",
   tf_guarantee_all, 0, 0,
   fac_nords,
   [itm_nordic_warlord_helmet, itm_plate_armor, itm_plate_boots, itm_nordic_champion_shield, itm_sword_of_war],
   def_attrib|level(35), wp(200), knows_common|knows_shield_5|knows_ironflesh_4|knows_power_strike_5|knows_athletics_5, 0, 0],

  # Bosses - каждые 5 волн
  ["ni_nord_chieftain", "Nord Chieftain", "Nord Chieftains",
   tf_hero|tf_guarantee_all|tf_unmoveable_in_party_window, 0, 0,
   fac_nords,
   [itm_nordic_warlord_helmet, itm_black_armor, itm_plate_boots, itm_tab_shield_round_e, itm_nordic_warlord_sword],
   def_attrib|level(50), wp(250), knows_common|knows_shield_5|knows_ironflesh_5|knows_power_strike_6, 0, 0],

  ["ni_nord_berserker_chief", "Berserker Chief", "Berserker Chiefs",
   tf_hero|tf_guarantee_all, 0, 0,
   fac_nords,
   [itm_bear_helmet, itm_berserker_armor_heavy, itm_iron_greaves, itm_great_axe],
   def_attrib|level(55), wp(270), knows_common|knows_ironflesh_6|knows_power_strike_7, 0, 0],
]

# Player defenders - can use native swadian troops or custom
# For Fianna style - just use swadian recruit -> knight progression via shop
