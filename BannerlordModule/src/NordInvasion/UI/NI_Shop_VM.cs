using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using NordInvasion.Managers;
using NordInvasion.Models;
using System.Collections.Generic;
using System.Text;

namespace NordInvasion.UI
{
    // Механика 12 (персистенция) + 2/18/23 (чертежи открывают постройки).
    //
    // Покупка идёт ЧЕРЕЗ БЭКЕНД (/api/shop/buy): цену, баланс и выдачу чертежа
    // проверяет сервер, мод получает новый баланс + список наград. Без бэкенда
    // PersistenceManager применяет покупку локально (забег сохраняется только
    // в памяти) - магазин не должен исчезать из-за недоступного MySQL.
    public class NI_Shop_VM : ViewModel
    {
        /// <summary>Позиций на одной странице меню (листается кнопкой NextPage).</summary>
        public const int PageSize = 8;

        private int _page;
        private string _goldText = "Gold: 500 | Wood: 0 | Metal: 0";
        private string _battlepassText = "";
        private string _itemsText = "";
        private bool _isVisible = false;

        [DataSourceProperty] public string GoldText { get => _goldText; set { if (_goldText != value) { _goldText = value; OnPropertyChanged(nameof(GoldText)); } } }
        [DataSourceProperty] public string BattlepassText { get => _battlepassText; set { if (_battlepassText != value) { _battlepassText = value; OnPropertyChanged(nameof(BattlepassText)); } } }
        [DataSourceProperty] public string ItemsText { get => _itemsText; set { if (_itemsText != value) { _itemsText = value; OnPropertyChanged(nameof(ItemsText)); } } }
        [DataSourceProperty] public bool IsVisible { get => _isVisible; set { if (_isVisible != value) { _isVisible = value; OnPropertyChanged(nameof(IsVisible)); } } }

        static Agent Me => Mission.Current != null ? Mission.Current.MainAgent : null;

        /// <summary>Каталог + цены + battlepass-строка. Вызывается при открытии и после покупки.</summary>
        public void Refresh(Agent agent)
        {
            if (agent == null) agent = Me;
            if (agent == null) return;
            var comp = agent.GetComponent<PersistenceManager.PlayerGoldComponent>();
            if (comp != null)
                GoldText = $"Gold: {comp.Gold} | Wood: {comp.Wood} | Metal: {comp.Metal} | Kills: {comp.Kills} | BP lvl {comp.BattlepassLevel}";

            var bp = PersistenceManager.Battlepass;
            BattlepassText = string.IsNullOrEmpty(bp.Line)
                ? $"BattlePass: LV {bp.Level}/{bp.MaxLevel}, до след. награды {bp.PointsToNext} SP"
                : bp.Line;

            var sb = new StringBuilder();
            var page = PageItems();
            for (int i = 0; i < page.Count; i++)
            {
                var item = page[i];
                sb.Append(i + 1).Append(". ").Append(item.Name)
                  .Append("  [").Append(Price(item)).Append("]");
                if (item.Type == "blueprint" && comp != null)
                {
                    string bpId = BlueprintOf(item);
                    if (!string.IsNullOrEmpty(bpId) && comp.Blueprints.Contains(bpId)) sb.Append("  (OWNED)");
                }
                sb.Append('\n');
            }
            int pages = (ShopCatalog.All.Count + PageSize - 1) / PageSize;
            sb.Append($"стр. {_page + 1}/{pages} - {(_page + 1 < pages ? "NextPage" : "FirstPage")}");
            ItemsText = sb.ToString();
        }

        public List<ShopItem> PageItems()
        {
            var res = new List<ShopItem>();
            if (ShopCatalog.All.Count == 0) return res;
            int start = _page * PageSize;
            for (int i = start; i < ShopCatalog.All.Count && i < start + PageSize; i++)
                res.Add(ShopCatalog.All[i]);
            return res;
        }

