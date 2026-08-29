using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using System.Linq;
using TaleWorlds.Core;

namespace NordInvasion.Managers
{
    // Mechanic 2: Modular Fort + 14 Destructible
    public class FortressBuildManager : MissionBehavior
    {
        public enum BuildType { Foundation, Wall, Door, Stakes, OilCauldron, Brazier, SpikeTrap, ShieldWall }

        // Максимум построек на забег (ограничение на кол-во сущностей)
        public const int MaxBuildings = 40;
        public int BuiltCount = 0;

        public bool TryPlace(BuildType type, Agent builder)
        {
            var goldComp = builder.GetComponent<PersistenceManager.PlayerGoldComponent>();
            if (goldComp == null) return false;

            if (BuiltCount >= MaxBuildings)
            {
                InformationManager.DisplayMessage(new InformationMessage("Fort limit reached! Destroy some barricades first", Colors.Red));
                return false;
            }

            var supply = Mission.GetMissionBehavior<Behaviors.SupplyBehavior>();
            var pos = builder.Position + builder.LookDirection * 2f;
            float yaw = builder.LookDirection.ToAngle2D();

            switch (type)
            {
                case BuildType.Foundation:
                    if (!Spend(goldComp, 5, 0, "Need 5 wood!")) return false;
                    Place("ni_foundation_wood", Machines.PropSpawner.FallbackWood, pos, yaw, new Machines.BarricadeDestructible());
                    break;
                case BuildType.Wall:
                    if (!Spend(goldComp, 3, 0, "Need 3 wood!")) return false;
                    Place("ni_wall_wood", Machines.PropSpawner.FallbackWall, pos, yaw, new Machines.BarricadeDestructible());
                    break;
                case BuildType.Door:
                    if (!Spend(goldComp, 5, 2, "Need 5 wood + 2 metal!")) return false;
                    Place("ni_wall_door", Machines.PropSpawner.FallbackWall, pos, yaw, new Machines.BarricadeDestructible());
                    break;
                case BuildType.Stakes: // Anti-cav Mechanic 7
                    if (!Spend(goldComp, 4, 0, "Need 4 wood for stakes!")) return false;
                    Place("ni_stakes", Machines.PropSpawner.FallbackFence, pos, yaw, new Machines.StakesTrap());
                    break;
                case BuildType.OilCauldron:
                    if (!Spend(goldComp, 10, 5, "Need 10 wood + 5 metal for oil cauldron!")) return false;
                    Place("ni_oil_cauldron", Machines.PropSpawner.FallbackBarrel, pos, yaw, new Machines.BarricadeDestructible());
                    break;
                case BuildType.Brazier:
                    if (goldComp.Wood < 2) { InformationManager.DisplayMessage(new InformationMessage("Need 2 wood for brazier!", Colors.Red)); return false; }
                    goldComp.Wood -= 2;
                    Place("ni_brazier", Machines.PropSpawner.FallbackTorch, pos, yaw, new Machines.BrazierUsable());
                    break;
                case BuildType.ShieldWall:
                    if (goldComp.Wood < 6) { InformationManager.DisplayMessage(new InformationMessage("Need 6 wood for shield wall!", Colors.Red)); return false; }
                    goldComp.Wood -= 6;
                    Place("ni_shield_wall", Machines.PropSpawner.FallbackWall, pos, yaw, new Machines.BarricadeDestructible());
                    break;
            }
            return true;
        }

        /// <summary>
        /// Экономика строительства: сначала личные ресурсы игрока (скраутинг),
        /// не хватает - добираем с общего склада (Warehouse/караван).
        /// </summary>
        bool Spend(PersistenceManager.PlayerGoldComponent comp, int wood, int metal, string msg)
        {
            int needWood = wood, needMetal = metal;
            needWood = Math.Max(0, needWood - comp.Wood);
            needMetal = Math.Max(0, needMetal - comp.Metal);

            var supply = Mission.GetMissionBehavior<Behaviors.SupplyBehavior>();
            if (needWood > 0 || needMetal > 0)
            {
                if (supply == null || supply.WoodStock < needWood || supply.MetalStock < needMetal)
                {
                    InformationManager.DisplayMessage(new InformationMessage(msg, Colors.Red));
                    return false;
                }
                supply.TrySpendWood(needWood);
                supply.MetalStock = Math.Max(0, supply.MetalStock - needMetal);
            }

            comp.Wood -= (wood - needWood);
            comp.Metal -= (metal - needMetal);
            return true;
        }

