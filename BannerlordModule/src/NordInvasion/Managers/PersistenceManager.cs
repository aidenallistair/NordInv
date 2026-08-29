using System;
using System.Net.Http;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using System.Collections.Generic;

namespace NordInvasion.Managers
{
    // Mechanic 12: Persistence 2.0 + 15 Campaign + 24 Meta + 26 Ranks
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
            public int Deaths = 0;
            public List<int> Perks = new List<int>();
            public List<string> Blueprints = new List<string>();
            public List<string> Titles = new List<string>();

            public PlayerGoldComponent(Agent agent) : base(agent) { }

            public void AddGold(int amount) { Gold += amount; if (Gold < 0) Gold = 0; }
            public void AddMetal(int amount) { Metal += amount; }
            public void AddWood(int amount) { Wood += amount; }
            public bool HasPerk(int perkId) => Perks.Contains(perkId);
            public void AddPerk(int perkId) { if (!Perks.Contains(perkId)) Perks.Add(perkId); }
        }

        public void OnKill(Agent killed, Agent killer, int wave)
        {
            if (killer == null) return;
            var comp = killer.GetComponent<PlayerGoldComponent>();
            if (comp == null) return;

            Task.Run(async () =>
            {
                try
                {
                    var content = new FormUrlEncodedContent(new[]
                    {
                        new System.Collections.Generic.KeyValuePair<string, string>("player_id", killer.MissionPeer?.Peer.Communicator.ToString() ?? "local"),
                        new System.Collections.Generic.KeyValuePair<string, string>("player_name", killer.Name.ToString()),
                        new System.Collections.Generic.KeyValuePair<string, string>("killed_troop", killed.Character?.StringId ?? "unknown"),
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

        public void OnCampaignWin()
        {
            Task.Run(async () =>
            {
                try
                {
                    var content = new FormUrlEncodedContent(new[]
                    {
                        new System.Collections.Generic.KeyValuePair<string, string>("village_id", "0"),
                        new System.Collections.Generic.KeyValuePair<string, string>("won", "true"),
                        new System.Collections.Generic.KeyValuePair<string, string>("players", ""),
                        new System.Collections.Generic.KeyValuePair<string, string>("wave_reached", "25"),
                    });
                    await _http.PostAsync($"{_backendUrl}/api/campaign/battle", content);
                }
                catch { }
            });
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);
            if (agent.IsPlayerControlled || (Mission.PlayerTeam != null && agent.Team == Mission.PlayerTeam))
            {
                if (agent.GetComponent<PlayerGoldComponent>() == null)
                    agent.AddComponent(new PlayerGoldComponent(agent));
                if (agent.GetComponent<Components.WoundStaminaComponent>() == null)
                    agent.AddComponent(new Components.WoundStaminaComponent(agent));
                if (agent.GetComponent<Components.PerkAgentComponent>() == null)
                    agent.AddComponent(new Components.PerkAgentComponent(agent));
                if (agent.GetComponent<Components.ElementalWeaponComponent>() == null)
                    agent.AddComponent(new Components.ElementalWeaponComponent(agent, Components.ElementalType.None));
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
                return new PlayerData { Gold = 500, Blueprints = new string[0] };
            }
            catch { return new PlayerData { Gold = 500, Blueprints = new string[0] }; }
        }

        public class PlayerData
        {
            public int Gold;
            public int Wood;
            public int Metal;
            public string[] Blueprints;
            public string[] Titles;
            public int Level;
        }
    }

    // Perk Manager Mechanic 1
    public class PerkManager : MissionBehavior
    {
        private Random _rand = new Random();

        public void ShowChoiceToAll()
        {
            foreach (var agent in Mission.PlayerTeam.ActiveAgents)
            {
                ShowChoice(agent);
            }
        }

        public void ShowChoice(Agent agent)
        {
            var perks = Models.PerkDatabase.GetRandomThree(_rand);
            // Open Gauntlet UI NI_PerkChoice_VM with 3 perks
            InformationManager.DisplayMessage(new InformationMessage($"Perk choice: {perks[0].Name} / {perks[1].Name} / {perks[2].Name} - Press 1-3", Colors.Gold));
            // For MVP, auto-apply first perk
            ApplyPerk(agent, perks[0].Id);
        }

        public void ApplyPerk(Agent agent, int perkId)
        {
            var goldComp = agent.GetComponent<PersistenceManager.PlayerGoldComponent>();
            goldComp?.AddPerk(perkId);

            var perkComp = agent.GetComponent<Components.PerkAgentComponent>();
            perkComp?.ApplyPerk(perkId);

            var def = Models.PerkDatabase.GetById(perkId);
            if (def != null)
                InformationManager.DisplayMessage(new InformationMessage($"Perk applied: {def.Name} - {def.Desc}", Colors.Green));
        }
    }

    // Loot Manager Mechanic 8
    public class LootManager : MissionBehavior
    {
        public void SpawnLootBag(Vec3 position, int goldValue)
        {
            InformationManager.DisplayMessage(new InformationMessage($"Boss loot! {goldValue} gold bag - carry to treasury! F to pick", Colors.Gold));
            // Spawn prop ni_loot_bag_gold at position
            // Mission.Current.Scene.CreateGameEntity...
        }
    }

    // Fortress, Scavenge, Squad managers are in FortressBuildManager.cs
}
