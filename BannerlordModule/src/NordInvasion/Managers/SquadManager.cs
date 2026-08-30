using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NordInvasion.Managers
{
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
