# Better Edition items

ni_items = [
  # Mechanic 3: Role items
  ["ni_medical_kit", "Medical Kit", [("medical_kit",0)], itp_type_goods|itp_merchandise, 0, 150, weight(2)|abundance(100)|weapon_length(0), imodbits_none, []],
  ["ni_repair_hammer", "Repair Hammer", [("hammer",0)], itp_type_goods, 0, 100, weight(2)|abundance(100), imodbits_none, []],
  ["ni_banner_swadia", "Swadian Banner", [("banner_a",0)], itp_type_goods, 0, 200, weight(3), imodbits_none, []],

  # Mechanic 2: Fortress resources
  ["ni_wood_plank", "Wood Plank", [("plank",0)], itp_type_goods|itp_merchandise, 0, 20, weight(3), imodbits_none, []],
  ["ni_scrap_metal", "Scrap Metal", [("iron",0)], itp_type_goods, 0, 30, weight(2), imodbits_none, []],
  ["ni_cloth_scrap", "Cloth Scrap", [("wool_cloth",0)], itp_type_goods, 0, 10, weight(1), imodbits_none, []],

  # Mechanic 9: Crafted
  ["ni_torch", "Torch", [("torch",0)], itp_type_one_handed_wpn|itp_primary, 0, 15, weight(1)|abundance(100)|weapon_length(80)|swing_damage(5, blunt), imodbits_none, []],
  ["ni_oil_pot", "Oil Pot", [("oil_pot",0)], itp_type_thrown|itp_primary, 0, 50, weight(3)|abundance(50)|thrust_damage(20, blunt)|weapon_length(20), imodbits_none, []],

  # Mechanic 8: Loot bags (as items that become scene props)
  ["ni_loot_bag_gold", "Bag of Gold", [("sack",0)], itp_type_goods, 0, 500, weight(10), imodbits_none, []],
  ["ni_blueprint_rare", "Rare Blueprint", [("book_a",0)], itp_type_book, 0, 1000, weight(1), imodbits_none, []],

  # Mechanic 14: Powder barrel
  ["ni_powder_barrel", "Powder Barrel", [("barrel",0)], itp_type_goods, 0, 100, weight(15), imodbits_none, []],
]
