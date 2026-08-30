using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using NordInvasion.Models;
using NordInvasion.Managers;

namespace NordInvasion.Behaviors
{
    public class NordInvasionWaveManagerBehavior : MissionBehavior
    {
        public const int VictoryWave = 25;

        // Core state
        public int WaveNumber = 1;
        public int BotsAlive = 0;
        public int BotsTotal = 0;
        public WaveState State = WaveState.Preparing;
        public WaveObjective Objective = WaveObjective.KillAll;
        public MutatorType Mutator = MutatorType.None;
        public float NextWaveTime = 0f;
        public bool IsRespawnWave => WaveNumber % 4 == 0;
        public bool IsCampWave => WaveNumber % 5 == 0;

        private List<Agent> _spawnedNords = new List<Agent>();
        private Random _rand = new Random();
        private Team _nordTeam;
        private Team _playerTeam;
        private float _endMissionAt = -1f;

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (_nordTeam == null)
                _nordTeam = Mission.Teams.FirstOrDefault(t => t.Side == BattleSideEnum.Attacker);
            if (_playerTeam == null)
                _playerTeam = Mission.PlayerTeam ?? Mission.Teams.FirstOrDefault(t => t.Side == BattleSideEnum.Defender);

            // Отложенное завершение миссии (после сообщения о победе/поражении)
            if (_endMissionAt > 0f && Mission.CurrentTime > _endMissionAt)
            {
                _endMissionAt = -1f;
                Mission.Current.EndMission();
                return;
            }

            if (_playerTeam == null || _nordTeam == null) return;

            // Preparing -> Spawning
            if (State == WaveState.Preparing && Mission.CurrentTime > NextWaveTime)
            {
                // Check camp phase
                if (IsCampWave && WaveNumber > 1)
                {
                    var camp = Mission.GetMissionBehavior<CampPhaseBehavior>();
                    if (camp != null && !camp.IsCampPhase)
                    {
                        camp.StartCampPhase();
                        State = WaveState.Camp;
                        return;
                    }
                }
                SpawnWave();
            }

            // InProgress -> Completed (все норды мертвы ИЛИ цель выполнена)
            if (State == WaveState.InProgress)
            {
                var objective = Mission.GetMissionBehavior<NordInvasionObjectiveBehavior>();
                if (BotsAlive <= 0 || (objective != null && objective.ShouldEndWave()))
                {
                    OnWaveCompleted();
                    return;
                }

                // Провал цели = поражение
                if (objective != null && objective.ObjectiveFailed)
                {
                    Defeat("Objective failed!");
                    return;
                }

                // Check defeat
                int alivePlayers = _playerTeam.ActiveAgents.Count(a => !IsFallen(a));
                if (alivePlayers == 0)
                {
                    if (IsRespawnWave)
                        RespawnAllPlayers();
                    else
                        Defeat("All players dead!");
                }
            }

            // HUD tick (живые норды)
            var hud = Mission.GetMissionBehavior<UI.HUD.NI_HUD_Behavior>();
            if (hud != null && State == WaveState.InProgress)
                hud.UpdateWave(WaveNumber, BotsTotal, BotsAlive, Objective, Mutator);
        }

        void Defeat(string reason)
        {
            if (State == WaveState.Failed) return;
            State = WaveState.Failed;
            InformationManager.DisplayMessage(new InformationMessage($"{reason} DEFEAT! Swadia fell...", Colors.Red));
            Audio.NISound.PlayDefeat();

            // Сохраняем забег (поражение) для каждого живого игрока
            var persist = Mission.GetMissionBehavior<PersistenceManager>();
            if (persist != null && _playerTeam != null)
            {
                foreach (var agent in _playerTeam.ActiveAgents)
                {
                    if (!IsFallen(agent)) persist.SaveRun(agent, false, WaveNumber);
                }
            }
            _endMissionAt = Mission.CurrentTime + 3f;
        }

        bool IsFallen(Agent agent)
        {
            var wound = agent.GetComponent<Components.WoundStaminaComponent>();
            return wound != null && wound.IsFallen;
        }