        void Place(string propId, string fallbackId, Vec3 pos, float yaw, ManagedScript component)
        {
            var entity = Machines.PropSpawner.SpawnWithFallback(Mission.Current.Scene, propId, fallbackId, pos, yaw);
            if (entity == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Could not spawn {propId} (asset missing?)", Colors.Red));
                return;
            }
            if (component != null)
                entity.AddComponent(component);

            BuiltCount++;
            InformationManager.DisplayMessage(new InformationMessage($"Placed {entity.Name}! Engineer can repair with hammer", Colors.Green));
        }

        /// <summary>Чинит пропс с DestructibleComponent (+20 HP * перк инженера).</summary>
        public void Repair(Agent engineer, GameEntity propEntity)
        {
            if (propEntity == null) return;
            var destructible = propEntity.GetFirstScriptOfType<DestructibleComponent>();
            if (destructible == null) return;

            int repairAmount = 20;
            var perkComp = engineer.GetComponent<Components.PerkAgentComponent>();
            if (perkComp != null && perkComp.BarricadeMod > 1f)
                repairAmount = (int)(repairAmount * perkComp.BarricadeMod);

            destructible.SetHitPoints(destructible.HitPoints + repairAmount);
            InformationManager.DisplayMessage(new InformationMessage($"Repaired {propEntity.Name} +{repairAmount} HP", Colors.Cyan));
        }

        /// <summary>Директор: relief-ящик с боеприпасами при низком стрессе.</summary>
        public void SpawnAmmoBox(Vec3 pos)
        {
            var entity = Machines.PropSpawner.SpawnWithFallback(Mission.Current.Scene, "ni_ammo_box", Machines.PropSpawner.FallbackChest, pos);
            if (entity != null)
                InformationManager.DisplayMessage(new InformationMessage("Ammo box spawned! Refill arrows", Colors.Yellow));
        }
    }

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

    public class SquadManager : MissionBehavior
    {
        // Mechanic 11: Squads with formations
        public void SpawnShieldWallSquad(int entryPoint)
        {
            var leaderTroop = Game.Current.ObjectManager.GetObject<CharacterObject>("ni_nord_shield_leader");
            var huscarl = Game.Current.ObjectManager.GetObject<CharacterObject>("ni_nord_huscarl");
            var archer = Game.Current.ObjectManager.GetObject<CharacterObject>("ni_nord_archer");
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
                // Лидер отряда (captain formation) - через нативный Formation API
                var formation = team.GetFormation(FormationClass.Infantry);
                if (formation != null) formation.Captain = leader;
            }

            // 3 huscarls front line shieldwall
            for (int i = 0; i < 3; i++)
            {
                var p = pos + new Vec3(i * 1.5f, 1f, 0f);
                Mission.Current.SpawnAgent(new AgentBuildData(huscarl).Team(team).InitialPosition(p));
            }

            // 3 archers behind
            for (int i = 0; i < 3; i++)
            {
                if (archer == null) continue;
                var p = pos + new Vec3(i * 1.5f, -3f, 0f);
                Mission.Current.SpawnAgent(new AgentBuildData(archer).Team(team).InitialPosition(p));
            }

            // Регистрация в морали
            Mission.GetMissionBehavior<Behaviors.MoraleBehavior>()?.RegisterSquad(entryPoint, null);

            InformationManager.DisplayMessage(new InformationMessage($"Shieldwall squad spawned at entry {entryPoint}!", Colors.Yellow));
        }

        public void SpawnBerserkWedge(int entryPoint)
        {
            var berserker = Game.Current.ObjectManager.GetObject<CharacterObject>("ni_nord_berserker");
            if (berserker == null) return;
            var entry = Mission.Current.GetEntryPoint(entryPoint);
            if (entry == null) return;
            var team = Mission.Current.Teams.FirstOrDefault(t => t.Side == BattleSideEnum.Attacker);
            if (team == null) return;
            // Wedge formation - 5 berserkers
            for (int i = 0; i < 5; i++)
                Mission.Current.SpawnAgent(new AgentBuildData(berserker).Team(team).InitialPosition(entry.Position + new Vec3(i, i, 0f)));
        }
    }
}
