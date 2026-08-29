using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using System.Linq;

namespace NordInvasion.Behaviors
{
    // Mechanic 5: AI-Director like Left 4 Dead
    public class NordInvasionDirectorBehavior : MissionBehavior
    {
        public int Stress = 50; // 0-100, low = team losing, high = winning
        public Agent MarkedPlayer = null; // For Odin mutator

        private float _lastCalcTime = 0f;

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (Mission.CurrentTime - _lastCalcTime > 1f)
            {
                _lastCalcTime = Mission.CurrentTime;
                CalculateStress();
            }
        }

        void CalculateStress()
        {
            var playerTeam = Mission.PlayerTeam;
            if (playerTeam == null) return;

            int alive = playerTeam.ActiveAgents.Count;
            int total = alive + playerTeam.DeathAgents.Count(a => a != null);
            total = System.Math.Max(total, 1);

            float aliveRatio = (float)alive / total;
            if (aliveRatio < 0.3f) Stress = (int)MathF.Clamp(Stress - 2, 0, 100);
            else if (aliveRatio > 0.8f) Stress = (int)MathF.Clamp(Stress + 1, 0, 100);
        }

        public void OnBotKilled() => Stress = (int)MathF.Clamp(Stress - 1, 0, 100);
        public void OnPlayerDied() => Stress = (int)MathF.Clamp(Stress + 3, 0, 100);
        public void OnWaveCompleted() => Stress = (int)MathF.Clamp(Stress - 5, 0, 100);

        public float GetMultiplier()
        {
            if (Stress > 80) return 1.2f;
            if (Stress < 30) return 0.8f;
            return 1f;
        }

        void TryRelief()
        {
            InformationManager.DisplayMessage(new InformationMessage("Director: Relief - Ammo box spawned! Stress low", Colors.Yellow));
            Stress = 30;
        }
    }
}