        /// <summary>Респавн-волна: все игроки возвращаются в бой.</summary>
        void RespawnAllPlayers()
        {
            InformationManager.DisplayMessage(new InformationMessage("RESPAWN WAVE! All defenders return to the fight!", Colors.Cyan));

            // Ищем ближайшую игроку точку спавна (0-31)
            Vec3 basePos = default;
            bool hasPos = false;
            for (int i = 0; i < 32 && !hasPos; i++)
            {
                var e = Mission.Current.GetEntryPoint(i);
                if (e != null) { basePos = e.Position; hasPos = true; }
            }
            if (!hasPos) return;

            var fallen = _playerTeam.ActiveAgents.Where(a => IsFallen(a)).ToList();
            foreach (var agent in fallen)
            {
                agent.GetComponent<Components.WoundStaminaComponent>()?.Revive();
            }

            // Мертвые (не в ActiveAgents) - переспаун
            var troop = _playerTeam.ActiveAgents.FirstOrDefault()?.Character
                ?? Game.Current.ObjectManager.GetObject<CharacterObject>("swadian_villager");
            if (troop == null) return;
            var deaths = _playerTeam.DeathAgents.Count(a => a != null);
            for (int i = 0; i < deaths; i++)
            {
                var pos = basePos + new Vec3(i * 1.5f, 0f, 0f);
                var agent = Mission.Current.SpawnAgent(new AgentBuildData(troop).Team(_playerTeam).InitialPosition(pos));
                if (agent != null)
                    Mission.GetMissionBehavior<PersistenceManager>()?.EnsureComponents(agent);
            }
        }

        public void SetupWave(int waveNo)
        {
            WaveNumber = waveNo;
            State = WaveState.Preparing;

            // Objective every 3 waves
            if (waveNo % 3 == 0)
                Objective = (WaveObjective)_rand.Next(1, 5);
            else
                Objective = WaveObjective.KillAll;

            // Mutator every 4 waves
            if (waveNo % 4 == 0)
                Mutator = (MutatorType)_rand.Next(1, 13);
            else
                Mutator = MutatorType.None;

            // Marked player for Odin mutator
            if (Mutator == MutatorType.Marked)
            {
                var players = _playerTeam?.ActiveAgents;
                if (players != null && players.Count > 0)
                {
                    var marked = players[_rand.Next(players.Count)];
                    var director = Mission.GetMissionBehavior<NordInvasionDirectorBehavior>();
                    if (director != null) director.MarkedPlayer = marked;
                }
            }

            // Weather every 5 waves
            if (waveNo % 5 == 0)
            {
                var weather = Mission.GetMissionBehavior<NordInvasionWeatherBehavior>();
                weather?.SetRandomWeather();
            }

            // Calculate bot count with director
            int playerCount = _playerTeam?.ActiveAgents.Count ?? 1;
            playerCount = Math.Max(playerCount, 1);
            int baseCount = 8 + waveNo * 2 + playerCount * 2;
            var director = Mission.GetMissionBehavior<NordInvasionDirectorBehavior>();
            if (director != null)
            {
                baseCount = (int)(baseCount * director.GetMultiplier());
            }
            if (Mutator == MutatorType.BossRush) baseCount += 5;

            BotsTotal = Utils.NIMath.ClampInt(baseCount, 1, 120); // Bannerlord может держать ~120
            BotsAlive = BotsTotal;

            NextWaveTime = Mission.CurrentTime + 8f;

            InformationManager.DisplayMessage(new InformationMessage(
                $"Wave {WaveNumber} preparing... {BotsTotal} Nords! Obj: {Objective} Mutator: {Mutator} {(IsRespawnWave ? "[RESPAWN WAVE]" : "")}", Colors.Cyan));

            // Звук + стильное объявление мутатора
            var mutatorBehavior = Mission.GetMissionBehavior<NordInvasionMutatorBehavior>();
            mutatorBehavior?.ApplyMutator(Mutator);

            // Update HUD
            var hud = Mission.GetMissionBehavior<UI.HUD.NI_HUD_Behavior>();
            hud?.UpdateWave(WaveNumber, BotsTotal, BotsAlive, Objective, Mutator);
        }

