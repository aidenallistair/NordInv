using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using System.Collections.Generic;

namespace NordInvasion.Behaviors
{
    // Mechanic 16: Commander Mode
    public class CommanderBehavior : MissionBehavior
    {
        public Agent CommanderAgent = null;
        public List<CommanderMarker> Markers = new List<CommanderMarker>();

        public class CommanderMarker
        {
            public Vec3 Position;
            public string Type; // Attack, Build, Retreat
            public float TimeCreated;
        }

        public bool TrySetCommander(Agent agent)
        {
            if (CommanderAgent != null) return false;
            // Check rank - need Veteran title or vote
            CommanderAgent = agent;
            InformationManager.DisplayMessage(new InformationMessage($"{agent.Name} is now Commander! Press R for top-down view", Colors.Cyan));
            return true;
        }

        public void PlaceMarker(Vec3 pos, string type)
        {
            if (CommanderAgent == null) return;
            Markers.Add(new CommanderMarker { Position = pos, Type = type, TimeCreated = Mission.CurrentTime });
            // Spawn visual entity
            // Mission.Current.Scene.CreateGameEntity(pos).AddComponent(...)

            // Notify players
            foreach (var player in Mission.Current.PlayerTeam.ActiveAgents)
            {
                if (player != CommanderAgent)
                    InformationManager.DisplayMessage(new InformationMessage($"Commander: {type} at {pos}! +10% XP if you follow", Colors.Yellow));
            }
        }

        public bool IsNearMarker(Agent agent, string type, float radius = 5f)
        {
            foreach (var m in Markers)
            {
                if (m.Type == type && agent.Position.Distance(m.Position) < radius)
                    return true;
            }
            return false;
        }

        public override void OnMissionTick(float dt)
        {
            // Clear old markers after 60 sec
            Markers.RemoveAll(m => Mission.CurrentTime - m.TimeCreated > 60f);
        }
    }
}
