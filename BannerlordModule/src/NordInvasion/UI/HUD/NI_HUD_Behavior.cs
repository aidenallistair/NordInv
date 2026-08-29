using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using NordInvasion.Models;
using NordInvasion.Managers;

namespace NordInvasion.UI.HUD
{
    public class NI_HUD_Behavior : MissionBehavior
    {
        private NI_HUD_VM _vm;
        private float _lastUpdate = 0f;

        public NI_HUD_VM ViewModel => _vm;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            _vm = new NI_HUD_VM();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (_vm == null)
                _vm = new NI_HUD_VM();

            if (Mission.CurrentTime - _lastUpdate > 0.5f)
            {
                _lastUpdate = Mission.CurrentTime;
                RefreshHud();
            }
        }

        public void RefreshHud()
        {
            if (_vm == null || Mission.Current == null) return;

            var waveMgr = Mission.Current.GetMissionBehavior<Behaviors.NordInvasionWaveManagerBehavior>();
            var dir = Mission.Current.GetMissionBehavior<Behaviors.NordInvasionDirectorBehavior>();
            var supply = Mission.Current.GetMissionBehavior<Behaviors.SupplyBehavior>();

            var mainAgent = Mission.Current.MainAgent;
            var goldComp = mainAgent?.GetComponent<PersistenceManager.PlayerGoldComponent>();

            int gold = goldComp?.Gold ?? 500;
            int wood = goldComp?.Wood ?? 0;
            int metal = goldComp?.Metal ?? 0;
            int stress = dir?.Stress ?? 50;

            if (waveMgr != null)
            {
                _vm.WaveInfo = $"Wave {waveMgr.WaveNumber}/25 | Nords: {waveMgr.BotsAlive}/{waveMgr.BotsTotal}";
                _vm.MutatorInfo = waveMgr.Mutator != MutatorType.None ? $"Mutator: {waveMgr.Mutator}" : "No Mutator";
                _vm.ObjectiveInfo = $"Objective: {waveMgr.Objective}";
            }

            string supplyText = supply != null ? $" [Stock: {supply.WoodStock}w/{supply.MetalStock}m]" : "";
            _vm.GoldInfo = $"Gold: {gold} | Wood: {wood} | Metal: {metal} | Stress: {stress}{supplyText}";
        }

        public void UpdateWave(int wave, int botsTotal, int botsAlive, WaveObjective objective, MutatorType mutator)
        {
            if (_vm != null)
            {
                _vm.WaveInfo = $"Wave {wave}/25 | Nords: {botsAlive}/{botsTotal}";
                _vm.MutatorInfo = mutator != MutatorType.None ? $"Mutator: {mutator}" : "No Mutator";
                _vm.ObjectiveInfo = $"Objective: {objective}";
            }
        }

        public void UpdateResources(int gold, int wood, int metal, int stress)
        {
            if (_vm != null)
            {
                _vm.GoldInfo = $"Gold: {gold} | Wood: {wood} | Metal: {metal} | Stress: {stress}";
            }
        }
    }

    public class NI_HUD_VM : TaleWorlds.Library.ViewModel
    {
        private string _waveInfo = "Wave 1/25 | Preparing...";
        private string _mutatorInfo = "No Mutator";
        private string _goldInfo = "Gold: 500 | Wood: 0 | Metal: 0 | Stress: 50";
        private string _objectiveInfo = "Objective: Kill All Nords";

        [DataSourceProperty]
        public string WaveInfo { get => _waveInfo; set { if (_waveInfo != value) { _waveInfo = value; OnPropertyChanged(nameof(WaveInfo)); } } }

        [DataSourceProperty]
        public string MutatorInfo { get => _mutatorInfo; set { if (_mutatorInfo != value) { _mutatorInfo = value; OnPropertyChanged(nameof(MutatorInfo)); } } }

        [DataSourceProperty]
        public string GoldInfo { get => _goldInfo; set { if (_goldInfo != value) { _goldInfo = value; OnPropertyChanged(nameof(GoldInfo)); } } }

        [DataSourceProperty]
        public string ObjectiveInfo { get => _objectiveInfo; set { if (_objectiveInfo != value) { _objectiveInfo = value; OnPropertyChanged(nameof(ObjectiveInfo)); } } }
    }
}
