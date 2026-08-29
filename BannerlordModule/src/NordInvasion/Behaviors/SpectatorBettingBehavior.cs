using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using System.Collections.Generic;
using NordInvasion.Utils;

namespace NordInvasion.Behaviors
{
    // Mechanic 27: Spectator, Killcam, Betting
    public class SpectatorBettingBehavior : MissionBehavior
    {
        public class Bet
        {
            public string PlayerId;
            public string BetOnPlayerId;
            public int Amount;
        }

        private List<Bet> _bets = new List<Bet>();
        private Agent _lastKiller;
        private Agent _lastVictim;
        private float _killcamEndTime = 0f;
        private bool _inKillcam = false;

        public void PlaceBet(string bettorId, string betOnId, int amount)
        {
            // Only dead players can bet
            _bets.Add(new Bet { PlayerId = bettorId, BetOnPlayerId = betOnId, Amount = amount });
            InformationManager.DisplayMessage(new InformationMessage($"{bettorId} bet {amount} on {betOnId} to survive!", Colors.Yellow));
        }

        public void OnPlayerKilled(Agent victim, Agent killer)
        {
            // Killcam
            if (killer != null && killer.Character.StringId.Contains("chieftain"))
            {
                _lastKiller = killer;
                _lastVictim = victim;
                _inKillcam = true;
                _killcamEndTime = Mission.CurrentTime + 3f;
                // Slow motion
                Mission.Current.SetTimeSpeed(0.3f);
                InformationManager.DisplayMessage(new InformationMessage($"KILLCAM: {killer.Name} killed {victim.Name}!", Colors.Red));
            }
        }

        public void OnWaveCompleted(int waveNumber, List<Agent> survivors)
        {
            // Pay bets
            foreach (var bet in _bets)
            {
                bool won = survivors.Exists(a => NIPeers.GetPeerId(a) == bet.BetOnPlayerId);
                if (won)
                {
                    int winAmount = (int)(bet.Amount * 1.5f);
                    // Give gold to bettor
                    InformationManager.DisplayMessage(new InformationMessage($"Bet won! {bet.PlayerId} gets {winAmount} gold!", Colors.Green));
                    // Add gold via PersistenceManager
                }
            }
            _bets.Clear();
        }

        public override void OnMissionTick(float dt)
        {
            if (_inKillcam && Mission.CurrentTime > _killcamEndTime)
            {
                _inKillcam = false;
                Mission.Current.SetTimeSpeed(1f);
            }
        }

        // Spectator camera is handled by Bannerlord's native SpectatorMissionBehavior.
        // OnPlayerKilled вызывает WaveManager.OnAgentRemoved (только для реальных смертей).
    }
}
