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

    public class PersistenceManager : MissionBehavior
    {
        public static string BackendUrl = "http://localhost:8080";
        public static string ApiSecret = "";

        /// <summary>
        /// Бэкенд ответил хотя бы раз (логин/каталог). Пока false - магазин
        /// работает локально (BuyLocal), чтобы мод был играбельным без MySQL.
        /// </summary>
        public static bool BackendReady;

        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private readonly HashSet<Agent> _loginStarted = new HashSet<Agent>();

        // Ответы бэкенда приходят из Task.Run - в UI/игру сообщения гоним через
        // очередь и отдаём в OnMissionTick (InformationManager не тред-безопасен).
        private readonly Queue<Action> _uiQueue = new Queue<Action>();

        public void QueueUi(Action action)
        {
            if (action == null) return;
            lock (_uiQueue) _uiQueue.Enqueue(action);
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            // Dedicated-сервер может задать адрес без пересборки dll или через NISettings:
            //   set NI_BACKEND_URL=http://10.0.0.5:8080
            //   set NI_API_SECRET=тот-же-секрет-что-в-config.php
            var settings = Settings.NISettings.Instance;
            if (!string.IsNullOrEmpty(settings.BackendUrl)) BackendUrl = settings.BackendUrl.TrimEnd('/');
            if (!string.IsNullOrEmpty(settings.ApiSecret)) ApiSecret = settings.ApiSecret;
            var url = Environment.GetEnvironmentVariable("NI_BACKEND_URL");
            if (!string.IsNullOrEmpty(url)) BackendUrl = url.TrimEnd('/');
            var secret = Environment.GetEnvironmentVariable("NI_API_SECRET");
            if (!string.IsNullOrEmpty(secret)) ApiSecret = secret;
        }

        private bool _armorySpawned = false;

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            // "Физический UI": оружейный ящик с UsableMachine (F = сервисные покупки),
            // пока Gauntlet-экраны не подключены (docs/AUDIT.md).
            if (!_armorySpawned)
            {
                var main = Mission.Current != null ? Mission.Current.MainAgent : null;
                if (main != null)
                {
                    _armorySpawned = true;
                    SpawnArmoryChest(main);
                }
                return;
            }

            if (_uiQueue.Count == 0) return;
            for (int i = 0; i < 8 && _uiQueue.Count > 0; i++)
            {
                Action action = null;
                lock (_uiQueue)
                    if (_uiQueue.Count > 0) action = _uiQueue.Dequeue();
                if (action == null) continue;
                try { action(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("PersistenceManager UI: " + ex.Message); }
            }
        }

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
            public List<string> Cosmetics = new List<string>(); // skins рангов (Mechanic 26)
            public List<string> MetaNodes = new List<string>();
            public int Level = 1;
            public int SeasonPoints = 0;
            public int SeasonPointsEarned = 0;
            public int BattlepassLevel = 0;
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
                    Cosmetics = NIJson.GetStringArray(obj, "cosmetics"),
                    BattlepassLevel = NIJson.GetInt(obj, "battlepass_level"),
                    SeasonPointsEarned = NIJson.GetInt(obj, "season_points_earned"),
                };
                BackendReady = true;
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
            if (data.Cosmetics != null)
                foreach (var c in data.Cosmetics) if (!comp.Cosmetics.Contains(c)) comp.Cosmetics.Add(c);
            comp.BattlepassLevel = data.BattlepassLevel;
            comp.SeasonPointsEarned = data.SeasonPointsEarned;

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
            public string[] Cosmetics;
            public int Level;
            public int SeasonPoints;
            public int SeasonPointsEarned;
            public int BattlepassLevel;
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

        // ===== Магазин / BattlePass / Кампания (Mechanic 12, 2, 18, 23, 26) =====
        //
        // Цена и наличие товара проверяет сервер (/api/shop/buy), клиент получает
        // новый баланс + список наград ("granted"). Если бэкенда нет - покупка
        // применяется локально, иначе одиночная игра была бы без магазина.

        /// <summary>
        /// Сообщение в игровой чат из фонового потока: кладём в очередь и отдаём в
        /// OnMissionTick - InformationManager нельзя трогать из Task.Run.
        /// </summary>
        void SpawnArmoryChest(Agent main)
        {
            var scene = Mission.Current.Scene;
            if (scene == null) return;
            var pos = main.Position + main.LookDirection * 3f;
            var entity = Machines.PropSpawner.SpawnWithFallback(scene, "ni_armory_chest",
                Machines.PropSpawner.FallbackChest, pos);
            if (entity == null) return;   // ни своего, ни vanilla-меша - просто нет ящика
            entity.AddComponent(new Machines.NI_ArmoryUsable());
            InformationManager.DisplayMessage(new InformationMessage(
                "Armory chest is next to the spawn - press F for heal/ammo/repair kits", Colors.Cyan));
        }

        public void Notify(string message, uint color)
        {
            QueueUi(() => InformationManager.DisplayMessage(new InformationMessage(message, color)));
        }

        static void SplitGrant(string grant, out string kind, out int value, out string arg)
        {
            kind = (grant ?? "").Trim();
            arg = "";
            value = 0;
            int colon = kind.IndexOf(':');
            if (colon >= 0)
            {
                arg = kind.Substring(colon + 1).Trim();
                kind = kind.Substring(0, colon).Trim();
                int.TryParse(arg, out value);
            }
        }

        /// <summary>
        /// Применяет награды бэкенда на агенте. Сервер уже начислил gold/wood/metal/
        /// blueprints в БД - здесь только то, что видно в миссии (heal/ammo/repair)
        /// и локальное зеркало счёта (wood/metal/blueprint).
        /// </summary>
        public static void ApplyGrants(Agent agent, string[] granted)
        {
            if (agent == null || granted == null) return;
            var comp = agent.GetComponent<PlayerGoldComponent>();
            var build = Mission.Current != null ? Mission.Current.GetMissionBehavior<FortressBuildManager>() : null;

            foreach (var raw in granted)
            {
                string kind, arg;
                int value;
                SplitGrant(raw, out kind, out value, out arg);

                switch (kind)
                {
                    case "wood":
                        comp?.AddWood(value);
                        break;
                    case "metal":
                        comp?.AddMetal(value);
                        break;
                    case "gold":
                        comp?.AddGold(value);
                        break;
                    case "blueprint":
                        if (comp != null && !string.IsNullOrEmpty(arg) && !comp.Blueprints.Contains(arg))
                        {
                            comp.Blueprints.Add(arg);
                            InformationManager.DisplayMessage(new InformationMessage($"Blueprint unlocked: {arg}", Colors.Cyan));
                        }
                        break;
                    case "title":
                        if (comp != null && !string.IsNullOrEmpty(arg) && !comp.Titles.Contains(arg))
                        {
                            comp.Titles.Add(arg);
                            Mission.Current?.GetMissionBehavior<MetaProgressionManager>()?.ApplyCosmetics(agent, comp.Titles.ToArray());
                        }
                        break;
                    case "skin":
                        if (comp != null && !string.IsNullOrEmpty(arg) && !comp.Cosmetics.Contains(arg)) comp.Cosmetics.Add(arg);
                        break;
                    case "heal":
                        if (value > 0) agent.Health = (int)System.Math.Min(agent.Health + value, agent.HealthLimit);
                        break;
                    case "ammo":
                        build?.SpawnAmmoBox(agent.Position);
                        break;
                    case "repair":
                        if (build != null && value > 0) build.RepairNearest(agent, value);
                        break;
                    case "season_points":
                        if (comp != null) comp.SeasonPointsEarned += value; // тратит/копит сервер
                        break;
                }
            }
        }

        static void ApplyBalances(PlayerGoldComponent comp, Dictionary<string, object> obj)
        {
            if (comp == null || obj == null || obj.Count == 0) return;
            if (obj.ContainsKey("gold")) comp.Gold = NIJson.GetInt(obj, "gold", comp.Gold);
            if (obj.ContainsKey("wood")) comp.Wood = NIJson.GetInt(obj, "wood", comp.Wood);
            if (obj.ContainsKey("metal")) comp.Metal = NIJson.GetInt(obj, "metal", comp.Metal);
            if (obj.ContainsKey("season_points")) comp.SeasonPoints = NIJson.GetInt(obj, "season_points", comp.SeasonPoints);
            if (obj.ContainsKey("season_points_earned")) comp.SeasonPointsEarned = NIJson.GetInt(obj, "season_points_earned", comp.SeasonPointsEarned);
            if (obj.ContainsKey("battlepass_level")) comp.BattlepassLevel = NIJson.GetInt(obj, "battlepass_level", comp.BattlepassLevel);
            foreach (var b in NIJson.GetStringArray(obj, "blueprints")) if (!comp.Blueprints.Contains(b)) comp.Blueprints.Add(b);
            foreach (var t in NIJson.GetStringArray(obj, "titles")) if (!comp.Titles.Contains(t)) comp.Titles.Add(t);
            foreach (var c in NIJson.GetStringArray(obj, "cosmetics")) if (!comp.Cosmetics.Contains(c)) comp.Cosmetics.Add(c);
        }

        /// <summary>Покупка без бэкенда: те же цены/награды, но только до конца забега.</summary>
        void BuyLocal(Agent agent, Models.ShopItem item)
        {
            var comp = agent.GetComponent<PlayerGoldComponent>();
            if (comp == null) return;
            if (item.Type == "blueprint" && item.Grants.Length > 0)
            {
                string k, arg;
                int v;
                SplitGrant(item.Grants[0], out k, out v, out arg);
                if (k == "blueprint" && comp.Blueprints.Contains(arg))
                {
                    Notify($"{item.Name}: already unlocked", Colors.Yellow);
                    return;
                }
            }
            if (comp.Gold < item.Gold || comp.Wood < item.Wood || comp.Metal < item.Metal)
            {
                Notify($"Not enough resources: {item.Name} costs {item.Gold}g {item.Wood}w {item.Metal}m", Colors.Red);
                return;
            }
            comp.Gold -= item.Gold;
            comp.Wood -= item.Wood;
            comp.Metal -= item.Metal;
            ApplyGrants(agent, item.Grants);
            Notify($"Bought {item.Name} (offline mode - not saved to profile)", Colors.Gold);
        }

        /// <summary>Покупка позиции каталога (серверная проверка цены -> начисление на складе).</summary>
        public void BuyShopItem(Agent agent, string itemId, int qty = 1)
        {
            var item = Models.ShopCatalog.Get(itemId);
            if (item == null) { Notify($"Unknown shop item: {itemId}", Colors.Red); return; }
            if (agent == null) return;
            var comp = agent.GetComponent<PlayerGoldComponent>();
            if (comp == null) return;

            if (!BackendReady)
            {
                BuyLocal(agent, item);
                return;
            }

            EnsureIdentity(agent, comp);
            string pid = comp.PlayerId, sid = comp.SteamId;
            string name = agent.Name != null ? agent.Name.ToString() : "unknown";
            if (qty < 1) qty = 1;
            if (qty > 5) qty = 5;

            Task.Run(async () =>
            {
                var body = await PostForm("/api/shop/buy",
                    Kv("player_id", pid), Kv("steam_id", sid), Kv("name", name),
                    Kv("item_id", itemId), Kv("qty", qty.ToString()));
                var obj = NIJson.ParseObject(body);
                var error = NIJson.GetString(obj, "error");
                if (obj.Count == 0)
                {
                    Notify($"Shop unavailable - bought {item.Name} locally (не сохранится)", Colors.Yellow);
                    BuyLocal(agent, item);
                    return;
                }
                if (error != "")
                {
                    Notify($"Purchase refused: {error}", Colors.Red);
                    return;
                }
                ApplyBalances(comp, obj);
                ApplyGrants(agent, NIJson.GetStringArray(obj, "granted"));
                BackendReady = true;
                Notify($"Bought {item.Name} x{qty}", Colors.Gold);
            });
        }

        /// <summary>Цены/наградные позиции тянутся с бэкенда, fallback - встроенная таблица.</summary>
        public void RefreshShopCatalog()
        {
            Task.Run(async () =>
            {
                var body = await GetText("/api/shop/catalog");
                var obj = NIJson.ParseObject(body);
                if (NIJson.GetString(obj, "error") != "" || obj.Count == 0) return;
                int n = Models.ShopCatalog.ReplaceWith(obj);
                if (n > 0)
                {
                    BackendReady = true;
                    QueueUi(() => InformationManager.DisplayMessage(
                        new InformationMessage($"Shop catalog loaded from backend ({n} positions)", Colors.Cyan)));
                }
            });
        }

        // ----- BattlePass -----

        public class BattlepassInfo
        {
            public int Level;
            public int MaxLevel = 20;
            public int Points;
            public int PointsToNext;
            public List<int> Claimed = new List<int>();
            public string Line = "";
        }

        public static readonly BattlepassInfo Battlepass = new BattlepassInfo();

        public void RefreshBattlepass(Agent agent)
        {
            var comp = agent != null ? agent.GetComponent<PlayerGoldComponent>() : null;
            if (comp == null) return;
            EnsureIdentity(agent, comp);
            string pid = comp.PlayerId, sid = comp.SteamId;

            Task.Run(async () =>
            {
                var body = await GetText("/api/battlepass/progress?player_id=" + Uri.EscapeDataString(pid)
                                         + "&steam_id=" + Uri.EscapeDataString(sid ?? ""));
                var obj = NIJson.ParseObject(body);
                if (obj.Count == 0 || NIJson.GetString(obj, "error") != "") return;
                Battlepass.Level = NIJson.GetInt(obj, "level");
                Battlepass.MaxLevel = NIJson.GetInt(obj, "max_level", 20);
                Battlepass.Points = NIJson.GetInt(obj, "points");
                Battlepass.PointsToNext = NIJson.GetInt(obj, "points_to_next");
                Battlepass.Claimed = new List<int>(NIJson.GetIntArray(obj, "claimed"));
                Battlepass.Line = $"BattlePass {Battlepass.Level}/{Battlepass.MaxLevel} - next in {Battlepass.PointsToNext} SP";
                comp.BattlepassLevel = Battlepass.Level;
                QueueUi(() => InformationManager.DisplayMessage(new InformationMessage(Battlepass.Line, Colors.Cyan)));
            });
        }

        /// <summary>Забирает награду уровня; если level &lt;= 0 - первый ещё не полученный.</summary>
        public void ClaimBattlepass(Agent agent, int level)
        {
            var comp = agent != null ? agent.GetComponent<PlayerGoldComponent>() : null;
            if (comp == null) return;
            EnsureIdentity(agent, comp);
            string pid = comp.PlayerId, sid = comp.SteamId;
            string name = agent.Name != null ? agent.Name.ToString() : "unknown";

            Task.Run(async () =>
            {
                var body = await PostForm("/api/battlepass/claim",
                    Kv("player_id", pid), Kv("steam_id", sid), Kv("name", name),
                    Kv("level", level.ToString()));
                var obj = NIJson.ParseObject(body);
                var error = NIJson.GetString(obj, "error");
                if (obj.Count == 0) { Notify("BattlePass server unavailable", Colors.Red); return; }
                if (error != "") { Notify($"BattlePass: {error}", Colors.Yellow); return; }
                ApplyBalances(comp, obj);
                ApplyGrants(agent, NIJson.GetStringArray(obj, "granted"));
                Notify($"BattlePass reward: {NIJson.GetString(obj, "reward_name", "granted")}", Colors.Gold);
                RefreshBattlepass(agent);
            });
        }

        /// <summary>Первый не выданный доступный уровень battlepass (для кнопки "Claim").</summary>
        public static int NextClaimableLevel()
        {
            for (int lvl = 1; lvl <= Battlepass.MaxLevel; lvl++)
                if (lvl <= Battlepass.Level && !Battlepass.Claimed.Contains(lvl)) return lvl;
            return -1;
        }

        // ----- Кампания: голоса и карта (Mechanic 15) -----

        public class CampaignVillage
        {
            public int Id;
            public string Name = "";
            public string Owner = "";
            public int Defense;
            public int Votes;
        }

        public static readonly List<CampaignVillage> Villages = new List<CampaignVillage>();

        public void VoteForVillage(Agent agent, int villageId)
        {
            var comp = agent != null ? agent.GetComponent<PlayerGoldComponent>() : null;
            if (comp == null) return;
            EnsureIdentity(agent, comp);
            string pid = comp.PlayerId, sid = comp.SteamId;
            string voter = string.IsNullOrEmpty(pid) ? (string.IsNullOrEmpty(sid) ? "unknown" : sid) : pid;

            if (!BackendReady)
            {
                Notify($"Voted for village {villageId} (offline - голос не сохранён)", Colors.Yellow);
                return;
            }

            Task.Run(async () =>
            {
                var body = await PostForm("/api/campaign/vote",
                    Kv("voter", voter), Kv("village_id", villageId.ToString()));
                var obj = NIJson.ParseObject(body);
                var error = NIJson.GetString(obj, "error");
                if (error != "") { Notify($"Campaign vote: {error}", Colors.Yellow); return; }
                Notify($"Vote recorded for village {villageId}", Colors.Green);
                RefreshCampaignMap();
            });
        }

        public void RefreshCampaignMap()
        {
            Task.Run(async () =>
            {
                var body = await GetText("/api/campaign/villages");
                var rows = NIJson.ParseObjectArrayFromJson(body);
                if (rows.Count == 0) return;
                var next = new List<CampaignVillage>();
                foreach (var row in rows)
                {
                    next.Add(new CampaignVillage
                    {
                        Id = NIJson.GetInt(row, "id"),
                        Name = NIJson.GetString(row, "name"),
                        Owner = NIJson.GetString(row, "owner"),
                        Defense = NIJson.GetInt(row, "defense"),
                        Votes = NIJson.GetInt(row, "votes"),
                    });
                }
                lock (Villages)
                {
                    Villages.Clear();
                    Villages.AddRange(next);
                }
                BackendReady = true;
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

            // Деревню атаки выбирает голосование сезона (механика 15), волна - из WaveManager
            int villageId = UI.NI_CampaignMap_VM.LeadingVillageId();
            if (villageId < 0) villageId = 0;
            int wave = Mission.GetMissionBehavior<Behaviors.NordInvasionWaveManagerBehavior>()?.WaveNumber ?? 0;
            bool won = wave >= Behaviors.NordInvasionWaveManagerBehavior.VictoryWave;

            Task.Run(async () =>
            {
                await PostForm("/api/campaign/battle",
                    Kv("village_id", villageId.ToString()),
                    Kv("won", won ? "1" : "0"),
                    Kv("players", csv),
                    Kv("wave_reached", wave.ToString()));
            });
        }
    }
}
