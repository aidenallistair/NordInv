using System.Collections.Generic;

namespace NordInvasion.Models
{
    // Каталог магазина (Mechanic 12/2/18/23).
    //
    // Канон - src/backend-php/shop_catalog.json: из него цены отдаёт PHP-бэкенд,
    // ему же сверяется tools/validate_module.py. Здесь лежит встроенный fallback,
    // чтобы магазин работал и без бэкенда (одиночная игра / сервер без MySQL):
    // тогда покупка применяется локально (PersistenceManager.BuyLocal).
    public class ShopItem
    {
        public string Id;
        public string Name;
        public string Type;      // resource | service | blueprint
        public string Desc;
        public int Gold;
        public int Wood;
        public int Metal;
        public string[] Grants = new string[0];
    }

    public static class ShopCatalog
    {
        public const string CatalogVersion = "1";

        /// <summary>Мета-уровень battlepass: сколько очков сезона на 1 уровень.</summary>
        public static int PointsPerLevel = 25;

        public static readonly List<ShopItem> All = new List<ShopItem>
        {
            // --- ресурсы (склад забега) ---
            new ShopItem { Id = "wood_pack_10", Name = "Wood Pack x10", Type = "resource", Gold = 60,
                Grants = new[] { "wood:10" }, Desc = "10 wood for the fort" },
            new ShopItem { Id = "metal_pack_5", Name = "Scrap Metal Pack x5", Type = "resource", Gold = 80,
                Grants = new[] { "metal:5" }, Desc = "5 scrap metal for doors and machines" },
            new ShopItem { Id = "supply_pack", Name = "Supply Wagon Pack", Type = "resource", Gold = 120,
                Grants = new[] { "wood:10", "metal:5" }, Desc = "10 wood + 5 metal (caravan price)" },

            // --- сервисы (мгновенный эффект в бою) ---
            new ShopItem { Id = "heal_kit", Name = "Field Dressing", Type = "service", Gold = 40,
                Grants = new[] { "heal:40" }, Desc = "Restore 40 HP instantly" },
            new ShopItem { Id = "ammo_crate", Name = "Ammo Crate", Type = "service", Gold = 50,
                Grants = new[] { "ammo" }, Desc = "Spawn an ammo box at your feet" },
            new ShopItem { Id = "repair_kit", Name = "Repair Kit", Type = "service", Gold = 35,
                Grants = new[] { "repair:60" }, Desc = "+60 HP to the nearest barricade" },

            // --- чертежи (открывают позиции в Build Menu) ---
            new ShopItem { Id = "wall_door", Name = "Blueprint: Gate Door", Type = "blueprint", Gold = 150,
                Grants = new[] { "blueprint:wall_door" }, Desc = "Unlocks Door in the build menu" },
            new ShopItem { Id = "brazier", Name = "Blueprint: Brazier", Type = "blueprint", Gold = 90,
                Grants = new[] { "blueprint:brazier" }, Desc = "Light the fort on night waves" },
            new ShopItem { Id = "stakes", Name = "Blueprint: Sharpened Stakes", Type = "blueprint", Gold = 120, Wood = 20,
                Grants = new[] { "blueprint:stakes" }, Desc = "Anti-cavalry stakes (wave 10+)" },
            new ShopItem { Id = "spike_trap", Name = "Blueprint: Spike Trap", Type = "blueprint", Gold = 100, Wood = 10,
                Grants = new[] { "blueprint:spike_trap" }, Desc = "Cheap 200 HP trap, breaks cavalry charges" },
            new ShopItem { Id = "oil_cauldron", Name = "Blueprint: Oil Cauldron", Type = "blueprint", Gold = 200, Wood = 20, Metal = 5,
                Grants = new[] { "blueprint:oil_cauldron" }, Desc = "Boiling oil over the wall" },
            new ShopItem { Id = "shield_wall", Name = "Blueprint: Shield Wall", Type = "blueprint", Gold = 180, Wood = 30,
                Grants = new[] { "blueprint:shield_wall" }, Desc = "1200 HP reusable wall segment" },
            new ShopItem { Id = "ballista", Name = "Blueprint: Ballista", Type = "blueprint", Gold = 250, Wood = 10, Metal = 8,
                Grants = new[] { "blueprint:ballista" }, Desc = "Siege ballista, pierces 3 nords" },
            new ShopItem { Id = "catapult", Name = "Blueprint: Catapult", Type = "blueprint", Gold = 300, Wood = 15, Metal = 12,
                Grants = new[] { "blueprint:catapult" }, Desc = "AOE stone thrower" },
            new ShopItem { Id = "rock_trap", Name = "Blueprint: Rock Trap", Type = "blueprint", Gold = 150, Metal = 4,
                Grants = new[] { "blueprint:rock_trap" }, Desc = "One-shot crush trap on a path" },
            new ShopItem { Id = "log_trap", Name = "Blueprint: Log Trap", Type = "blueprint", Gold = 150, Wood = 12,
                Grants = new[] { "blueprint:log_trap" }, Desc = "Rolling log down the approach" },
            new ShopItem { Id = "oil_ditch", Name = "Blueprint: Oil Ditch", Type = "blueprint", Gold = 200, Metal = 10,
                Grants = new[] { "blueprint:oil_ditch" }, Desc = "Spill oil, ignite with a torch" },
        };

