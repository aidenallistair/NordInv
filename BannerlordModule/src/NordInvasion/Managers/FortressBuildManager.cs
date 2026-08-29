using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using System.Linq;

namespace NordInvasion.Managers
{
    // Mechanic 2: Modular Fort + 14 Destructible
    public class FortressBuildManager : MissionBehavior
    {
        public enum BuildType { Foundation, Wall, Door, Stakes, OilCauldron, Brazier, SpikeTrap, ShieldWall }

        public bool TryPlace(BuildType type, Agent builder)
        {
            var goldComp = builder.GetComponent<PersistenceManager.PlayerGoldComponent>();
            if (goldComp == null) return false;

            var supply = Mission.GetMissionBehavior<Behaviors.SupplyBehavior>();
            // Check warehouse stock first
            bool hasWarehouse = supply != null;

            switch (type)
            {
                case BuildType.Foundation:
                    if (goldComp.Wood < 5) { InformationManager.DisplayMessage(new InformationMessage("Need 5 wood!", Colors.Red)); return false; }
                    if (hasWarehouse && !supply.TrySpendWood(5)) { InformationManager.DisplayMessage(new InformationMessage("Warehouse empty! Need caravan", Colors.Red)); return false; }
                    goldComp.Wood -= 5;
                    SpawnProp("ni_foundation_wood", builder.Position + builder.LookDirection * 2f);
                    break;
                case BuildType.Wall:
                    if (goldComp.Wood < 3) { InformationManager.DisplayMessage(new InformationMessage("Need 3 wood!", Colors.Red)); return false; }
                    if (hasWarehouse && !supply.TrySpendWood(3)) return false;
                    goldComp.Wood -= 3;
                    SpawnProp("ni_wall_wood", builder.Position + builder.LookDirection * 2f);
                    break;
                case BuildType.Door:
                    if (goldComp.Wood < 5 || goldComp.Metal < 2) { InformationManager.DisplayMessage(new InformationMessage("Need 5 wood + 2 metal!", Colors.Red)); return false; }
                    goldComp.Wood -= 5; goldComp.Metal -= 2;
                    SpawnProp("ni_wall_door", builder.Position + builder.LookDirection * 2f);
                    break;
                case BuildType.Stakes: // Anti-cav Mechanic 7
                    if (goldComp.Wood < 4) { InformationManager.DisplayMessage(new InformationMessage("Need 4 wood for stakes!", Colors.Red)); return false; }
                    if (hasWarehouse && !supply.TrySpendWood(4)) return false;
                    goldComp.Wood -= 4;
                    SpawnProp("ni_stakes", builder.Position + builder.LookDirection * 2f);
                    break;
                case BuildType.OilCauldron:
                    if (goldComp.Wood < 10 || goldComp.Metal < 5) { InformationManager.DisplayMessage(new InformationMessage("Need 10 wood + 5 metal for oil cauldron!", Colors.Red)); return false; }
                    goldComp.Wood -= 10; goldComp.Metal -= 5;
                    SpawnProp("ni_oil_cauldron", builder.Position + builder.LookDirection * 2f);
                    break;
                case BuildType.Brazier:
                    if (goldComp.Wood < 2) return false;
                    goldComp.Wood -= 2;
                    SpawnProp("ni_brazier", builder.Position + builder.LookDirection * 2f);
                    break;
                case BuildType.ShieldWall:
                    if (goldComp.Wood < 6) return false;
                    goldComp.Wood -= 6;
                    SpawnProp("ni_shield_wall", builder.Position + builder.LookDirection * 2f);
                    break;
            }
            return true;
        }

        void SpawnProp(string propId, Vec3 pos)
        {
            InformationManager.DisplayMessage(new InformationMessage($"Placed {propId}! Engineer can repair with hammer", Colors.Green));
            // Real spawn: Mission.Current.Scene.CreateGameEntity with DestructibleComponent
        }

        public void Repair(Agent engineer, GameEntity propEntity)
        {
            var destructible = propEntity.GetFirstScriptOfType<DestructibleComponent>();
            if (destructible != null)
            {
                int repairAmount = 20;
                var perkComp = engineer.GetComponent<Components.PerkAgentComponent>();
                if (perkComp != null && perkComp.BarricadeMod > 1f) repairAmount = (int)(repairAmount * perkComp.BarricadeMod);

                destructible.SetHitPoints((int)(destructible.HitPoints + repairAmount));
                InformationManager.DisplayMessage(new InformationMessage($"Repaired {propEntity.Name} +{repairAmount} HP", Colors.Cyan));
            }
        }

