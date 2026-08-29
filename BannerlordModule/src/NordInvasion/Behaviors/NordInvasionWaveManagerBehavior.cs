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
    public enum WaveState { Idle, Preparing, Spawning, InProgress, Completed, Failed }
    public enum WaveObjective { KillAll, DestroyRam, Escort, BurnCamps, DefendTreasury }

    public class NordInvasionWaveManagerBehavior : MissionBehavior
    {
        public int WaveNumber = 1;
        public int BotsAlive = 0;
        public int BotsTotal = 0;
        public WaveState State = WaveState.Preparing;
        public WaveObjective Objective = WaveObjective.KillAll;
        public MutatorType Mutator = MutatorType.None;
        public float NextWaveTime = 0f;
        public bool IsRespawnWave => WaveNumber % 4 == 0;

        private List<Agent> _spawnedNords = new List<Agent>();
        private Random _rand = new Random();

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (State == WaveState.Preparing && Mission.CurrentTime > NextWaveTime)
            {
                SpawnWave();
            }

            if (State == WaveState.InProgress && BotsAlive <= 0)
            {
                OnWaveCompleted();
            }

            // Check defeat
            if (Mission.PlayerTeam != null && Mission.PlayerTeam.ActiveAgents.Count == 0 && !IsRespawnWave)
            {
                State = WaveState.Failed;
                InformationManager.DisplayMessage(new InformationMessage("All players dead! Defeat!", Colors.Red));
                // Mission.EndMission...
            }
        }

        public void SetupWave(int waveNo)
        {
            WaveNumber = waveNo;
            State = WaveState.Preparing;

            // Objective every 3 waves (Mechanic 4)
            if (waveNo % 3 == 0)
                Objective = (WaveObjective)_rand.Next(1, 5);
            else
                Objective = WaveObjective.KillAll;

            // Mutator every 4 waves (Mechanic 10)
            if (waveNo % 4 == 0)
                Mutator = (MutatorType)_rand.Next(1, 13);
            else
                Mutator = MutatorType.None;

            // Weather every 5 waves (Mechanic 6)
            if (waveNo % 5 == 0)
                Mission.GetMissionBehavior<NordInvasionWeatherBehavior>()?.SetRandomWeather();

            // Director affects count (Mechanic 5)
            int playerCount = Mission.PlayerTeam?.ActiveAgents.Count ?? 1;
            int baseCount = 8 + waveNo * 2 + playerCount * 2;
            var director = Mission.GetMissionBehavior<NordInvasionDirectorBehavior>();
            if (director != null)
            {
                if (director.Stress > 80) baseCount = (int)(baseCount * 1.2f);
                else if (director.Stress < 30) baseCount = (int)(baseCount * 0.8f);
            }
            if (Mutator == MutatorType.BossRush) baseCount += 5;

            BotsTotal = Math.Min(baseCount, 100); // Bannerlord can handle 100 easily, even 400
            BotsAlive = BotsTotal;

            NextWaveTime = Mission.CurrentTime + 8f; // 8 sec prep

            InformationManager.DisplayMessage(new InformationMessage(
                $"Wave {WaveNumber} preparing... {BotsTotal} Nords! Obj: {Objective} Mutator: {Mutator}", Colors.Cyan));
        }

        void SpawnWave()
        {
            State = WaveState.Spawning;
            _spawnedNords.Clear();

            // Mechanic 11: Squads every 3rd wave
            if (WaveNumber % 3 == 0 && Objective == WaveObjective.KillAll)
            {
                Mission.GetMissionBehavior<SquadManager>()?.SpawnShieldWallSquad(32);
                Mission.GetMissionBehavior<SquadManager>()?.SpawnShieldWallSquad(40);
                BotsTotal -= 16; // squads already spawned
            }

            for (int i = 0; i < BotsTotal; i++)
            {
                CharacterObject troop = GetTroopForWave(WaveNumber);

                // Mechanic 7: Cavalry after wave 10
                if (WaveNumber >= 10 && _rand.NextDouble() < 0.2)
                    troop = GetCavalryTroop();

                // Mutator overrides
                if (Mutator == MutatorType.Berserk) troop = GetBerserker();
                if (Mutator == MutatorType.CavalryRush) troop = GetCavalryTroop();

                var entry = 32 + (i % 32);
                var pos = Mission.Current.GetEntryPoint(entry).Position;
                var agent = Mission.Current.SpawnAgent(new AgentBuildData(troop).Team(Mission.Current.Teams.First(t => t.Side == BattleSideEnum.Attacker)).InitialPosition(pos));
                if (agent != null)
                {
                    _spawnedNords.Add(agent);
                    // Apply mutator buffs
                    if (Mutator == MutatorType.Berserk)
                    {
                        agent.SetMaximumSpeedFactor(1.5f);
                    }
                }
            }

            // Boss
            if (WaveNumber % 5 == 0)
            {
                SpawnBoss();
            }
            if (Mutator == MutatorType.BossRush)
            {
                SpawnBoss(3);
            }

            // Objectives
            var objectiveBehavior = Mission.GetMissionBehavior<NordInvasionObjectiveBehavior>();
            objectiveBehavior?.SetupObjective(Objective);

            State = WaveState.InProgress;
            InformationManager.DisplayMessage(new InformationMessage($"Wave {WaveNumber} STARTED! Kill all Nords!", Colors.Green));
        }

        void SpawnBoss(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                var bossTroop = GetBossTroop();
                var pos = Mission.Current.GetEntryPoint(64).Position;
                Mission.Current.SpawnAgent(new AgentBuildData(bossTroop).Team(Mission.Current.Teams.First(t => t.Side == BattleSideEnum.Attacker)).InitialPosition(pos));
                BotsTotal++;
                BotsAlive++;
            }
        }

        void OnWaveCompleted()
        {
            State = WaveState.Completed;
            InformationManager.DisplayMessage(new InformationMessage($"Wave {WaveNumber} COMPLETED!", Colors.Green));

            // Reward alive players
            foreach (var agent in Mission.PlayerTeam.ActiveAgents)
            {
                var comp = agent.GetComponent<PersistenceManager.PlayerGoldComponent>();
                comp?.AddGold(20);
            }

            // Mechanic 1: Perk choice every 3 waves
            if (WaveNumber % 3 == 0)
            {
                Mission.GetMissionBehavior<Managers.PerkManager>()?.ShowChoiceToAll();
            }

            // Director relief
            Mission.GetMissionBehavior<NordInvasionDirectorBehavior>()?.OnWaveCompleted();

            if (WaveNumber >= 25)
            {
                InformationManager.DisplayMessage(new InformationMessage("VICTORY! All waves defeated!", Colors.Gold));
                State = WaveState.Completed;
                // End mission after 10 sec
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

            // Gold reward + scavenging (Mechanic 9)
            if (killer != null && killer.Team == Mission.PlayerTeam)
            {
                int gold = GetGoldForTroop(killed.Character);
                if (Mutator == MutatorType.Greedy) gold *= 2;

                var goldComp = killer.GetComponent<PersistenceManager.PlayerGoldComponent>();
                goldComp?.AddGold(gold);

                // Scavenging 20% metal
                if (_rand.Next(100) < 20)
                {
                    goldComp?.AddMetal(1);
                    InformationManager.DisplayMessage(new InformationMessage("+1 Scrap Metal!", Colors.Yellow));
                }

                // Mechanic 8: Boss loot
                if (killed.Character.StringId.Contains("chieftain") || killed.Character.StringId.Contains("boss"))
                {
                    Mission.GetMissionBehavior<Managers.LootManager>()?.SpawnLootBag(killed.Position, 500);
                }
            }

            // Backend call (Mechanic 12)
            Mission.GetMissionBehavior<PersistenceManager>()?.OnKill(killed, killer, WaveNumber);
        }

        CharacterObject GetTroopForWave(int wave)
        {
            // Simplified - in real mod load from XML
            if (wave < 4) return Game.Current.ObjectManager.GetObject<CharacterObject>("ni_nord_peasant");
            if (wave < 8) return Game.Current.ObjectManager.GetObject<CharacterObject>("ni_nord_footman");
            if (wave < 12) return Game.Current.ObjectManager.GetObject<CharacterObject>("ni_nord_huscarl");
            return Game.Current.ObjectManager.GetObject<CharacterObject>("ni_nord_jarl_guard");
        }

        CharacterObject GetCavalryTroop() => Game.Current.ObjectManager.GetObject<CharacterObject>("ni_nord_raider_mounted");
        CharacterObject GetBerserker() => Game.Current.ObjectManager.GetObject<CharacterObject>("ni_nord_berserker");
        CharacterObject GetBossTroop() => Game.Current.ObjectManager.GetObject<CharacterObject>("ni_nord_chieftain");
        int GetGoldForTroop(CharacterObject troop)
        {
            if (troop.StringId.Contains("peasant")) return 3;
            if (troop.StringId.Contains("footman")) return 6;
            if (troop.StringId.Contains("huscarl")) return 15;
            if (troop.StringId.Contains("chieftain")) return 100;
            return 10;
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectedAgent.Team.Side == BattleSideEnum.Attacker)
                OnBotKilled(affectedAgent, affectorAgent);
        }
    }
}
