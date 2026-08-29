using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using System.Linq;
using TaleWorlds.Core;

namespace NordInvasion.Managers
{
    // Mechanic 2: Modular Fort + 14 Destructible
    public class FortressBuildManager : MissionBehavior
    {
        // Foundation/Wall - базовые, остальное открывают чертежи из магазина (Mechanic 2/18/23)
        public enum BuildType
        {
            Foundation, Wall, Door, Stakes, OilCauldron, Brazier, SpikeTrap, ShieldWall,
            Ballista, Catapult, RockTrap, LogTrap, OilDitch
        }

        // Максимум построек на забег (ограничение на кол-во сущностей)
        public const int MaxBuildings = 40;
        public int BuiltCount = 0;
        private Agent _builder;
        private readonly List<GameEntity> _placed = new List<GameEntity>();

        /// <summary>Постройки, поставленные в этом забеге (для ремонта и статистики).</summary>
        public IReadOnlyList<GameEntity> Placed => _placed;

        public bool TryPlace(BuildType type, Agent builder)
        {
            _builder = builder;
            var goldComp = builder.GetComponent<PersistenceManager.PlayerGoldComponent>();
            if (goldComp == null) return false;

            if (BuiltCount >= MaxBuildings)
            {
                InformationManager.DisplayMessage(new InformationMessage("Fort limit reached! Destroy some barricades first", Colors.Red));
                return false;
            }

            // Механика 2: продвинутые постройки открываются чертежами (покупка в магазине -> бэкенд)
            var needBlueprint = Models.ShopCatalog.BlueprintFor(type);
            if (!string.IsNullOrEmpty(needBlueprint)
                && (goldComp.Blueprints == null || !goldComp.Blueprints.Contains(needBlueprint)))
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Blueprint '{needBlueprint}' required! Buy it in the shop (N)", Colors.Red));
                return false;
            }

            var supply = Mission.GetMissionBehavior<Behaviors.SupplyBehavior>();
            var pos = builder.Position + builder.LookDirection * 2f;
            float yaw = builder.LookDirection.ToAngle2D();

            // Если мы на dedicated сервере - шлём запрос через сеть, а не ставим локально
            if (GameNetwork.IsClient)
            {
                var msg = new Multiplayer.RequestBuildMessage(type, pos, yaw);
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(msg);
                GameNetwork.EndModuleEventAsClient();
                InformationManager.DisplayMessage(new InformationMessage($"Build request sent: {type}", Colors.Yellow));
                return true;
            }

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
                    if (!Spend(goldComp, 6, 0, "Need 6 wood for shield wall!")) return false;
                    Place("ni_shield_wall", Machines.PropSpawner.FallbackWall, pos, yaw, new Machines.BarricadeDestructible());
                    break;
                case BuildType.SpikeTrap:
                    if (!Spend(goldComp, 2, 0, "Need 2 wood for spike trap!")) return false;
                    Place("ni_spike_trap", Machines.PropSpawner.FallbackFence, pos, yaw, new Machines.StakesTrap());
                    break;

                // Механика 18: осадные орудия игроками (чертежи из магазина)
                case BuildType.Ballista:
                    if (!Spend(goldComp, 8, 6, "Need 8 wood + 6 metal for ballista!")) return false;
                    Place("ni_ballista", Machines.PropSpawner.FallbackWood, pos, yaw, new Machines.BallistaUsable());
                    break;
                case BuildType.Catapult:
                    if (!Spend(goldComp, 12, 10, "Need 12 wood + 10 metal for catapult!")) return false;
                    Place("ni_catapult", Machines.PropSpawner.FallbackWood, pos, yaw, new Machines.CatapultUsable());
                    break;

                // Механика 23: ловушки окружения
                case BuildType.RockTrap:
                    if (!Spend(goldComp, 3, 4, "Need 3 wood + 4 metal for rock trap!")) return false;
                    Place("ni_rock_trap", Machines.PropSpawner.FallbackBarrel, pos, yaw, new Machines.RockTrapUsable());
                    break;
                case BuildType.LogTrap:
                    if (!Spend(goldComp, 6, 0, "Need 6 wood for log trap!")) return false;
                    Place("ni_log_trap", Machines.PropSpawner.FallbackWood, pos, yaw, new Machines.LogTrapUsable());
                    break;
                case BuildType.OilDitch:
                    if (!Spend(goldComp, 4, 5, "Need 4 wood + 5 metal for oil ditch!")) return false;
                    Place("ni_oil_ditch", Machines.PropSpawner.FallbackBarrel, pos, yaw, new Machines.OilDitchUsable());
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
            needWood = System.Math.Max(0, needWood - comp.Wood);
            needMetal = System.Math.Max(0, needMetal - comp.Metal);