        static Dictionary<string, ShopItem> _byId;

        public static Dictionary<string, ShopItem> ById
        {
            get
            {
                if (_byId == null)
                {
                    _byId = new Dictionary<string, ShopItem>();
                    foreach (var item in All)
                        if (!_byId.ContainsKey(item.Id)) _byId[item.Id] = item;
                }
                return _byId;
            }
        }

        public static ShopItem Get(string id)
        {
            ShopItem item;
            return id != null && ById.TryGetValue(id, out item) ? item : null;
        }

        /// <summary>
        /// Замена каталога ответом бэкенда (GET /api/shop/catalog).
        /// Пустой/битый ответ - оставляем встроенный fallback (мод не должен
        /// оставаться без магазина, если MySQL недоступен).
        /// </summary>
        public static int ReplaceWith(Dictionary<string, object> backendCatalog)
        {
            if (backendCatalog == null) return 0;
            var items = Utils.NIJson.GetObjectArray(backendCatalog, "items");
            if (items.Count == 0) return 0;

            var next = new List<ShopItem>();
            foreach (var row in items)
            {
                var id = Utils.NIJson.GetString(row, "id");
                if (string.IsNullOrEmpty(id)) continue;
                var grants = Utils.NIJson.GetStringArray(row, "grants");
                next.Add(new ShopItem
                {
                    Id = id,
                    Name = Utils.NIJson.GetString(row, "name", id),
                    Type = Utils.NIJson.GetString(row, "type", "resource"),
                    Desc = Utils.NIJson.GetString(row, "desc"),
                    Gold = Utils.NIJson.GetInt(row, "gold"),
                    Wood = Utils.NIJson.GetInt(row, "wood"),
                    Metal = Utils.NIJson.GetInt(row, "metal"),
                    Grants = grants,
                });
            }
            if (next.Count == 0) return 0;

            All.Clear();
            All.AddRange(next);
            _byId = null;
            PointsPerLevel = Utils.NIJson.GetInt(backendCatalog, "bp_points_per_level", 25);
            return next.Count;
        }

        /// <summary>Чертеж, нужный для типа постройки (FortressBuildManager).</summary>
        public static string BlueprintFor(Managers.FortressBuildManager.BuildType type)
        {
            switch (type)
            {
                case Managers.FortressBuildManager.BuildType.Door: return "wall_door";
                case Managers.FortressBuildManager.BuildType.Stakes: return "stakes";
                case Managers.FortressBuildManager.BuildType.SpikeTrap: return "spike_trap";
                case Managers.FortressBuildManager.BuildType.OilCauldron: return "oil_cauldron";
                case Managers.FortressBuildManager.BuildType.Brazier: return "brazier";
                case Managers.FortressBuildManager.BuildType.ShieldWall: return "shield_wall";
                case Managers.FortressBuildManager.BuildType.Ballista: return "ballista";
                case Managers.FortressBuildManager.BuildType.Catapult: return "catapult";
                case Managers.FortressBuildManager.BuildType.RockTrap: return "rock_trap";
                case Managers.FortressBuildManager.BuildType.LogTrap: return "log_trap";
                case Managers.FortressBuildManager.BuildType.OilDitch: return "oil_ditch";
                default: return ""; // Foundation/Wall - всегда доступны
            }
        }
    }
}
