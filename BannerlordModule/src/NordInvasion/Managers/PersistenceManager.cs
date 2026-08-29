using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using NordInvasion.Utils;

namespace NordInvasion.Managers
{
    // Mechanic 12: Persistence 2.0 + 15 Campaign + 24 Meta + 26 Ranks
    //
    // Бэкенд: PHP + MySQL (src/backend-php). HTTP form-encoded -> JSON.
    // URL/секрет задаются статически перед стартом миссии (DedicatedServer):
    //   PersistenceManager.BackendUrl = "http://127.0.0.1:8080";
    //   PersistenceManager.ApiSecret  = "...";
    public class PersistenceManager : MissionBehavior
    {
        public static string BackendUrl = "http://localhost:8080";
        public static string ApiSecret = "";

        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private readonly HashSet<Agent> _loginStarted = new HashSet<Agent>();

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
            public List<string> MetaNodes = new List<string>();
            public int Level = 1;
            public int SeasonPoints = 0;
            public int BestWave = 0;
            public string PlayerId = "";
            public string SteamId = "";

            public PlayerGoldComponent(Agent agent) : base(agent) { }

            public void AddGold(int amount) { Gold += amount; if (Gold < 0) Gold = 0; }
            public void AddMetal(int amount) { Metal += amount; }
            public void AddWood(int amount) { Wood += amount; }
            public bool HasPerk(int perkId) => Perks.Contains(perkId);
            public void AddPerk(int perkId) { if (!Perks.Contains(perkId)) Perks.Add(perkId); }
        }

        // ===== HTTP (form-encoded -> JSON) =====

        static async Task<string> PostForm(string path, params KeyValuePair<string, string>[] fields)
        {
            try
            {
                var msg = new HttpRequestMessage(HttpMethod.Post, BackendUrl + path);
                msg.Content = new FormUrlEncodedContent(fields);
                if (!string.IsNullOrEmpty(ApiSecret))
                    msg.Headers.Add("X-NI-Secret", ApiSecret);
                var resp = await _http.SendAsync(msg);
                return await resp.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Backend POST " + path + " error: " + ex.Message);
                return "";
            }
        }

        static async Task<string> GetText(string path)
        {
            try
            {
                var msg = new HttpRequestMessage(HttpMethod.Get, BackendUrl + path);
                if (!string.IsNullOrEmpty(ApiSecret))
                    msg.Headers.Add("X-NI-Secret", ApiSecret);
                var resp = await _http.SendAsync(msg);
                return await resp.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Backend GET " + path + " error: " + ex.Message);
                return "";
            }
        }

        static KeyValuePair<string, string> Kv(string k, string v)
        {
            return new KeyValuePair<string, string>(k, v ?? "");
        }

        /// <summary>
        /// Заполняет PlayerId/SteamId, если логин ещё не завершился
        /// (иначе бэкенд не сможет привязать событие к профилю).
        /// </summary>
        static void EnsureIdentity(Agent agent, PlayerGoldComponent comp)
        {
            if (string.IsNullOrEmpty(comp.SteamId))
                comp.SteamId = NIPeers.GetSteamId(agent);
            if (string.IsNullOrEmpty(comp.PlayerId))
                comp.PlayerId = NIPeers.MakePlayerId(comp.SteamId, NIPeers.GetPeerId(agent));
        }

        // ===== Логин / профиль =====

