using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using System.Collections.Generic;
using System.Linq;

namespace NordInvasion.Behaviors
{
    // Mechanic 17: Morale and Panic
    public class MoraleBehavior : MissionBehavior
    {
        public class SquadMorale
        {
            public int SquadId;
            public float Morale = 100f;
            public List<Agent> Agents = new List<Agent>();
        }

        private List<SquadMorale> _nordSquads = new List<SquadMorale>();
        public float PlayerTeamMorale = 100f;

        public void RegisterSquad(int id, List<Agent> agents)
        {
            _nordSquads.Add(new SquadMorale { SquadId = id, Agents = agents, Morale = 100f });
        }

        public void OnAgentKilled(Agent killed, Agent killer)
        {
            // Find squad of killed
            var squad = _nordSquads.FirstOrDefault(s => s.Agents.Contains(killed));
            if (squad != null)
            {
                // Leader death - big morale drop
                if (killed.Character.StringId.Contains("leader"))
                    squad.Morale -= 40f;
                else
                    squad.Morale -= 15f;

                // Nearby allies lose morale
                foreach (var ally in squad.Agents.Where(a => a.IsActive() && a.Position.Distance(killed.Position) < 10f))
                {
                    // Panic check
                    if (squad.Morale < 30f)
                    {
                        // Flee
                        ally.SetTeam(Mission.Current.Teams.First(t => t.Side == BattleSideEnum.Attacker), true);
                        // Set flee behavior
                        // ally.SetBehavior(Flee)
                        InformationManager.DisplayMessage(new InformationMessage($"Nord squad {squad.SquadId} is panicking! Morale {squad.Morale}", Colors.Red));
                    }
                }
            }

            // Player morale
            if (killed.Team == Mission.PlayerTeam)
            {
                PlayerTeamMorale -= 10f;
                if (PlayerTeamMorale < 30f)
                {
                    // Camera shake, -20% damage for all players
                    foreach (var player in Mission.PlayerTeam.ActiveAgents)
                    {
                        player.SetMaximumSpeedFactor(0.8f);
                    }
                }
            }
        }

        public override void OnMissionTick(float dt)
        {
            // Regen player morale slowly if near banner
            if (PlayerTeamMorale < 100f)
                PlayerTeamMorale += dt * 2f;
        }
    }
}
