using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using System.Collections.Generic;
using System.Text;
using NordInvasion.Managers;

namespace NordInvasion.UI
{
    /// <summary>
    /// Механика 15: карта кампании. Голос уходит на бэкенд (POST /api/campaign/vote,
    /// 1 голос на сезон - уникальность держит MySQL), список деревень и голоса
    /// приходят с сервера (GET /api/campaign/villages). Игрок, за которого проголосовало
    /// больше всех, становится целью забега: результат передаётся в /api/campaign/battle
    /// (см. PersistenceManager.OnCampaignWin).
    /// </summary>
    public class NI_CampaignMap_VM : ViewModel
    {
        private string _info = "Village lines come from the backend (1 vote per season)";
        private string _rows = "";

        [DataSourceProperty] public string Info { get => _info; set { if (_info != value) { _info = value; OnPropertyChanged(nameof(Info)); } } }
        [DataSourceProperty] public string VillageRows { get => _rows; set { if (_rows != value) { _rows = value; OnPropertyChanged(nameof(VillageRows)); } } }

        public void Refresh()
        {
            var villages = PersistenceManager.Villages;
            if (villages == null || villages.Count == 0)
            {
                Info = "No campaign data from backend yet - votes are queued locally";
                VillageRows = "";
                return;
            }

            var sb = new StringBuilder();
            lock (villages)
            {
                foreach (var v in villages)
                    sb.Append(v.Id).Append(". ").Append(v.Name)
                      .Append("  owner=").Append(v.Owner)
                      .Append(" def=").Append(v.Defense)
                      .Append(" votes=").Append(v.Votes).Append('\n');
            }
            VillageRows = sb.ToString();
            int lead = LeadingVillageId();
            Info = lead >= 0 ? $"Majority: village {lead} - it will be attacked next run" : "No votes yet this season";
        }

        /// <summary>Деревня с наибольшим числом голосов (-1 - данных нет).</summary>
        public static int LeadingVillageId()
        {
            var villages = PersistenceManager.Villages;
            if (villages == null || villages.Count == 0) return -1;
            int best = -1, bestVotes = 0;
            lock (villages)
            {
                foreach (var v in villages)
                    if (v.Votes > bestVotes) { bestVotes = v.Votes; best = v.Id; }
            }
            return bestVotes > 0 ? best : -1;
        }

        void Vote(int villageId)
        {
            var agent = Mission.Current != null ? Mission.Current.MainAgent : null;
            var persist = Mission.Current != null ? Mission.Current.GetMissionBehavior<PersistenceManager>() : null;
            if (agent == null || persist == null) return;
            persist.VoteForVillage(agent, villageId);
            Refresh();
        }

        public void ExecuteVillage1() => Vote(0);
        public void ExecuteVillage2() => Vote(1);
        public void ExecuteVillage3() => Vote(2);
        public void ExecuteVillage4() => Vote(3);
        public void ExecuteVillage5() => Vote(4);
        public void ExecuteVillage6() => Vote(5);
        public void ExecuteVillage7() => Vote(6);
        public void ExecuteVillage8() => Vote(7);

        public void ExecuteRefresh()
        {
            Mission.Current?.GetMissionBehavior<PersistenceManager>()?.RefreshCampaignMap();
            Refresh();
        }

        /// <summary>Открыть фазу лагеря (кнопка "Make camp here" - торговцы/кузнец).</summary>
        public void ExecuteMakeCamp()
        {
            Mission.Current?.GetMissionBehavior<Behaviors.CampPhaseBehavior>()?.StartCampPhase();
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