        public void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);
            if (agent == null) return;
            if (Mission.PlayerTeam == null || agent.Team != Mission.PlayerTeam) return;
            EnsureComponents(agent);
            StartLogin(agent);
        }

        /// <summary>Асинхронный login + применение сохранённого профиля (gold/wood/metal/perks/titles/meta).</summary>
        void StartLogin(Agent agent)
        {
            if (_loginStarted.Contains(agent)) return;
            _loginStarted.Add(agent);

            string steamId = NIPeers.GetSteamId(agent);
            string name = NIPeers.GetPeerId(agent);
            string pid = NIPeers.MakePlayerId(steamId, name);

            Task.Run(async () =>
            {
                var data = await LoginPlayer(pid, steamId, name);
                if (data == null) return;
                if (agent == null || !agent.IsActive()) return;
                ApplyProfile(agent, data);
            });
        }

        public async Task<PlayerData> LoginPlayer(string pid, string steamId, string name)
        {
            try
            {
                var body = await PostForm("/api/player/login",
                    Kv("player_id", pid),
                    Kv("steam_id", steamId ?? ""),
                    Kv("name", name ?? ""));
                var obj = NIJson.ParseObject(body);
                if (obj.Count == 0 || NIJson.GetString(obj, "error") != "") return null;

                return new PlayerData
                {
                    Gold = NIJson.GetInt(obj, "gold", 500),
                    Wood = NIJson.GetInt(obj, "wood"),
                    Metal = NIJson.GetInt(obj, "metal"),
                    Level = NIJson.GetInt(obj, "level", 1),
                    SeasonPoints = NIJson.GetInt(obj, "season_points"),
                    BestWave = NIJson.GetInt(obj, "best_wave"),
                    Kills = NIJson.GetInt(obj, "kills"),
                    Deaths = NIJson.GetInt(obj, "deaths"),
                    Blueprints = NIJson.GetStringArray(obj, "blueprints"),
                    Titles = NIJson.GetStringArray(obj, "titles"),
                    Perks = NIJson.GetIntArray(obj, "perks"),
                    Meta = NIJson.GetStringArray(obj, "meta"),
                };
            }
            catch { return null; }
        }

        /// <summary>Применяет сохранённый профиль к свежему агенту (респаун/пересоздание).</summary>
        public void ApplyProfile(Agent agent, PlayerData data)
        {
            if (agent == null || data == null) return;
            EnsureComponents(agent);
            var comp = agent.GetComponent<PlayerGoldComponent>();
            if (comp == null) return;

            comp.Gold = Math.Max(comp.Gold, data.Gold); // не обнуляем то, что уже заработано в бою
            comp.Wood += data.Wood;
            comp.Metal += data.Metal;
            comp.Level = Math.Max(comp.Level, data.Level);
            comp.SeasonPoints = data.SeasonPoints;
            comp.BestWave = data.BestWave;
            comp.Kills += data.Kills;
            comp.Deaths += data.Deaths;
            comp.SteamId = NIPeers.GetSteamId(agent);
            comp.PlayerId = NIPeers.MakePlayerId(comp.SteamId, NIPeers.GetPeerId(agent));

            foreach (var p in data.Perks) comp.AddPerk(p);
            foreach (var b in data.Blueprints) if (!comp.Blueprints.Contains(b)) comp.Blueprints.Add(b);
            foreach (var t in data.Titles) if (!comp.Titles.Contains(t)) comp.Titles.Add(t);
            foreach (var m in data.Meta) if (!comp.MetaNodes.Contains(m)) comp.MetaNodes.Add(m);

            // перки-эффекты на агенте
            var perkComp = agent.GetComponent<Components.PerkAgentComponent>();
            if (perkComp != null)
                foreach (var p in data.Perks) perkComp.ApplyPerk(p);

            // мета-бонусы (veteran_1: +gold, blacksmith: +ресурсы, HP-моды)
            Mission.GetMissionBehavior<MetaProgressionManager>()?.ApplyMetaBonuses(agent, data);
            Mission.GetMissionBehavior<MetaProgressionManager>()?.ApplyCosmetics(agent, data.Titles);

            InformationManager.DisplayMessage(new InformationMessage(
                $"Profile loaded: Lv{comp.Level}, {comp.Gold}g, {comp.Wood}w/{comp.Metal}m"
                + (comp.Titles.Count > 0 ? ", titles: " + string.Join(",", comp.Titles) : ""), Colors.Green));
        }

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

        // ===== Боевые события -> backend =====

        public void OnKill(Agent killed, Agent killer, int wave, int goldReward)
        {
            if (killer == null) return;
            var comp = killer.GetComponent<PlayerGoldComponent>();
            if (comp == null) return;
            EnsureIdentity(killer, comp);

            string troop = killed.Character != null ? killed.Character.StringId : "unknown";
            bool isBoss = troop.Contains("chieftain") || troop.Contains("boss");

            string pid = comp.PlayerId, sid = comp.SteamId;
            string name = killer.Name != null ? killer.Name.ToString() : "unknown";

            Task.Run(async () =>
            {
                await PostForm("/api/kill",
                    Kv("player_id", pid), Kv("steam_id", sid), Kv("name", name),
                    Kv("killed_troop", troop),
                    Kv("gold_reward", goldReward.ToString()),
                    Kv("wood", "0"), Kv("metal", "0"),
                    Kv("wave", wave.ToString()),
                    Kv("is_boss", isBoss ? "1" : "0"));
            });
        }

        /// <summary>Награды за волну + best_wave + XP (по одному вызову на живого игрока).</summary>
        public void OnWaveCompletedFor(Agent agent, int wave, int gold, int wood, int metal)
        {
            var comp = agent.GetComponent<PlayerGoldComponent>();
            if (comp == null) return;
            EnsureIdentity(agent, comp);
            string pid = comp.PlayerId, sid = comp.SteamId;
            string name = agent.Name != null ? agent.Name.ToString() : "unknown";

            Task.Run(async () =>
            {
                await PostForm("/api/wave/complete",
                    Kv("player_id", pid), Kv("steam_id", sid), Kv("name", name),
                    Kv("wave", wave.ToString()),
                    Kv("gold", gold.ToString()),
                    Kv("wood", wood.ToString()),
                    Kv("metal", metal.ToString()),
                    Kv("perk_id", "-1"));
            });
        }

        /// <summary>Игрок выбрал перк - сохраняем (без повторных наград).</summary>
        public void ReportPerk(Agent agent, int perkId)
        {
            var comp = agent.GetComponent<PlayerGoldComponent>();
            if (comp == null) return;
            EnsureIdentity(agent, comp);
            string pid = comp.PlayerId, sid = comp.SteamId;
            string name = agent.Name != null ? agent.Name.ToString() : "unknown";

            Task.Run(async () =>
            {
                await PostForm("/api/perk/record",
                    Kv("player_id", pid), Kv("steam_id", sid), Kv("name", name),
                    Kv("perk_id", perkId.ToString()));
            });
        }

        /// <summary>Конец забега: победа/поражение, best_wave, бонусы, титулы.</summary>
        public void SaveRun(Agent agent, bool won, int waveReached)
        {
            var comp = agent.GetComponent<PlayerGoldComponent>();
            if (comp == null) return;
            EnsureIdentity(agent, comp);
            string pid = comp.PlayerId, sid = comp.SteamId;
            string name = agent.Name != null ? agent.Name.ToString() : "unknown";
            int kills = comp.Kills, deaths = comp.Deaths;

            Task.Run(async () =>
            {
                await PostForm("/api/run/save",
                    Kv("player_id", pid), Kv("steam_id", sid), Kv("name", name),
                    Kv("won", won ? "1" : "0"),
                    Kv("wave_reached", waveReached.ToString()),
                    Kv("kills", kills.ToString()),
                    Kv("deaths", deaths.ToString()));
            });
        }

        /// <summary>Медик реанимировал игрока -> ранг Savior (50 реанимаций).</summary>
        public void OnMedicRevive(Agent medic)
        {
            IncrementStat(medic, "revives");
        }

        /// <summary>Построена постройка -> ранг Master Engineer (100 построек).</summary>
        public void OnBuildPlaced(Agent builder)
        {
            IncrementStat(builder, "builds");
        }

        void IncrementStat(Agent agent, string stat)
        {
            var comp = agent.GetComponent<PlayerGoldComponent>();
            if (comp == null) return;
            EnsureIdentity(agent, comp);
            string pid = comp.PlayerId, sid = comp.SteamId;
            string name = agent.Name != null ? agent.Name.ToString() : "unknown";

            Task.Run(async () =>
            {
                var body = await PostForm("/api/stat/increment",
                    Kv("player_id", pid), Kv("steam_id", sid), Kv("name", name),
                    Kv("stat", stat));
                var obj = NIJson.ParseObject(body);
                var earned = NIJson.GetStringArray(obj, "titles_earned");
                if (earned.Length > 0)
                {
                    foreach (var t in earned) if (!comp.Titles.Contains(t)) comp.Titles.Add(t);
                    if (agent.IsActive())
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"{agent.Name} earned title: {string.Join(", ", earned)}", Colors.Gold));
                }
            });
        }

        /// <summary>Профиль игрока из бэкенда (login).</summary>
        public class PlayerData
        {
            public int Gold;
            public int Wood;
            public int Metal;
            public string[] Blueprints;
            public string[] Titles;
            public string[] Meta;
            public int[] Perks;
            public int Level;
            public int SeasonPoints;
            public int BestWave;
            public int Kills;
            public int Deaths;
        }

        // ===== Кабинет игрока (shop/meta) =====

        public void UnlockBlueprint(Agent agent, string blueprintId)
        {
            var comp = agent.GetComponent<PlayerGoldComponent>();
            if (comp == null) return;
            EnsureIdentity(agent, comp);
            if (comp.Blueprints.Contains(blueprintId)) return;
            string pid = comp.PlayerId, sid = comp.SteamId;
            string name = agent.Name != null ? agent.Name.ToString() : "unknown";

            Task.Run(async () =>
            {
                var body = await PostForm("/api/blueprint/unlock",
                    Kv("player_id", pid), Kv("steam_id", sid), Kv("name", name),
                    Kv("blueprint_id", blueprintId));
                var obj = NIJson.ParseObject(body);
                if (NIJson.GetString(obj, "error") == "")
                {
                    comp.Blueprints.Add(blueprintId);
                    if (agent.IsActive())
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"Blueprint unlocked: {blueprintId}", Colors.Cyan));
                }
            });
        }

        /// <summary>Покупка узла мета-дерева за season_points (сервер проверяет prerequisites).</summary>
        public void UnlockMetaNode(Agent agent, string nodeId)
        {
            var comp = agent.GetComponent<PlayerGoldComponent>();
            if (comp == null) return;
            EnsureIdentity(agent, comp);
            if (comp.MetaNodes.Contains(nodeId)) return;
            string pid = comp.PlayerId, sid = comp.SteamId;
            string name = agent.Name != null ? agent.Name.ToString() : "unknown";

            Task.Run(async () =>
            {
                var body = await PostForm("/api/meta/unlock",
                    Kv("player_id", pid), Kv("steam_id", sid), Kv("name", name),
                    Kv("node_id", nodeId));
                var obj = NIJson.ParseObject(body);
                if (NIJson.GetString(obj, "error") == "")
                {
                    comp.MetaNodes.Add(nodeId);
                    var earned = NIJson.GetStringArray(obj, "titles_earned");
                    if (earned.Length > 0 && agent.IsActive())
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"{agent.Name} earned title: {string.Join(", ", earned)}", Colors.Gold));
                }
                else
                {
                    if (agent.IsActive())
                        InformationManager.DisplayMessage(new InformationMessage(
                            "Meta unlock failed: " + NIJson.GetString(obj, "error"), Colors.Red));
                }
            });
        }

        // ===== Кампания (village battle report) =====

        public void OnCampaignWin()
        {
            var team = Mission.PlayerTeam;
            // бэкенд ищет игроков по player_id (steam_.../name_md5), а не по имени
            var players = (team != null ? team.ActiveAgents : Enumerable.Empty<Agent>())
                .Select(a =>
                {
                    var comp = a.GetComponent<PlayerGoldComponent>();
                    return (comp != null && !string.IsNullOrEmpty(comp.PlayerId))
                        ? comp.PlayerId
                        : NIPeers.MakePlayerId(NIPeers.GetSteamId(a), NIPeers.GetPeerId(a));
                })
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToList();
            string csv = string.Join(",", players);

            Task.Run(async () =>
            {
                await PostForm("/api/campaign/battle",
                    Kv("village_id", "0"),
                    Kv("won", "1"),
                    Kv("players", csv),
                    Kv("wave_reached", "25"));
            });
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
        private readonly Random _rand = new Random();

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

            // сохраняем выбранный перк в бэкенд
            Mission.GetMissionBehavior<PersistenceManager>()?.ReportPerk(agent, perkId);

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
                main?.GetComponent<PersistenceManager.PlayerGoldComponent>()?.AddGold(goldValue);
                return;
            }

            var bag = new Machines.LootBagUsable { GoldValue = goldValue };
            entity.AddComponent(bag);
        }
    }

    // Fortress, Scavenge, Squad managers are in FortressBuildManager.cs
}