            var supply = Mission.GetMissionBehavior<Behaviors.SupplyBehavior>();
            if (needWood > 0 || needMetal > 0)
            {
                if (supply == null || supply.WoodStock < needWood || supply.MetalStock < needMetal)
                {
                    InformationManager.DisplayMessage(new InformationMessage(msg, Colors.Red));
                    return false;
                }
                supply.TrySpendWood(needWood);
                supply.MetalStock = System.Math.Max(0, supply.MetalStock - needMetal);
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
            _placed.Add(entity);
            InformationManager.DisplayMessage(new InformationMessage($"Placed {entity.Name}! Engineer can repair with hammer", Colors.Green));

            // ранг Master Engineer: 100 построек (бэкенд считает)
            Mission.GetMissionBehavior<PersistenceManager>()?.OnBuildPlaced(_builder);

            // MP: если мы сервер - бродкастим всем клиентам
            if (GameNetwork.IsServer)
            {
                var msg = new Multiplayer.BuildPlacedMessage(propId, fallbackId, pos, yaw);
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(msg);
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }
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

        /// <summary>
        /// Ремонт ближайшей постройки (магазин: "Repair Kit", перки инженера, ability медика-инженера).
        /// Возвращает false, если ставить/чинить нечего.
        /// </summary>
        public bool RepairNearest(Agent agent, int hitPoints, float radius = 25f)
        {
            var target = FindNearestStructure(agent.Position, radius);
            if (target == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("No barricade in range to repair", Colors.Yellow));
                return false;
            }
            var destructible = target.GetFirstScriptOfType<DestructibleComponent>();
            if (destructible == null) return false;

            destructible.SetHitPoints(System.Math.Min(destructible.HitPoints + hitPoints, destructible.MaxHitPoints));
            InformationManager.DisplayMessage(new InformationMessage($"Repaired {target.Name} +{hitPoints} HP", Colors.Cyan));
            return true;
        }

        /// <summary>Ближайшая постройка форта из поставленных в этом забеге.</summary>
        public GameEntity FindNearestStructure(Vec3 from, float radius = 25f)
        {
            GameEntity best = null;
            float bestDist = radius * radius;
            for (int i = _placed.Count - 1; i >= 0; i--)
            {
                var e = _placed[i];
                if (e == null) { _placed.RemoveAt(i); continue; }
                float d2;
                try
                {
                    var delta = e.GlobalPosition - from;
                    d2 = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
                }
                catch { _placed.RemoveAt(i); continue; }   // сущность уже удалена из мира
                if (d2 < bestDist) { bestDist = d2; best = e; }
            }
            return best;
        }

        /// <summary>Директор: relief-ящик с боеприпасами при низком стрессе.</summary>
        public void SpawnAmmoBox(Vec3 pos)
        {
            var entity = Machines.PropSpawner.SpawnWithFallback(Mission.Current.Scene, "ni_ammo_box", Machines.PropSpawner.FallbackChest, pos);
            if (entity != null)
                InformationManager.DisplayMessage(new InformationMessage("Ammo box spawned! Refill arrows", Colors.Yellow));
        }

        // ===== Multiplayer support (DedicatedCustomServer) =====

        /// <summary>
        /// Сервер-авторитетная постройка для MP. Вызывается на сервере когда клиент прислал RequestBuildMessage.
        /// </summary>
        public bool TryPlaceMP(BuildType type, Vec3 pos, float yaw, NetworkCommunicator peer)
        {
            if (peer == null) return false;
            var rep = peer.GetComponent<Multiplayer.NIMissionRepresentative>();
            var agent = peer.ControlledAgent;
            if (rep == null) return false;

            if (BuiltCount >= MaxBuildings) return false;

            // Проверка чертежей через goldComp если есть
            if (agent != null)
            {
                var goldComp = agent.GetComponent<PersistenceManager.PlayerGoldComponent>();
                if (goldComp != null)
                {
                    var needBlueprint = Models.ShopCatalog.BlueprintFor(type);
                    if (!string.IsNullOrEmpty(needBlueprint) && !goldComp.Blueprints.Contains(needBlueprint))
                        return false;
                }
            }

            if (!SpendMP(rep, type)) return false;

            string propId = GetPropIdFor(type);
            string fallback = GetFallbackFor(type);
            var component = GetComponentFor(type);

            var entity = Machines.PropSpawner.SpawnWithFallback(Mission.Current.Scene, propId, fallback, pos, yaw);
            if (entity == null) return false;
            if (component != null) entity.AddComponent(component);

            BuiltCount++;
            _placed.Add(entity);
            _builder = agent;

            if (GameNetwork.IsServer)
            {
                var msg = new Multiplayer.BuildPlacedMessage(propId, fallback, pos, yaw);
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(msg);
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }

            if (agent != null) Mission.GetMissionBehavior<PersistenceManager>()?.OnBuildPlaced(agent);
            return true;
        }