        static string Price(ShopItem item)
        {
            var sb = new StringBuilder();
            if (item.Gold > 0) sb.Append(item.Gold).Append("g ");
            if (item.Wood > 0) sb.Append(item.Wood).Append("w ");
            if (item.Metal > 0) sb.Append(item.Metal).Append("m ");
            return sb.ToString().Trim();
        }

        static string BlueprintOf(ShopItem item)
        {
            if (item.Grants == null) return "";
            foreach (var g in item.Grants)
                if (g != null && g.StartsWith("blueprint:")) return g.Substring("blueprint:".Length);
            return "";
        }

        // ===== команды Gauntlet =====

        public void ExecuteBuySlot1() => BuySlot(0);
        public void ExecuteBuySlot2() => BuySlot(1);
        public void ExecuteBuySlot3() => BuySlot(2);
        public void ExecuteBuySlot4() => BuySlot(3);
        public void ExecuteBuySlot5() => BuySlot(4);
        public void ExecuteBuySlot6() => BuySlot(5);
        public void ExecuteBuySlot7() => BuySlot(6);
        public void ExecuteBuySlot8() => BuySlot(7);

        void BuySlot(int index)
        {
            var page = PageItems();
            if (index < 0 || index >= page.Count) return;
            Buy(page[index].Id);
        }

        /// <summary>Покупка позиции каталога (id - из shop_catalog.json / ответа бэкенда).</summary>
        public void Buy(string itemId)
        {
            var agent = Me;
            if (agent == null) return;
            var persist = Mission.Current.GetMissionBehavior<PersistenceManager>();
            if (persist == null) return;
            persist.BuyShopItem(agent, itemId);
            Refresh(agent);
        }

        public void ExecuteNextPage()
        {
            int pages = (ShopCatalog.All.Count + PageSize - 1) / PageSize;
            _page = pages <= 0 ? 0 : (_page + 1) % pages;
            Refresh(Me);
        }

        public void ExecutePrevPage()
        {
            int pages = (ShopCatalog.All.Count + PageSize - 1) / PageSize;
            _page = pages <= 0 ? 0 : (_page - 1 + pages) % pages;
            Refresh(Me);
        }

