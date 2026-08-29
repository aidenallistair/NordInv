using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using System.Collections.Generic;

namespace NordInvasion.Managers
{
    // Mechanic 2: Modular Fort + 14 Destructible
    public class FortressBuildManager : MissionBehavior
    {
        public enum BuildType { Foundation, Wall, Door, Stakes, OilCauldron, Brazier, SpikeTrap }

        public bool TryPlace(BuildType type, Agent builder)
        {
            var goldComp = builder.GetComponent<PersistenceManager.PlayerGoldComponent>();
            if (goldComp == null) return false;

            // Check resources
            switch (type)
            {
                case BuildType.Foundation:
                    if (goldComp.Wood < 5) { InformationManager.DisplayMessage(new InformationMessage("Need 5 wood!", Colors.Red)); return false; }
                    goldComp.Wood -= 5;
                    SpawnProp("ni_foundation_wood", builder.Position + builder.LookDirection * 2f);
                    break;
                case BuildType.Wall:
                    if (goldComp.Wood < 3) return false;
                    goldComp.Wood -= 3;
                    SpawnProp("ni_wall_wood", builder.Position + builder.LookDirection * 2f);
                    break;
                case BuildType.Stakes: // Anti-cav Mechanic 7
                    if (goldComp.Wood < 4) return false;
                    goldComp.Wood -= 4;
                    SpawnProp("ni_stakes", builder.Position + builder.LookDirection * 2f);
                    break;
                case BuildType.OilCauldron:
                    if (goldComp.Wood < 10 || goldComp.Metal < 5) return false;
                    goldComp.Wood -= 10; goldComp.Metal -= 5;
                    SpawnProp("ni_oil_cauldron", builder.Position + builder.LookDirection * 2f);
                    break;
            }
            return true;
        }

        void SpawnProp(string propId, Vec3 pos)
        {
            // In Bannerlord, spawn via Mission.Scene.CreateGameEntity
            // Simplified
            // Mission.Current.Scene.CreateGameEntity(pos);
            InformationManager.DisplayMessage(new InformationMessage($"Placed {propId}!", Colors.Green));
        }

        public void Repair(Agent engineer, GameEntity propEntity)
        {
            // Check if engineer class
            var destructible = propEntity.GetFirstScriptOfType<DestructibleComponent>();
            if (destructible != null)
            {
                destructible.SetHitPoints((int)(destructible.HitPoints + 20));
            }
        }
    }

    public class ScavengeManager : MissionBehavior
    {
        // Mechanic 9
        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectedAgent.Team.Side == BattleSideEnum.Attacker && affectorAgent != null)
            {
                // 20% metal
                if (MBRandom.RandomInt(100) < 20)
                {
                    var comp = affectorAgent.GetComponent<PersistenceManager.PlayerGoldComponent>();
                    comp?.AddMetal(1);
                }
            }
        }
    }

    public class SquadManager : MissionBehavior
    {
        // Mechanic 11
        public void SpawnShieldWallSquad(int entryPoint)
        {
            var leaderTroop = Game.Current.ObjectManager.GetObject<TaleWorlds.Core.CharacterObject>("ni_nord_shield_leader");
            var huscarl = Game.Current.ObjectManager.GetObject<TaleWorlds.Core.CharacterObject>("ni_nord_huscarl");
            var archer = Game.Current.ObjectManager.GetObject<TaleWorlds.Core.CharacterObject>("ni_nord_archer");

            var pos = Mission.Current.GetEntryPoint(entryPoint).Position;
            var team = Mission.Current.Teams.First(t => t.Side == BattleSideEnum.Attacker);

            // Leader
            Mission.Current.SpawnAgent(new AgentBuildData(leaderTroop).Team(team).InitialPosition(pos));

            // 3 huscarls
            for (int i = 0; i < 3; i++)
                Mission.Current.SpawnAgent(new AgentBuildData(huscarl).Team(team).InitialPosition(pos + new Vec3(i, 0, 0)));

            // 3 archers behind
            for (int i = 0; i < 3; i++)
                Mission.Current.SpawnAgent(new AgentBuildData(archer).Team(team).InitialPosition(pos + new Vec3(0, -2, 0)));
        }
    }

    public class LootManager : MissionBehavior
    {
        // Mechanic 8
        public void SpawnLootBag(Vec3 position, int goldValue)
        {
            // Spawn usable machine loot bag
            InformationManager.DisplayMessage(new InformationMessage($"Boss loot spawned! {goldValue} gold - carry to treasury!", Colors.Gold));
            // Actual spawn: Mission.Current.Scene.CreateGameEntity...
        }
    }
}