        bool SpendMP(Multiplayer.NIMissionRepresentative rep, BuildType type)
        {
            int wood = 0, metal = 0;
            switch (type)
            {
                case BuildType.Foundation: wood = 5; break;
                case BuildType.Wall: wood = 3; break;
                case BuildType.Door: wood = 5; metal = 2; break;
                case BuildType.Stakes: wood = 4; break;
                case BuildType.OilCauldron: wood = 10; metal = 5; break;
                case BuildType.Brazier: wood = 2; break;
                case BuildType.ShieldWall: wood = 6; break;
                case BuildType.SpikeTrap: wood = 2; break;
                case BuildType.Ballista: wood = 8; metal = 6; break;
                case BuildType.Catapult: wood = 12; metal = 10; break;
                case BuildType.RockTrap: wood = 3; metal = 4; break;
                case BuildType.LogTrap: wood = 6; break;
                case BuildType.OilDitch: wood = 4; metal = 5; break;
            }

            if (rep.Wood < wood || rep.Metal < metal)
            {
                var supply = Mission.GetMissionBehavior<Behaviors.SupplyBehavior>();
                int needWood = System.Math.Max(0, wood - rep.Wood);
                int needMetal = System.Math.Max(0, metal - rep.Metal);
                if (supply == null || supply.WoodStock < needWood || supply.MetalStock < needMetal)
                    return false;
                supply.TrySpendWood(needWood);
                supply.MetalStock = System.Math.Max(0, supply.MetalStock - needMetal);
            }

            rep.Wood -= System.Math.Min(rep.Wood, wood);
            rep.Metal -= System.Math.Min(rep.Metal, metal);
            return true;
        }

        string GetPropIdFor(BuildType type)
        {
            switch (type)
            {
                case BuildType.Foundation: return "ni_foundation_wood";
                case BuildType.Wall: return "ni_wall_wood";
                case BuildType.Door: return "ni_wall_door";
                case BuildType.Stakes: return "ni_stakes";
                case BuildType.OilCauldron: return "ni_oil_cauldron";
                case BuildType.Brazier: return "ni_brazier";
                case BuildType.ShieldWall: return "ni_shield_wall";
                case BuildType.SpikeTrap: return "ni_spike_trap";
                case BuildType.Ballista: return "ni_ballista";
                case BuildType.Catapult: return "ni_catapult";
                case BuildType.RockTrap: return "ni_rock_trap";
                case BuildType.LogTrap: return "ni_log_trap";
                case BuildType.OilDitch: return "ni_oil_ditch";
                default: return "ni_wall_wood";
            }
        }

        string GetFallbackFor(BuildType type)
        {
            switch (type)
            {
                case BuildType.Foundation:
                case BuildType.Ballista:
                case BuildType.Catapult:
                case BuildType.LogTrap: return Machines.PropSpawner.FallbackWood;
                case BuildType.Wall:
                case BuildType.Door:
                case BuildType.ShieldWall: return Machines.PropSpawner.FallbackWall;
                case BuildType.Stakes:
                case BuildType.SpikeTrap: return Machines.PropSpawner.FallbackFence;
                case BuildType.OilCauldron:
                case BuildType.RockTrap:
                case BuildType.OilDitch: return Machines.PropSpawner.FallbackBarrel;
                case BuildType.Brazier: return Machines.PropSpawner.FallbackTorch;
                default: return Machines.PropSpawner.FallbackWall;
            }
        }

        ManagedScript GetComponentFor(BuildType type)
        {
            switch (type)
            {
                case BuildType.Stakes:
                case BuildType.SpikeTrap: return new Machines.StakesTrap();
                case BuildType.Brazier: return new Machines.BrazierUsable();
                case BuildType.Ballista: return new Machines.BallistaUsable();
                case BuildType.Catapult: return new Machines.CatapultUsable();
                case BuildType.RockTrap: return new Machines.RockTrapUsable();
                case BuildType.LogTrap: return new Machines.LogTrapUsable();
                case BuildType.OilDitch: return new Machines.OilDitchUsable();
                default: return new Machines.BarricadeDestructible();
            }
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
