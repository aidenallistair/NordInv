using System;
using System.Net.Http;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace NordInvasion.Managers
{
    // Mechanic 12: Persistence 2.0 + 15 Campaign
    public class PersistenceManager : MissionBehavior
    {
        private static HttpClient _http = new HttpClient();
        private string _backendUrl = "http://localhost:8000";

        public class PlayerGoldComponent : AgentComponent
        {
            public int Gold = 500;
            public int Wood = 0;
            public int Metal = 0;
            public int Kills = 0;
            public void AddGold(int amount) { Gold += amount; }
            public void AddMetal(int amount) { Metal += amount; }
            public void AddWood(int amount) { Wood += amount; }
        }

        public void OnKill(Agent killed, Agent killer, int wave)
        {
            if (killer == null) return;
            var comp = killer.GetComponent<PlayerGoldComponent>();
            if (comp == null) return;

            // Async backend call
            Task.Run(async () =>
            {
                try
                {
                    var content = new FormUrlEncodedContent(new[]
                    {
                        new System.Collections.Generic.KeyValuePair<string, string>("player_id", killer.MissionPeer?.Peer.Communicator.ToString() ?? "local"),
                        new System.Collections.Generic.KeyValuePair<string, string>("player_name", killer.Name.ToString()),
                        new System.Collections.Generic.KeyValuePair<string, string>("killed_troop", killed.Character.StringId),
                        new System.Collections.Generic.KeyValuePair<string, string>("gold_reward", "10"),
                        new System.Collections.Generic.KeyValuePair<string, string>("wave", wave.ToString()),
                    });
                    await _http.PostAsync($"{_backendUrl}/api/kill", content);
                }
                catch (Exception ex)
                {
                    Debug.Print($"Backend kill error: {ex.Message}");
                }
            });
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (agent.IsPlayerControlled || agent.Team == Mission.PlayerTeam)
            {
                agent.AddComponent(new PlayerGoldComponent(agent));
                agent.AddComponent(new Components.WoundStaminaComponent(agent));
                agent.AddComponent(new Components.PerkAgentComponent(agent));
            }
        }

        public async Task<PlayerData> LoginPlayer(string steamId, string name)
        {
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new System.Collections.Generic.KeyValuePair<string, string>("player_id", steamId),
                    new System.Collections.Generic.KeyValuePair<string, string>("steam_id", steamId),
                    new System.Collections.Generic.KeyValuePair<string, string>("player_name", name),
                });
                var resp = await _http.PostAsync($"{_backendUrl}/api/player/login", content);
                var json = await resp.Content.ReadAsStringAsync();
                // Parse json...
                return new PlayerData { Gold = 500 };
            }
            catch { return new PlayerData { Gold = 500 }; }
        }

        public class PlayerData
        {
            public int Gold;
            public int Wood;
            public int Metal;
            public string[] Blueprints;
        }
    }
}
