using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using System.Collections.Generic;

namespace NordInvasion.UI
{
    public class NI_CampaignMap_VM : ViewModel
    {
        private string _info = "Choose village - vote with team";

        [DataSourceProperty] public string Info { get => _info; set { _info = value; OnPropertyChanged(nameof(Info)); } }

        public void ExecuteVillage1() => Vote(0);
        public void ExecuteVillage2() => Vote(1);
        public void ExecuteVillage3() => Vote(2);
        public void ExecuteVillage4() => Vote(3);
        public void ExecuteVillage5() => Vote(4);
        public void ExecuteVillage6() => Vote(5);
        public void ExecuteVillage7() => Vote(6);
        public void ExecuteVillage8() => Vote(7);

        void Vote(int villageId)
        {
            var player = Mission.Current?.MainAgent;
            if (player == null) return;
            Mission.Current.GetMissionBehavior<Behaviors.CampPhaseBehavior>()?.StartCampPhase(); // placeholder
            InformationManager.DisplayMessage(new InformationMessage($"Voted for village {villageId}! Majority wins", Colors.Cyan));
            // POST /api/campaign/vote
        }
    }

    public class NI_ClassSelect_VM : ViewModel
    {
        public void ExecuteInfantry() => SelectClass(Components.PlayerClass.Infantry);
        public void ExecuteArcher() => SelectClass(Components.PlayerClass.Archer);
        public void ExecuteMedic() => SelectClass(Components.PlayerClass.Medic);
        public void ExecuteEngineer() => SelectClass(Components.PlayerClass.Engineer);
        public void ExecuteBanner() => SelectClass(Components.PlayerClass.Banner);

        void SelectClass(Components.PlayerClass cls)
        {
            var agent = Mission.Current?.MainAgent;
            if (agent == null) return;
            var comp = agent.GetComponent<Components.ClassComponent>();
            if (comp == null) agent.AddComponent(new Components.ClassComponent(agent, cls));
            else comp.Class = cls;

            // Add role component
            switch (cls)
            {
                case Components.PlayerClass.Medic:
                    if (agent.GetComponent<Components.MedicComponent>() == null) agent.AddComponent(new Components.MedicComponent(agent));
                    break;
                case Components.PlayerClass.Engineer:
                    if (agent.GetComponent<Components.EngineerComponent>() == null) agent.AddComponent(new Components.EngineerComponent(agent));
                    break;
                case Components.PlayerClass.Banner:
                    if (agent.GetComponent<Components.BannerComponent>() == null) agent.AddComponent(new Components.BannerComponent(agent));
                    break;
            }

            InformationManager.DisplayMessage(new InformationMessage($"Class selected: {cls}", Colors.Green));
        }
    }

    public class NI_Spectator_VM : ViewModel
    {
        private string _spectatedPlayer = "Spectating: Player1";
        private string _betInfo = "Place bet on survivor! Dead players only";

        [DataSourceProperty] public string SpectatedPlayer { get => _spectatedPlayer; set { _spectatedPlayer = value; OnPropertyChanged(nameof(SpectatedPlayer)); } }
        [DataSourceProperty] public string BetInfo { get => _betInfo; set { _betInfo = value; OnPropertyChanged(nameof(BetInfo)); } }

        public void ExecuteBetPlayer1() => PlaceBet(0);
        public void ExecuteBetPlayer2() => PlaceBet(1);

        void PlaceBet(int playerIndex)
        {
            var betting = Mission.Current.GetMissionBehavior<Behaviors.SpectatorBettingBehavior>();
            betting?.PlaceBet("local", $"player{playerIndex}", 50);
        }
    }
}
