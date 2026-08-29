using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace NordInvasion.Behaviors
{
    // Mechanic 5: AI-Director like Left 4 Dead
    public class NordInvasionDirectorBehavior : MissionBehavior
    {
        public int Stress = 50; // 0-100, low = team losing, high = team winning

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            // Calculate every second
            if (Mission.CurrentTime % 1f < 0.1f)
                CalculateStress();
        }

        void CalculateStress()
        {
            var playerTeam = Mission.PlayerTeam;
            if (playerTeam == null) return;

            int alive = playerTeam.ActiveAgents.Count;
            int totalPlayers = playerTeam.ActiveAgents.Count + playerTeam.DeathAgents.Count;

            // If many dead -> stress low (need relief)
            if (totalPlayers > 0)
            {
                float aliveRatio = (float)alive / totalPlayers;
                if (aliveRatio < 0.3f) Stress -= 2;
                else if (aliveRatio > 0.8f) Stress += 1;
            }

            Stress = MathF.Clamp(Stress, 0, 100);
        }

        public void OnBotKilled() => Stress = MathF.Clamp(Stress - 1, 0, 100);
        public void OnPlayerDied() => Stress = MathF.Clamp(Stress + 2, 0, 100);
        public void OnWaveCompleted() => Stress = MathF.Clamp(Stress - 5, 0, 100);

        public float GetMultiplier()
        {
            if (Stress > 80) return 1.2f; // team winning, add pressure
            if (Stress < 30) return 0.8f; // team losing, relief
            return 1f;
        }

        public void TryRelief()
        {
            if (Stress < 20)
            {
                InformationManager.DisplayMessage(new InformationMessage("Director: Relief - Ammo box spawned!", Colors.Yellow));
                // Spawn ammo box at player base
                // Mission.GetMissionBehavior<FortressBuildManager>().SpawnAmmoBox();
                Stress = 30;
            }
        }
    }
}