        void SpawnWave()
        {
            State = WaveState.Spawning;
            _spawnedNords.Clear();

            // Squad spawn every 3rd wave (Mechanic 11)
            if (WaveNumber % 3 == 0 && Objective == WaveObjective.KillAll)
            {
                var squadMgr = Mission.GetMissionBehavior<SquadManager>();
                if (squadMgr != null)
                {
                    squadMgr.SpawnShieldWallSquad(32);
                    squadMgr.SpawnShieldWallSquad(40);
                    BotsTotal = Math.Max(0, BotsTotal - 12);
                    BotsAlive = BotsTotal;
                }
            }

            for (int i = 0; i < BotsTotal; i++)
            {
                CharacterObject troop = GetTroopForWave(WaveNumber);

                // Cavalry after wave 10 (Mechanic 7)
                if (WaveNumber >= 10 && _rand.NextDouble() < 0.2)
                    troop = GetCavalryTroop();

                // Mutator overrides
                if (Mutator == MutatorType.Berserk) troop = GetBerserker();
                if (Mutator == MutatorType.CavalryRush) troop = GetCavalryTroop();
                if (Mutator == MutatorType.ShieldWall) troop = GetShieldTroop();

                int entry = 32 + (i % 32);
                var entryPoint = Mission.Current.GetEntryPoint(entry);
                if (entryPoint == null) continue;

                var pos = entryPoint.Position;
                var agent = Mission.Current.SpawnAgent(new AgentBuildData(troop).Team(_nordTeam).InitialPosition(pos));
                if (agent != null)
                {
                    _spawnedNords.Add(agent);
                    // Apply mutator buffs
                    if (Mutator == MutatorType.Berserk)
                        agent.SetMaximumSpeedFactor(1.5f);
                    // Register for morale
                    Mission.GetMissionBehavior<MoraleBehavior>()?.RegisterAgentToNearestSquad(agent);
                }
            }

            // Boss
            if (WaveNumber % 5 == 0)
                SpawnBoss();
            if (Mutator == MutatorType.BossRush)
                SpawnBoss(3);

            // Objectives (Mechanic 4)
            var objectiveBehavior = Mission.GetMissionBehavior<NordInvasionObjectiveBehavior>();
            objectiveBehavior?.SetupObjective(Objective);

            State = WaveState.InProgress;
            InformationManager.DisplayMessage(new InformationMessage($"Wave {WaveNumber} STARTED! Kill all Nords!", Colors.Green));
        }

        void SpawnBoss(int count = 1)
        {
            var bossTroop = GetBossTroop();
            if (bossTroop == null) return;
            var entry = Mission.Current.GetEntryPoint(64);
            if (entry == null) return;

            for (int i = 0; i < count; i++)
            {
                var pos = entry.Position + new Vec3(_rand.Next(-2, 2), _rand.Next(-2, 2), 0f);
                var agent = Mission.Current.SpawnAgent(new AgentBuildData(bossTroop).Team(_nordTeam).InitialPosition(pos));
                if (agent != null)
                {
                    BotsTotal++;
                    BotsAlive++;
                    var bossBehavior = Mission.GetMissionBehavior<BossPhaseBehavior>();
                    bossBehavior?.RegisterBoss(agent);
                }
            }
            InformationManager.DisplayMessage(new InformationMessage($"BOSS SPAWNED! x{count}", Colors.Red));
        }

