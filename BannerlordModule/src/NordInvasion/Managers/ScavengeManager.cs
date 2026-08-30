using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NordInvasion.Managers
{
    public class ScavengeManager : MissionBehavior
    {
        // Mechanic 9
        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectedAgent.Team != null && affectedAgent.Team.Side == BattleSideEnum.Attacker
                && affectorAgent != null && Mission.PlayerTeam != null && affectorAgent.Team == Mission.PlayerTeam)
            {
                var comp = affectorAgent.GetComponent<PersistenceManager.PlayerGoldComponent>();
                if (comp == null) return;

                // Перк Scavenger: +50% ресурсов
                float mod = 1f;
                var perkComp = affectorAgent.GetComponent<Components.PerkAgentComponent>();
                if (perkComp != null && perkComp.HasPerk(24)) mod = 1.5f;

                // 20% metal from veteran+
                if (affectedAgent.Character != null && (affectedAgent.Character.StringId.Contains("veteran")
                    || affectedAgent.Character.StringId.Contains("huscarl")
                    || affectedAgent.Character.StringId.Contains("jarl")))
                {
                    if (MBRandom.RandomInt(100) < (int)(20 * mod))
                    {
                        comp.AddMetal(1);
                        InformationManager.DisplayMessage(new InformationMessage("+1 Scrap Metal from veteran!", Colors.Yellow));
                    }
                }
                // 30% wood from any nord
                if (MBRandom.RandomInt(100) < (int)(30 * mod))
                {
                    comp.AddWood(1);
                }
            }
        }
    }
}
