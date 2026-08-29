using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using NordInvasion.Models;

namespace NordInvasion.UI.HUD
{
    public class NI_HUD_Behavior : MissionBehavior
    {
        private NI_HUD_VM _vm;

        public override void OnMissionTick(float dt)
        {
            if (_vm == null)
            {
                _vm = new NI_HUD_VM();
                // Mission.Current.AddViewModel(_vm); // Gauntlet
            }
        }

        public void UpdateWave(int wave, int botsTotal, int botsAlive, WaveObjective objective, MutatorType mutator)
        {
            if (_vm != null)
            {
                _vm.WaveInfo = $"Wave: {wave} | Nords: {botsAlive}/{botsTotal} | Obj: {objective}";
                _vm.MutatorInfo = $"Mutator: {mutator}";
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
        private string _waveInfo = "Wave 1";
        private string _mutatorInfo = "Mutator: None";
        private string _goldInfo = "Gold: 500";

        [DataSourceProperty]
        public string WaveInfo { get => _waveInfo; set { if (_waveInfo != value) { _waveInfo = value; OnPropertyChanged(nameof(WaveInfo)); } } }

        [DataSourceProperty]
        public string MutatorInfo { get => _mutatorInfo; set { if (_mutatorInfo != value) { _mutatorInfo = value; OnPropertyChanged(nameof(MutatorInfo)); } } }

        [DataSourceProperty]
        public string GoldInfo { get => _goldInfo; set { if (_goldInfo != value) { _goldInfo = value; OnPropertyChanged(nameof(GoldInfo)); } } }
    }
}