        void OnWaveCompleted()
        {
            State = WaveState.Completed;
            InformationManager.DisplayMessage(new InformationMessage($"Wave {WaveNumber} COMPLETED!", Colors.Green));

            // Reward alive players +20 gold (+ сохранение в бэкенд)
            var persist = Mission.GetMissionBehavior<PersistenceManager>();
            foreach (var agent in _playerTeam.ActiveAgents)
            {
                var comp = agent.GetComponent<PersistenceManager.PlayerGoldComponent>();
                comp?.AddGold(20);
                if (!IsFallen(agent))
                    persist?.OnWaveCompletedFor(agent, WaveNumber, 20, 0, 0);
            }

            // Perk choice every 3 waves (Mechanic 1)
            if (WaveNumber % 3 == 0)
            {
                var perkMgr = Mission.GetMissionBehavior<PerkManager>();
                perkMgr?.ShowChoiceToAll();
            }

            // Director relief
            Mission.GetMissionBehavior<NordInvasionDirectorBehavior>()?.OnWaveCompleted();

            // Betting payout (Mechanic 27)
            var betting = Mission.GetMissionBehavior<SpectatorBettingBehavior>();
            betting?.OnWaveCompleted(WaveNumber, _playerTeam.ActiveAgents.ToList());

            if (WaveNumber >= VictoryWave)
            {
                InformationManager.DisplayMessage(new InformationMessage("VICTORY! All 25 waves defeated! Swadia saved!", Colors.Gold));
                Audio.NISound.PlayVictory();
                Mission.GetMissionBehavior<PersistenceManager>()?.OnCampaignWin();

                // Сохраняем забег (победа) для каждого игрока
                var persistWin = Mission.GetMissionBehavior<PersistenceManager>();
                foreach (var agent in _playerTeam.ActiveAgents)
                    persistWin?.SaveRun(agent, true, VictoryWave);

                _endMissionAt = Mission.CurrentTime + 5f;
                return;
            }

            WaveNumber++;
            SetupWave(WaveNumber);
        }

        public void OnBotKilled(Agent killed, Agent killer)
        {
            if (killed == null || killed.Character == null) return;
            BotsAlive = Math.Max(0, BotsAlive - 1);

            // Director stress down
            Mission.GetMissionBehavior<NordInvasionDirectorBehavior>()?.OnBotKilled();
            Mission.GetMissionBehavior<MoraleBehavior>()?.OnAgentKilled(killed, killer);

            int backendGold = 10;
            // Gold reward + scavenging
            if (killer != null && _playerTeam != null && killer.Team == _playerTeam)
            {
                int gold = GetGoldForTroop(killed.Character);
                if (Mutator == MutatorType.Greedy) gold *= 2;

                var goldComp = killer.GetComponent<PersistenceManager.PlayerGoldComponent>();
                if (goldComp != null)
                {
                    // Gold hunter perk
                    if (goldComp.HasPerk(22)) gold = (int)(gold * 1.2f);
                    goldComp.AddGold(gold);
                    goldComp.Kills++;
                    backendGold = gold;

                    InformationManager.DisplayMessage(new InformationMessage($"+{gold} gold! Total: {goldComp.Gold}", Colors.Yellow));

                    // Scavenging 20% metal (Mechanic 9)
                    if (_rand.Next(100) < 20)
                    {
                        goldComp.AddMetal(1);
                        InformationManager.DisplayMessage(new InformationMessage("+1 Scrap Metal!", Colors.Yellow));
                    }
                    if (_rand.Next(100) < 30)
                    {
                        goldComp.AddWood(1);
                    }
                }

                // Boss loot (Mechanic 8)
                if (killed.Character.StringId.Contains("chieftain") || killed.Character.StringId.Contains("boss"))
                {
                    Mission.GetMissionBehavior<LootManager>()?.SpawnLootBag(killed.Position, 500);
                }

                // Elemental combo check
                var elemental = killer.GetComponent<Components.ElementalWeaponComponent>();
                elemental?.OnHit(killed);
            }

            // Backend call (Mechanic 12)
            Mission.GetMissionBehavior<PersistenceManager>()?.OnKill(killed, killer, WaveNumber, backendGold);
        }

