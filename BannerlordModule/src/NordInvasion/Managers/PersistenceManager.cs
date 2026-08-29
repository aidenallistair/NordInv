using System;
using System.Net.Http;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using System.Collections.Generic;
using System.Linq;
using NordInvasion.Utils;
using TaleWorlds.Core;

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
            public bool IsCarryingLoot = false; // Mechanic 8: несет мешок босса
            public int CarriedLootValue = 0;
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
                        new System.Collections.Generic.KeyValuePair<string, string>("player_id", NIPeers.GetPeerId(killer)),
                        new System.Collections.Generic.KeyValuePair<string, string>("player_name", killer.Name != null ? killer.Name.ToString() : "unknown"),
                        new System.Collections.Generic.KeyValuePair<string, string>("killed_troop", killed.Character?.StringId ?? "unknown"),
                        new System.Collections.Generic.KeyValuePair<string, string>("gold_reward", "10"),
                        new System.Collections.Generic.KeyValuePair<string, string>("wave", wave.ToString()),
                    });
                    await _http.PostAsync($"{_backendUrl}/api/kill", content);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Backend kill error: {ex.Message}");
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
                EnsureComponents(agent);
        }

        /// <summary>Гарантирует наличие игровых компонентов (для переспауненных агентов).</summary>
        public void EnsureComponents(Agent agent)
        {
            if (agent == null) return;
            if (agent.GetComponent<PlayerGoldComponent>() == null)
                agent.AddComponent(new PlayerGoldComponent(agent));
            if (agent.GetComponent<Components.WoundStaminaComponent>() == null)
                agent.AddComponent(new Components.WoundStaminaComponent(agent));
            if (agent.GetComponent<Components.PerkAgentComponent>() == null)
                agent.AddComponent(new Components.PerkAgentComponent(agent));
            if (agent.GetComponent<Components.ElementalWeaponComponent>() == null)
                agent.AddComponent(new Components.ElementalWeaponComponent(agent, Components.ElementalType.None));
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
        // Окно выбора: 15 сек (как в NI_PerkChoice_VM)
        private const float ChoiceWindowSec = 15f;

        private class PendingChoice
        {
            public List<Models.PerkDefinition> Perks;
            public float EndTime;
        }

        private Dictionary<Agent, PendingChoice> _pending = new Dictionary<Agent, PendingChoice>();

        public override void OnMissionTick(float dt)
        {
            if (Mission.PlayerTeam == null) return;
            var now = Mission.CurrentTime;

            // Убранные агенты
            foreach (var kvp in _pending.ToList())
                if (!kvp.Key.IsActive()) _pending.Remove(kvp.Key);

            // Тайм-аут: не выбрал за 15 сек - рандомный перк
            foreach (var kvp in _pending.ToList())
            {
                if (now > kvp.Value.EndTime)
                {
                    var perk = kvp.Value.Perks[Utils.NIMath.ClampInt(MBRandom.RandomInt(kvp.Value.Perks.Count), 0, kvp.Value.Perks.Count - 1)];
                    ApplyPerk(kvp.Key, perk.Id);
                    InformationManager.DisplayMessage(new InformationMessage($"No choice in time - got: {perk.Name}", Colors.Yellow));
                    _pending.Remove(kvp.Key);
                }
            }
        }

        public void ShowChoiceToAll()
        {
            if (Mission.PlayerTeam == null) return;
            foreach (var agent in Mission.PlayerTeam.ActiveAgents.ToList())
            {
                ShowChoice(agent);
            }
        }

        public void ShowChoice(Agent agent)
        {
            if (agent == null || _pending.ContainsKey(agent)) return;
            var perks = Models.PerkDatabase.GetRandomThree(_rand);
            _pending[agent] = new PendingChoice { Perks = perks, EndTime = Mission.CurrentTime + ChoiceWindowSec };

            // MVP: выбор через сообщения (Gauntlet-подключение - следующий шаг, см. docs/AUDIT.md).
            InformationManager.DisplayMessage(new InformationMessage(
                $"PERK CHOICE ({(int)ChoiceWindowSec}s): 1) {perks[0].Name}  2) {perks[1].Name}  3) {perks[2].Name}", Colors.Gold));
        }

        /// <summary>Вызов от Gauntlet-кнопок (ExecuteChoose1/2/3 в NI_PerkChoice_VM).</summary>
        public void ChooseForAgent(Agent agent, int index)
        {
            if (!_pending.TryGetValue(agent, out var choice)) return;
            if (index < 0 || index >= choice.Perks.Count) return;
            ApplyPerk(agent, choice.Perks[index].Id);
            _pending.Remove(agent);
        }

        public void ApplyPerk(Agent agent, int perkId)
        {
            var goldComp = agent.GetComponent<PersistenceManager.PlayerGoldComponent>();
            goldComp?.AddPerk(perkId);

            var perkComp = agent.GetComponent<Components.PerkAgentComponent>();
            perkComp?.ApplyPerk(perkId);

            var def = Models.PerkDatabase.GetById(perkId);
            if (def != null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Perk applied: {def.Name} - {def.Desc}", Colors.Green));
                Audio.NISound.PlayPerkApplied();
            }
        }
    }

    // Loot Manager Mechanic 8
    public class LootManager : MissionBehavior
    {
        /// <summary>Спавнит физ-мешок с золотом босса. F - подобрать, донести до казны.</summary>
        public void SpawnLootBag(Vec3 position, int goldValue)
        {
            InformationManager.DisplayMessage(new InformationMessage($"Boss loot! {goldValue} gold bag - carry to treasury! F to pick", Colors.Gold));

            var entity = Machines.PropSpawner.SpawnWithFallback(
                Mission.Current.Scene, "ni_loot_bag_gold", Machines.PropSpawner.FallbackChest, position);
            if (entity == null)
            {
                // Fallback: без пропса золото просто падает в казну
                InformationManager.DisplayMessage(new InformationMessage($"(loot bag asset missing - +{goldValue} gold auto-deposited)", Colors.Yellow));
                var main = Mission.Current?.MainAgent;
                main?.GetComponent<PlayerGoldComponent>()?.AddGold(goldValue);
                return;
            }

            var bag = new Machines.LootBagUsable { GoldValue = goldValue };
            entity.AddComponent(bag);
        }
    }

    // Fortress, Scavenge, Squad managers are in FortressBuildManager.cs
}
