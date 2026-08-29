using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using NordInvasion.Managers;

namespace NordInvasion.UI
{
    public class NI_Shop_VM : ViewModel
    {
        private string _goldText = "Gold: 500 | Wood: 0 | Metal: 0";
        private bool _isVisible = false;

        [DataSourceProperty] public string GoldText { get => _goldText; set { if (_goldText != value) { _goldText = value; OnPropertyChanged(nameof(GoldText)); } } }
        [DataSourceProperty] public bool IsVisible { get => _isVisible; set { if (_isVisible != value) { _isVisible = value; OnPropertyChanged(nameof(IsVisible)); } } }

        public void Refresh(Agent agent)
        {
            var comp = agent.GetComponent<PersistenceManager.PlayerGoldComponent>();
            if (comp != null)
                GoldText = $"Gold: {comp.Gold} | Wood: {comp.Wood} | Metal: {comp.Metal} | Kills: {comp.Kills}";
        }

        public void Buy(string itemId, int goldCost, int woodCost = 0, int metalCost = 0)
        {
            var playerAgent = Mission.Current?.MainAgent;
            if (playerAgent == null) return;
            var comp = playerAgent.GetComponent<PersistenceManager.PlayerGoldComponent>();
            if (comp == null) return;

            if (comp.Gold < goldCost || comp.Wood < woodCost || comp.Metal < metalCost)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Not enough resources! Need {goldCost}g {woodCost}w {metalCost}m", Colors.Red));
                return;
            }

            comp.Gold -= goldCost;
            comp.Wood -= woodCost;
            comp.Metal -= metalCost;

            // Give item
            // playerAgent.EquipWeaponFromInventory...
            InformationManager.DisplayMessage(new InformationMessage($"Bought {itemId} for {goldCost}g!", Colors.Green));
            Refresh(playerAgent);
        }

        // Called from Gauntlet buttons
        public void ExecuteBuySword() => Buy("Sword", 50);
        public void ExecuteBuyBow() => Buy("Bow", 80);
        public void ExecuteBuyArmor() => Buy("Armor", 100);
        public void ExecuteBuyBarricade() => Buy("Barricade Kit", 150, 5, 0);
        public void ExecuteBuyStakes() => Buy("Stakes", 100, 4, 0);
        public void ExecuteBuyOil() => Buy("Oil Cauldron", 200, 10, 5);
        public void ExecuteClose() => IsVisible = false;
    }

    public class NI_BuildMenu_VM : ViewModel
    {
        private string _resources = "Wood: 0 | Metal: 0";
        [DataSourceProperty] public string Resources { get => _resources; set { if (_resources != value) { _resources = value; OnPropertyChanged(nameof(Resources)); } } }

        public void Refresh(Agent agent)
        {
            var comp = agent.GetComponent<PersistenceManager.PlayerGoldComponent>();
            if (comp != null) Resources = $"Wood: {comp.Wood} | Metal: {comp.Metal}";
        }

        public void ExecuteBuildFoundation()
        {
            var agent = Mission.Current?.MainAgent;
            if (agent == null) return;
            Mission.Current.GetMissionBehavior<FortressBuildManager>()?.TryPlace(FortressBuildManager.BuildType.Foundation, agent);
            Refresh(agent);
        }
        public void ExecuteBuildWall() { var a = Mission.Current?.MainAgent; if (a != null) { Mission.Current.GetMissionBehavior<FortressBuildManager>()?.TryPlace(FortressBuildManager.BuildType.Wall, a); Refresh(a); } }
        public void ExecuteBuildDoor() { var a = Mission.Current?.MainAgent; if (a != null) { Mission.Current.GetMissionBehavior<FortressBuildManager>()?.TryPlace(FortressBuildManager.BuildType.Door, a); Refresh(a); } }
        public void ExecuteBuildStakes() { var a = Mission.Current?.MainAgent; if (a != null) { Mission.Current.GetMissionBehavior<FortressBuildManager>()?.TryPlace(FortressBuildManager.BuildType.Stakes, a); Refresh(a); } }
        public void ExecuteBuildOil() { var a = Mission.Current?.MainAgent; if (a != null) { Mission.Current.GetMissionBehavior<FortressBuildManager>()?.TryPlace(FortressBuildManager.BuildType.OilCauldron, a); Refresh(a); } }
    }

    public class NI_PerkChoice_VM : ViewModel
    {
        private string _perk1Name = "Iron Skin +15% HP";
        private string _perk1Desc = "Survivor branch";
        private string _perk2Name = "Bloodlust";
        private string _perk2Desc = "Damage when wounded";
        private string _perk3Name = "Engineer +30% HP barricades";
        private string _perk3Desc = "Tactician branch";
        private int _timeLeft = 15;

        [DataSourceProperty] public string Perk1Name { get => _perk1Name; set { _perk1Name = value; OnPropertyChanged(nameof(Perk1Name)); } }
        [DataSourceProperty] public string Perk1Desc { get => _perk1Desc; set { _perk1Desc = value; OnPropertyChanged(nameof(Perk1Desc)); } }
        [DataSourceProperty] public string Perk2Name { get => _perk2Name; set { _perk2Name = value; OnPropertyChanged(nameof(Perk2Name)); } }
        [DataSourceProperty] public string Perk2Desc { get => _perk2Desc; set { _perk2Desc = value; OnPropertyChanged(nameof(Perk2Desc)); } }
        [DataSourceProperty] public string Perk3Name { get => _perk3Name; set { _perk3Name = value; OnPropertyChanged(nameof(Perk3Name)); } }
        [DataSourceProperty] public string Perk3Desc { get => _perk3Desc; set { _perk3Desc = value; OnPropertyChanged(nameof(Perk3Desc)); } }
        [DataSourceProperty] public int TimeLeft { get => _timeLeft; set { _timeLeft = value; OnPropertyChanged(nameof(TimeLeft)); } }

        private int[] _perkIds = new int[3];

        public void SetPerks(Models.PerkDefinition p1, Models.PerkDefinition p2, Models.PerkDefinition p3)
        {
            Perk1Name = p1.Name; Perk1Desc = p1.Desc; _perkIds[0] = p1.Id;
            Perk2Name = p2.Name; Perk2Desc = p2.Desc; _perkIds[1] = p2.Id;
            Perk3Name = p3.Name; Perk3Desc = p3.Desc; _perkIds[2] = p3.Id;
        }

        public void ExecuteChoose1() { Apply(_perkIds[0]); }
        public void ExecuteChoose2() { Apply(_perkIds[1]); }
        public void ExecuteChoose3() { Apply(_perkIds[2]); }

        void Apply(int id)
        {
            var agent = Mission.Current?.MainAgent;
            if (agent != null) Mission.Current.GetMissionBehavior<PerkManager>()?.ApplyPerk(agent, id);
            // Close UI
        }
    }
}
