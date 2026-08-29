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

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (_nordTeam == null)
                _nordTeam = Mission.Teams.FirstOrDefault(t => t.Side == BattleSideEnum.Attacker);
            if (_playerTeam == null)
                _playerTeam = Mission.PlayerTeam ?? Mission.Teams.FirstOrDefault(t => t.Side == BattleSideEnum.Defender);

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

            // InProgress -> Completed
            if (State == WaveState.InProgress && BotsAlive <= 0)
            {
                OnWaveCompleted();
            }

            // Check defeat
            if (State == WaveState.InProgress && _playerTeam != null)
            {
                int alivePlayers = _playerTeam.ActiveAgents.Count(a => !IsFallen(a));
                if (alivePlayers == 0 && !IsRespawnWave)
                {
                    State = WaveState.Failed;
                    InformationManager.DisplayMessage(new InformationMessage("All players dead! Defeat!", Colors.Red));
                    // TODO: End mission
                }
            }
        }

        bool IsFallen(Agent agent)
        {
            var wound = agent.GetComponent<Components.WoundStaminaComponent>();
            return wound != null && wound.IsFallen;
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
                var players = Mission.PlayerTeam?.ActiveAgents;
                if (players != null && players.Count > 0)
                {
                    var marked = players[_rand.Next(players.Count)];
                    // Store in global or director
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

            BotsTotal = Math.Min(baseCount, 120); // Bannerlord can handle 120
            BotsAlive = BotsTotal;

            NextWaveTime = Mission.CurrentTime + 8f;

            InformationManager.DisplayMessage(new InformationMessage(
                $"Wave {WaveNumber} preparing... {BotsTotal} Nords! Obj: {Objective} Mutator: {Mutator} {(IsRespawnWave ? "[RESPAWN WAVE]" : "")}", Colors.Cyan));

            // Update HUD
            var hud = Mission.GetMissionBehavior<UI.HUD.NI_HUD_Behavior>();
            hud?.UpdateWave(WaveNumber, BotsTotal, 0, Objective, Mutator);
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
                    BotsTotal = Math.Max(0, BotsTotal - 16);
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
                var agent = Mission.Current.SpawnAgent(new AgentBuildData(troop).Team(_nordTeam).InitialPosition(pos).InitialDirection(entryPoint.Direction));
                if (agent != null)
                {
                    _spawnedNords.Add(agent);
                    // Apply mutator buffs
                    if (Mutator == MutatorType.Berserk)
                    {
                        agent.SetMaximumSpeedFactor(1.5f);
                        // No block - set AI to not block?
                    }
                    // Register for morale
                    var morale = Mission.GetMissionBehavior<MoraleBehavior>();
                    // morale?.RegisterAgent(agent);
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
            var entry = Mission.Current.GetEntryPoint(64);
            if (entry == null) return;

            for (int i = 0; i < count; i++)
            {
                var pos = entry.Position + new Vec3(_rand.Next(-2, 2), _rand.Next(-2, 2), 0);
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

            // Reward alive players +20 gold
            foreach (var agent in _playerTeam.ActiveAgents)
            {
                var comp = agent.GetComponent<PersistenceManager.PlayerGoldComponent>();
                comp?.AddGold(20);
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

            // Supply check
            var supply = Mission.GetMissionBehavior<SupplyBehavior>();
            // supply auto repair if level 2

            if (WaveNumber >= 25)
            {
                InformationManager.DisplayMessage(new InformationMessage("VICTORY! All 25 waves defeated! Swadia saved!", Colors.Gold));
                State = WaveState.Completed;
                // End mission after 10 sec
                NextWaveTime = Mission.CurrentTime + 10f;
                // TODO: Campaign win
                var persistence = Mission.GetMissionBehavior<PersistenceManager>();
                persistence?.OnCampaignWin();
            }
            else
            {
                WaveNumber++;
                SetupWave(WaveNumber);
            }
        }

        public void OnBotKilled(Agent killed, Agent killer)
        {
            BotsAlive = Math.Max(0, BotsAlive - 1);

            // Director stress down
            Mission.GetMissionBehavior<NordInvasionDirectorBehavior>()?.OnBotKilled();
            Mission.GetMissionBehavior<MoraleBehavior>()?.OnAgentKilled(killed, killer);

            // Gold reward + scavenging
            if (killer != null && killer.Team == _playerTeam)
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
            Mission.GetMissionBehavior<PersistenceManager>()?.OnKill(killed, killer, WaveNumber);
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

            return Game.Current.ObjectManager.GetObject<CharacterObject>(id) ?? Game.Current.ObjectManager.GetObject<CharacterObject>("ni_nord_peasant");
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
            if (affectedAgent.Team != null && affectedAgent.Team.Side == BattleSideEnum.Attacker)
                OnBotKilled(affectedAgent, affectorAgent);
            else if (affectedAgent.Team == _playerTeam)
            {
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
                }
            }
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData collisionData)
        {
            // Stamina system (Mechanic 13)
            var wound = affectedAgent.GetComponent<Components.WoundStaminaComponent>();
            wound?.OnHit(blow.InflictedDamage);

            // Marked player mutator
            if (Mutator == MutatorType.Marked)
            {
                var director = Mission.GetMissionBehavior<NordInvasionDirectorBehavior>();
                if (director != null && director.MarkedPlayer != null && affectedAgent == director.MarkedPlayer)
                {
                    // All bots chase marked
                    foreach (var bot in _spawnedNords.Where(a => a.IsActive()))
                    {
                        // Set target
                    }
                }
            }

            // Greedy mutator steals gold on hit
            if (Mutator == MutatorType.Greedy && affectedAgent.Team == _playerTeam)
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