        /// <summary>Забирает первую доступную награду BattlePass (если есть).</summary>
        public void ExecuteClaimBattlepass()
        {
            var agent = Me;
            if (agent == null) return;
            int level = PersistenceManager.NextClaimableLevel();
            if (level <= 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "BattlePass: нет доступных наград (играй волны - растут Season Points)", Colors.Yellow));
                return;
            }
            Mission.Current.GetMissionBehavior<PersistenceManager>()?.ClaimBattlepass(agent, level);
            Refresh(agent);
        }

        /// <summary>Обновить каталог/профиль с бэкенда (кнопка "Reload").</summary>
        public void ExecuteReload()
        {
            var agent = Me;
            var persist = Mission.Current != null ? Mission.Current.GetMissionBehavior<PersistenceManager>() : null;
            if (persist == null || agent == null) return;
            persist.RefreshShopCatalog();
            persist.RefreshBattlepass(agent);
            Refresh(agent);
        }

        public void ExecuteClose() => IsVisible = false;

        // ===== совместимость со старыми кнопками NI_Shop.xml =====
        // Прежние позиции ("Buy Sword/Bow/Armor") были заглушками: выдача оружия
        // в миссии не была реализована, а ресурсы списывались локально. Теперь
        // кнопки ведут в реальный каталог; методы оставлены, чтобы не ломать XML.
        public void ExecuteBuySword() => Buy("heal_kit");
        public void ExecuteBuyBow() => Buy("ammo_crate");
        public void ExecuteBuyArmor() => Buy("supply_pack");
        public void ExecuteBuyBarricade() => Buy("wood_pack_10");
        public void ExecuteBuyStakes() => Buy("stakes");
        public void ExecuteBuyOil() => Buy("oil_cauldron");
        public void ExecuteBuyBallista() => Buy("ballista");
    }

    // Механика 2: Build Menu. Продвинутые позиции закрыты чертежами (ShopCatalog.BlueprintFor).
    public class NI_BuildMenu_VM : ViewModel
    {
        private string _resources = "Wood: 0 | Metal: 0";
        private string _locked = "";

        [DataSourceProperty] public string Resources { get => _resources; set { if (_resources != value) { _resources = value; OnPropertyChanged(nameof(Resources)); } } }
        [DataSourceProperty] public string LockedInfo { get => _locked; set { if (_locked != value) { _locked = value; OnPropertyChanged(nameof(LockedInfo)); } } }

        public void Refresh(Agent agent)
        {
            if (agent == null) return;
            var comp = agent.GetComponent<PersistenceManager.PlayerGoldComponent>();
            if (comp == null) return;
            Resources = $"Wood: {comp.Wood} | Metal: {comp.Metal} | Postings: {Mission.Current.GetMissionBehavior<FortressBuildManager>()?.BuiltCount ?? 0}/{FortressBuildManager.MaxBuildings}";

            var locked = new List<string>();
            foreach (var t in System.Enum.GetValues(typeof(FortressBuildManager.BuildType)))
            {
                var type = (FortressBuildManager.BuildType)t;
                var bp = ShopCatalog.BlueprintFor(type);
                if (!string.IsNullOrEmpty(bp) && !comp.Blueprints.Contains(bp)) locked.Add($"{type}: {bp}");
            }
            LockedInfo = locked.Count == 0 ? "All blueprints unlocked" : "Locked - " + string.Join(", ", locked);
        }

        static void Place(FortressBuildManager.BuildType type)
        {
            var agent = Mission.Current != null ? Mission.Current.MainAgent : null;
            if (agent == null) return;
            Mission.Current.GetMissionBehavior<FortressBuildManager>()?.TryPlace(type, agent);
            // VM может быть вызван из UI-команды без внешнего Refresh
            var vm = Mission.Current.GetMissionBehavior<NI_BuildMenu_Behavior>();
            vm?.Menu?.Refresh(agent);
        }

        public void ExecuteBuildFoundation() => Place(FortressBuildManager.BuildType.Foundation);
        public void ExecuteBuildWall() => Place(FortressBuildManager.BuildType.Wall);
        public void ExecuteBuildDoor() => Place(FortressBuildManager.BuildType.Door);
        public void ExecuteBuildStakes() => Place(FortressBuildManager.BuildType.Stakes);
        public void ExecuteBuildOil() => Place(FortressBuildManager.BuildType.OilCauldron);
        public void ExecuteBuildBrazier() => Place(FortressBuildManager.BuildType.Brazier);
        public void ExecuteBuildShieldWall() => Place(FortressBuildManager.BuildType.ShieldWall);
        public void ExecuteBuildSpikeTrap() => Place(FortressBuildManager.BuildType.SpikeTrap);
        public void ExecuteBuildBallista() => Place(FortressBuildManager.BuildType.Ballista);
        public void ExecuteBuildCatapult() => Place(FortressBuildManager.BuildType.Catapult);
        public void ExecuteBuildRockTrap() => Place(FortressBuildManager.BuildType.RockTrap);
        public void ExecuteBuildLogTrap() => Place(FortressBuildManager.BuildType.LogTrap);
        public void ExecuteBuildOilDitch() => Place(FortressBuildManager.BuildType.OilDitch);
    }

    /// <summary>
    /// Держит экземпляр NI_BuildMenu_VM, чтобы кнопки сами обновляли данные
    /// (Gauntlet-подключение prefab'ов ещё не сделано - см. docs/AUDIT.md;
    /// без него VM используется машиной ArmoryUsable и отладочными командами).
    /// </summary>
    public class NI_BuildMenu_Behavior : MissionBehavior
    {
        public NI_BuildMenu_VM Menu { get; private set; }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            Menu = new NI_BuildMenu_VM();
        }
    }

    // Механика 1: выбор перка. Кнопки 1/2/3 в NI_PerkChoice.xml -> PerkManager.
    public class NI_PerkChoice_VM : ViewModel
    {
        // Префикс пути к иконкам. Пока меши не готовы (docs/ART_TASKS.md) - пусто,
        // UI показывает только текст. Когда меши+материалы будут импортированы:
        // IconPathPrefix = "Modules/NordInvasion/Textures/PerkIcons/"
        public static string IconPathPrefix = "";

        private string _perk1Name = "Iron Skin I +15% HP";
        private string _perk1Desc = "Survivor branch";
        private string _perk1Icon = "";
        private string _perk2Name = "Bloodlust";
        private string _perk2Desc = "Damage when wounded";
        private string _perk2Icon = "";
        private string _perk3Name = "Engineer I";
        private string _perk3Desc = "Barricades +30% HP";
        private string _perk3Icon = "";
        private int _timeLeft = 15;

        [DataSourceProperty] public string Perk1Name { get => _perk1Name; set { _perk1Name = value; OnPropertyChanged(nameof(Perk1Name)); } }
        [DataSourceProperty] public string Perk1Desc { get => _perk1Desc; set { _perk1Desc = value; OnPropertyChanged(nameof(Perk1Desc)); } }
        [DataSourceProperty] public string Perk1Icon { get => _perk1Icon; set { _perk1Icon = value; OnPropertyChanged(nameof(Perk1Icon)); } }
        [DataSourceProperty] public string Perk2Name { get => _perk2Name; set { _perk2Name = value; OnPropertyChanged(nameof(Perk2Name)); } }
        [DataSourceProperty] public string Perk2Desc { get => _perk2Desc; set { _perk2Desc = value; OnPropertyChanged(nameof(Perk2Desc)); } }
        [DataSourceProperty] public string Perk2Icon { get => _perk2Icon; set { _perk2Icon = value; OnPropertyChanged(nameof(Perk2Icon)); } }
        [DataSourceProperty] public string Perk3Name { get => _perk3Name; set { _perk3Name = value; OnPropertyChanged(nameof(Perk3Name)); } }
        [DataSourceProperty] public string Perk3Desc { get => _perk3Desc; set { _perk3Desc = value; OnPropertyChanged(nameof(Perk3Desc)); } }
        [DataSourceProperty] public string Perk3Icon { get => _perk3Icon; set { _perk3Icon = value; OnPropertyChanged(nameof(Perk3Icon)); } }
        [DataSourceProperty] public int TimeLeft { get => _timeLeft; set { _timeLeft = value; OnPropertyChanged(nameof(TimeLeft)); } }

        private int[] _perkIds = new int[3];

        /// <summary>ID трёх предложенных перков (для отладки и для машины TotemUsable).</summary>
        public int[] CurrentPerkIds => _perkIds;

        public void SetPerks(PerkDefinition p1, PerkDefinition p2, PerkDefinition p3)
        {
            Perk1Name = p1.Name; Perk1Desc = p1.Desc; _perkIds[0] = p1.Id;
            Perk2Name = p2.Name; Perk2Desc = p2.Desc; _perkIds[1] = p2.Id;
            Perk3Name = p3.Name; Perk3Desc = p3.Desc; _perkIds[2] = p3.Id;
            Perk1Icon = IconPathFor(p1.Icon);
            Perk2Icon = IconPathFor(p2.Icon);
            Perk3Icon = IconPathFor(p3.Icon);
            TimeLeft = 15;
        }

        static string IconPathFor(string iconKey) =>
            string.IsNullOrEmpty(IconPathPrefix) ? "" : IconPathPrefix + iconKey + ".dds";

        public void ExecuteChoose1() => Apply(0);
        public void ExecuteChoose2() => Apply(1);
        public void ExecuteChoose3() => Apply(2);
        public void ExecuteSkip() => Apply(-1); // -1 = случайный (как тайм-аут)

        void Apply(int slot)
        {
            var agent = Mission.Current != null ? Mission.Current.MainAgent : null;
            if (agent == null) return;
            // Слот 0/1/2 -> текущий выбор из PerkManager (тайм-аут = рандом)
            Mission.Current.GetMissionBehavior<PerkManager>()?.ChooseForAgent(agent, slot);
        }
    }
}