        // Troop getters - load from CharacterObject
        CharacterObject GetTroopForWave(int wave)
        {
            string id;
            if (wave < 4) id = "ni_nord_peasant";
            else if (wave < 8) id = "ni_nord_footman";
            else if (wave < 12) id = "ni_nord_veteran";
            else if (wave < 16) id = "ni_nord_huscarl";
            else id = "ni_nord_jarl_guard";

            return Game.Current.ObjectManager.GetObject<CharacterObject>(id)
                ?? Game.Current.ObjectManager.GetObject<CharacterObject>("ni_nord_peasant");
        }

        CharacterObject GetCavalryTroop() => Game.Current.ObjectManager.GetObject<CharacterObject>("ni_nord_raider_mounted") ?? GetTroopForWave(1);
        CharacterObject GetBerserker() => Game.Current.ObjectManager.GetObject<CharacterObject>("ni_nord_berserker") ?? GetTroopForWave(1);
        CharacterObject GetShieldTroop() => Game.Current.ObjectManager.GetObject<CharacterObject>("ni_nord_huscarl") ?? GetTroopForWave(1);
        CharacterObject GetBossTroop() => Game.Current.ObjectManager.GetObject<CharacterObject>("ni_nord_chieftain") ?? GetTroopForWave(1);

        int GetGoldForTroop(CharacterObject troop)
        {
            if (troop == null) return 5;
            var id = troop.StringId;
            if (id.Contains("peasant")) return 3;
            if (id.Contains("footman")) return 6;
            if (id.Contains("archer")) return 7;
            if (id.Contains("veteran")) return 10;
            if (id.Contains("huscarl")) return 15;
            if (id.Contains("berserker")) return 20;
            if (id.Contains("jarl")) return 35;
            if (id.Contains("chieftain")) return 100;
            return 10;
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectedAgent == null || affectedAgent.Team == null) return;

            if (affectedAgent.Team.Side == BattleSideEnum.Attacker)
            {
                OnBotKilled(affectedAgent, affectorAgent);
                return;
            }

            if (_playerTeam == null || affectedAgent.Team != _playerTeam) return;

            // Check if fallen or dead
            var wound = affectedAgent.GetComponent<Components.WoundStaminaComponent>();
            if (wound != null && wound.TryFall())
            {
                // Fallen, not dead - can be revived
                InformationManager.DisplayMessage(new InformationMessage($"{affectedAgent.Name} fallen! Medic can revive!", Colors.Yellow));
            }
            else
            {
                // Real death
                Mission.GetMissionBehavior<NordInvasionDirectorBehavior>()?.OnPlayerDied();
                Mission.GetMissionBehavior<MoraleBehavior>()?.OnAgentKilled(affectedAgent, affectorAgent);
                Mission.GetMissionBehavior<SpectatorBettingBehavior>()?.OnPlayerKilled(affectedAgent, affectorAgent);
            }
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData collisionData)
        {
            if (affectedAgent == null) return;

            // Stamina system (Mechanic 13)
            var wound = affectedAgent.GetComponent<Components.WoundStaminaComponent>();
            wound?.OnHit(blow.InflictedDamage);

            // Marked player mutator
            if (Mutator == MutatorType.Marked)
            {
                var director = Mission.GetMissionBehavior<NordInvasionDirectorBehavior>();
                if (director != null && director.MarkedPlayer != null && affectedAgent == director.MarkedPlayer)
                {
                    // Все боты преследуют помеченного
                    foreach (var bot in _spawnedNords.Where(a => a.IsActive()))
                    {
                        bot.SetTargetForAI(affectedAgent);
                    }
                }
            }

            // Greedy mutator steals gold on hit
            if (Mutator == MutatorType.Greedy && _playerTeam != null && affectedAgent.Team == _playerTeam)
            {
                var goldComp = affectedAgent.GetComponent<PersistenceManager.PlayerGoldComponent>();
                if (goldComp != null && goldComp.Gold >= 5)
                {
                    goldComp.AddGold(-5);
                    InformationManager.DisplayMessage(new InformationMessage("Loki steals 5 gold on hit!", Colors.Red));
                }
            }
        }
    }
}