        public void SpawnAmmoBox(Vec3 pos)
        {
            InformationManager.DisplayMessage(new InformationMessage("Ammo box spawned! Refill arrows", Colors.Yellow));
            // Spawn ni_armory_chest
        }
    }

    public class ScavengeManager : MissionBehavior
    {
        // Mechanic 9
        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectedAgent.Team != null && affectedAgent.Team.Side == BattleSideEnum.Attacker && affectorAgent != null && affectorAgent.Team == Mission.PlayerTeam)
            {
                var comp = affectorAgent.GetComponent<PersistenceManager.PlayerGoldComponent>();
                if (comp == null) return;

                // 20% metal from veteran+
                if (affectedAgent.Character != null && (affectedAgent.Character.StringId.Contains("veteran") || affectedAgent.Character.StringId.Contains("huscarl") || affectedAgent.Character.StringId.Contains("jarl")))
                {
                    if (MBRandom.RandomInt(100) < 20)
                    {
                        comp.AddMetal(1);
                        InformationManager.DisplayMessage(new InformationMessage("+1 Scrap Metal from veteran!", Colors.Yellow));
                    }
                }
                // 30% wood from any nord
                if (MBRandom.RandomInt(100) < 30)
                {
                    comp.AddWood(1);
                }
            }
        }
    }

    public class SquadManager : MissionBehavior
    {
        // Mechanic 11: Squads with formations
        public void SpawnShieldWallSquad(int entryPoint)
        {
            var leaderTroop = Game.Current.ObjectManager.GetObject<TaleWorlds.Core.CharacterObject>("ni_nord_shield_leader");
            var huscarl = Game.Current.ObjectManager.GetObject<TaleWorlds.Core.CharacterObject>("ni_nord_huscarl");
            var archer = Game.Current.ObjectManager.GetObject<TaleWorlds.Core.CharacterObject>("ni_nord_archer");
            if (leaderTroop == null || huscarl == null) return;

            var entry = Mission.Current.GetEntryPoint(entryPoint);
            if (entry == null) return;

            var pos = entry.Position;
            var team = Mission.Current.Teams.FirstOrDefault(t => t.Side == BattleSideEnum.Attacker);
            if (team == null) return;

            // Leader
            var leader = Mission.Current.SpawnAgent(new AgentBuildData(leaderTroop).Team(team).InitialPosition(pos));
            if (leader != null)
            {
                // Set as formation captain
                var formation = team.GetFormation(TaleWorlds.Core.FormationClass.Infantry);
                // formation.Captain = leader; // Bannerlord API
            }

            // 3 huscarls front line shieldwall
            for (int i = 0; i < 3; i++)
            {
                var p = pos + new Vec3(i * 1.5f, 1f, 0);
                Mission.Current.SpawnAgent(new AgentBuildData(huscarl).Team(team).InitialPosition(p));
            }

            // 3 archers behind
            for (int i = 0; i < 3; i++)
            {
                if (archer == null) continue;
                var p = pos + new Vec3(i * 1.5f, -3f, 0);
                Mission.Current.SpawnAgent(new AgentBuildData(archer).Team(team).InitialPosition(p));
            }

            InformationManager.DisplayMessage(new InformationMessage($"Shieldwall squad spawned at entry {entryPoint}!", Colors.Yellow));
        }

        public void SpawnBerserkWedge(int entryPoint)
        {
            var berserker = Game.Current.ObjectManager.GetObject<TaleWorlds.Core.CharacterObject>("ni_nord_berserker");
            if (berserker == null) return;
            var entry = Mission.Current.GetEntryPoint(entryPoint);
            if (entry == null) return;
            var team = Mission.Current.Teams.FirstOrDefault(t => t.Side == BattleSideEnum.Attacker);
            // Wedge formation - 5 berserkers
            for (int i = 0; i < 5; i++)
                Mission.Current.SpawnAgent(new AgentBuildData(berserker).Team(team).InitialPosition(entry.Position + new Vec3(i, i, 0)));
        }
    }
}
