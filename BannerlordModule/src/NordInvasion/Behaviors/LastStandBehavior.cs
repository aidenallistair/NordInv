using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using System.Linq;

namespace NordInvasion.Behaviors
{
    // Mechanic 29: Last Stand
    public class LastStandBehavior : MissionBehavior
    {
        private bool _inLastStand = false;
        private float _lastStandEndTime = 0f;
        private Agent _lastAlive = null;

        public override void OnMissionTick(float dt)
        {
            if (_inLastStand)
            {
                if (Mission.CurrentTime > _lastStandEndTime)
                {
                    EndLastStand(false);
                }
                return;
            }

            // Check condition: 1 alive, rest fallen, 1 nord left
            var playerTeam = Mission.PlayerTeam;
            if (playerTeam == null) return;

            int alive = playerTeam.ActiveAgents.Count;
            int fallen = 0;
            foreach (var agent in Mission.Current.AllAgents)
            {
                var wound = agent.GetComponent<Components.WoundStaminaComponent>();
                if (wound != null && wound.IsFallen) fallen++;
            }

            var waveManager = Mission.GetMissionBehavior<NordInvasionWaveManagerBehavior>();
            if (waveManager == null) return;

            if (alive == 1 && fallen == playerTeam.ActiveAgents.Count + fallen - 1 && waveManager.BotsAlive == 1)
            {
                StartLastStand(playerTeam.ActiveAgents.First());
            }
        }

        void StartLastStand(Agent lastAlive)
        {
            _inLastStand = true;
            _lastAlive = lastAlive;
            _lastStandEndTime = Mission.CurrentTime + 10f;

            Mission.Current.SetTimeSpeed(0.3f);
            lastAlive.SetMaximumSpeedFactor(1.5f);
            // +100% damage via component
            var perkComp = lastAlive.GetComponent<Components.PerkAgentComponent>();
            if (perkComp != null) perkComp.DamageMod += 1f;

            InformationManager.DisplayMessage(new InformationMessage("LAST STAND! 10 sec slow-mo! Last player +100% damage! Crawl and revive!", Colors.Red));
            // Music change
            // Mission.Current.SetMusic("last_stand_music");
        }

        void EndLastStand(bool success)
        {
            _inLastStand = false;
            Mission.Current.SetTimeSpeed(1f);
            if (_lastAlive != null && _lastAlive.IsActive())
            {
                _lastAlive.SetMaximumSpeedFactor(1f);
                var perkComp = _lastAlive.GetComponent<Components.PerkAgentComponent>();
                if (perkComp != null) perkComp.DamageMod -= 1f;
            }

            if (success)
            {
                InformationManager.DisplayMessage(new InformationMessage("LAST STAND SUCCESS! Wave continues!", Colors.Green));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("LAST STAND FAILED! Defeat with honor!", Colors.Red));
                // Epic defeat cutscene
                var waveManager = Mission.GetMissionBehavior<NordInvasionWaveManagerBehavior>();
                if (waveManager != null) waveManager.State = WaveState.Failed;
            }
        }

        public void OnPlayerRevived()
        {
            if (_inLastStand)
            {
                EndLastStand(true);
            }
        }
    }
}
